extends Node
## Holds the Touhou data archives the player has dropped onto the game window.
## The game ships none of ZUN's files; players bring their own copy (sold on Steam).
## A dropped file is copied into user:// (browser storage on the web build), so it only
## has to be dropped once.

signal archive_loaded(game_id: String, file_count: int)
signal archive_failed(message: String)
## thbgm.dat was accepted and its tracks are being cut out (a few seconds per track).
signal music_preparing(track: int, of: int)
## Emitted once the graphics and sounds built from the .dat files are in place.
signal graphics_ready

const CACHE_DIR: String = "user://game_files"
## Graphics and sounds built from the .dat files, saved so later launches skip unpacking them
## (a few seconds per character). Also only on this machine, made from the player's own copy.
## Cleared whenever a .dat is dropped again.
const ASSET_CACHE: String = "user://game_files/graphics"
# game id -> [file name, archive format, a file only that game's archive has]. The marker
# tells the games apart whatever the dropped file is called (th08 shares th09's format).
const KNOWN_FILES: Dictionary = {
	"th09": ["th09.dat", "PBGZ", "pl15.sht"],
	"th15": ["th15.dat", "THA1", "st07enm3.anm"],
}
## PoFV's soundtrack, cut from the player's thbgm.dat (DatMusic). Kept apart from ASSET_CACHE
## so dropping another .dat doesn't make the player hand over 440 MB of music again.
const MUSIC_CACHE: String = "user://game_files/music"
## The "game id" the file screen and signals use for thbgm.dat.
const MUSIC_ID: String = "thbgm"
## Limits on searching a dropped folder: a Steam library's games sit at steamapps/common/<game>,
## three levels down, so six leaves room for other layouts.
const SCAN_MAX_DEPTH: int = 6
const SCAN_MAX_FOLDERS: int = 20000
const OTHER_MUSIC_MESSAGE: String = "That thbgm.dat is another game's music. PoFV's is the one in the same folder as th09.dat."

# game id ("th09") -> ThDatArchive
var archives: Dictionary = {}
# "game:file.anm" -> ThAnm, parsed on first use
var _anms: Dictionary = {}
# "game:file:sprite:scale" -> ImageTexture
var _sprite_textures: Dictionary = {}
# Resources given graphics from the .dat files; held so the resource cache keeps these copies.
var _filled_resources: Array[Resource] = []
## True once graphics_ready has fired for the current set of files.
var graphics_are_ready: bool = false


func _ready() -> void:
	get_window().files_dropped.connect(_on_files_dropped)
	_setup_web_drop()
	_load_cached_files()


func has_archive(game_id: String) -> bool:
	if game_id == MUSIC_ID:
		return has_music()
	return archives.has(game_id)


func has_all_files() -> bool:
	for game_id in KNOWN_FILES:
		if not archives.has(game_id):
			return false
	return has_music()


## True once every track has been cut from thbgm.dat and saved.
func has_music() -> bool:
	for id in DatMusic.TRACKS:
		if not FileAccess.file_exists(_music_path(id)):
			return false
	return true


func _music_path(id: String) -> String:
	return "%s/%s_v%d.qoa" % [MUSIC_CACHE, id, DatMusic.VERSION]


## thbgm.fmt from th09.dat, parsed; empty until th09.dat is in.
func _music_fmt() -> Dictionary:
	if not archives.has("th09"):
		return {}
	return DatMusic.parse_fmt(archives["th09"].extract(DatMusic.FMT_FILE))


## PoFV's thbgm.dat size, where the last track in its track list ends; 0 until th09.dat is
## in. Every Touhou game names its music thbgm.dat with the same header, but each game's
## track list ends at its own file's exact size, so this tells PoFV's apart.
func _music_file_size() -> int:
	var end := 0
	for entry in _music_fmt().values():
		end = maxi(end, entry.offset + entry.length)
	return end


## Cuts every track the game plays out of thbgm.dat, compresses it and saves it. `read` is
## (offset, length) -> PackedByteArray and may be a coroutine: on desktop it reads the file,
## on the web build it asks the browser for just that slice (see WEB_DROP_JS). Yields
## between tracks so the file screen can show progress.
func _extract_music(read: Callable, file_name: String) -> void:
	var fmt := _music_fmt()
	if fmt.is_empty():
		archive_failed.emit("%s couldn't be read." % file_name)
		return
	DirAccess.make_dir_recursive_absolute(MUSIC_CACHE)
	var done := 0
	for id in DatMusic.TRACKS:
		music_preparing.emit(done + 1, DatMusic.TRACKS.size())
		await get_tree().process_frame
		await get_tree().process_frame
		var entry: Dictionary = fmt[DatMusic.TRACKS[id]]
		var pcm: PackedByteArray = await read.call(entry.offset, entry.length)
		var qoa := DatMusic.compress(pcm, entry) if pcm.size() == entry.length else PackedByteArray()
		var out := FileAccess.open(_music_path(id), FileAccess.WRITE)
		if out == null or qoa.is_empty():
			archive_failed.emit("Couldn't save the music from %s." % file_name)
			return
		out.store_buffer(qoa)
		out.close()
		done += 1
	_prepare_music()
	archive_loaded.emit(MUSIC_ID, done)
	_prepare_graphics_soon()


## Desktop: thbgm.dat stays where it is on disk, and only the track slices are read.
func _extract_music_from_path(path: String) -> void:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		archive_failed.emit("%s couldn't be read." % path.get_file())
		return
	await _extract_music(func(offset: int, length: int) -> PackedByteArray:
		file.seek(offset)
		return file.get_buffer(length), path.get_file())


## Web build: Godot's own drop handler reads a dropped file into memory whole, which for
## thbgm.dat's 440 MB would crash many browsers. This listener runs first (capture phase on
## window, before the canvas handler) and keeps any big file as a browser File reference,
## so only the track slices are ever read (File.slice). Other files still go to Godot.
const WEB_BIG_FILE_BYTES: int = 100 * 1024 * 1024
## A dropped folder is walked here too, by name only (readEntries never reads a file), and
## only the wanted files are handed to Godot as File references (twaOnFolder), so a Steam
## library never goes near Godot's handler, which would read every file into memory.
## Formatted with: the wanted names (a JSON array), SCAN_MAX_DEPTH, SCAN_MAX_FOLDERS, then
## WEB_BIG_FILE_BYTES.
const WEB_DROP_JS: String = """
window.twaBigFile = null;
window.twaFolderFiles = [];
var twaWanted = %s;
function twaWalk(roots, label) {
	var found = [], pending = 1, folders = 0, finished = false;
	function done() {
		if (pending === 0 && !finished) {
			finished = true;
			window.twaFolderFiles = found;
			window.twaOnFolder(label, JSON.stringify(found.map(function (f) { return [f.name, f.size]; })));
		}
	}
	function visit(entry, depth) {
		if (entry.isFile) {
			if (twaWanted.indexOf(entry.name.toLowerCase()) >= 0) {
				pending++;
				entry.file(function (f) { found.push(f); pending--; done(); },
					function () { pending--; done(); });
			}
			return;
		}
		if (depth > %d || folders >= %d || entry.name.charAt(0) === '.') { return; }
		folders++;
		pending++;
		var reader = entry.createReader();
		(function read() {
			reader.readEntries(function (batch) {
				if (batch.length === 0) { pending--; done(); return; }
				for (var j = 0; j < batch.length; j++) { visit(batch[j], depth + 1); }
				read();
			}, function () { pending--; done(); });
		})();
	}
	for (var i = 0; i < roots.length; i++) { visit(roots[i], 0); }
	pending--;
	done();
}
window.addEventListener('drop', function (event) {
	// Entries must be taken during the event; they stay usable after it.
	var items = event.dataTransfer ? event.dataTransfer.items : null;
	var entries = [], folder = null;
	for (var k = 0; items && k < items.length; k++) {
		var entry = items[k].webkitGetAsEntry ? items[k].webkitGetAsEntry() : null;
		if (entry) {
			entries.push(entry);
			if (entry.isDirectory && !folder) { folder = entry.name; }
		}
	}
	if (folder) {
		event.preventDefault();
		event.stopImmediatePropagation();
		twaWalk(entries, folder);
		return;
	}
	var files = event.dataTransfer ? event.dataTransfer.files : null;
	if (!files) { return; }
	for (var i = 0; i < files.length; i++) {
		if (files[i].size > %d) {
			event.preventDefault();
			event.stopImmediatePropagation();
			window.twaBigFile = files[i];
			window.twaOnBigFile(files[i].name, files.length, files[i].size);
			return;
		}
	}
}, true);
window.twaReadSlice = function (offset, length) {
	window.twaBigFile.slice(offset, offset + length).arrayBuffer().then(function (buffer) {
		window.twaOnSlice(buffer);
	});
};
"""

signal _web_slice_read(bytes: PackedByteArray)

# Kept referenced: JavaScript only holds weak handles to these callbacks.
var _web_big_file_callback: JavaScriptObject
var _web_slice_callback: JavaScriptObject
var _web_folder_callback: JavaScriptObject


func _setup_web_drop() -> void:
	if not OS.has_feature("web"):
		return
	_web_big_file_callback = JavaScriptBridge.create_callback(_on_web_big_file)
	_web_slice_callback = JavaScriptBridge.create_callback(_on_web_slice)
	_web_folder_callback = JavaScriptBridge.create_callback(_on_web_folder)
	var window := JavaScriptBridge.get_interface("window")
	window.twaOnBigFile = _web_big_file_callback
	window.twaOnSlice = _web_slice_callback
	window.twaOnFolder = _web_folder_callback
	JavaScriptBridge.eval(WEB_DROP_JS % [JSON.stringify(_wanted_file_names()), SCAN_MAX_DEPTH,
		SCAN_MAX_FOLDERS, WEB_BIG_FILE_BYTES], true)


## A folder dropped on the web build, walked by WEB_DROP_JS: `args` is the folder's name and
## a JSON list of [name, size], one per wanted file found, indexed as window.twaFolderFiles.
## The .dat files are read in and loaded like a dropped file; then PoFV's music, picked by size.
func _on_web_folder(args: Array) -> void:
	var found: Array = JSON.parse_string(str(args[1])) if args.size() > 1 else []
	if found.is_empty():
		archive_failed.emit("No game files found in %s." % str(args[0]))
		return
	var music: Array[int] = []
	for i in found.size():
		var file_name: String = found[i][0]
		if file_name.to_lower() == "thbgm.dat":
			music.append(i)
			continue
		JavaScriptBridge.eval("window.twaBigFile = window.twaFolderFiles[%d];" % i, true)
		var bytes: PackedByteArray = await _web_read(0, int(found[i][1]))
		var incoming := "%s/incoming_%s" % [CACHE_DIR, file_name]
		DirAccess.make_dir_recursive_absolute(CACHE_DIR)
		var out := FileAccess.open(incoming, FileAccess.WRITE)
		if out == null:
			archive_failed.emit("%s couldn't be read." % file_name)
			continue
		out.store_buffer(bytes)
		out.close()
		load_archive_file(incoming)
		DirAccess.remove_absolute(incoming)
	var size := _music_file_size()
	for i in music:
		if int(found[i][1]) == size:
			JavaScriptBridge.eval("window.twaBigFile = window.twaFolderFiles[%d];" % i, true)
			await _extract_music(_web_read, str(found[i][0]))
			return


func _on_web_big_file(args: Array) -> void:
	var file_name: String = str(args[0])
	if int(args[1]) > 1:
		archive_failed.emit("Please drop %s on its own." % file_name)
		return
	var header: PackedByteArray = await _web_read(0, 4)
	if not DatMusic.is_thbgm(header):
		archive_failed.emit("%s isn't one of the files listed." % file_name)
		return
	if not archives.has("th09"):
		archive_failed.emit("Drop th09.dat first, then thbgm.dat (it needs th09.dat's track list).")
		return
	if int(args[2]) != _music_file_size():
		archive_failed.emit(OTHER_MUSIC_MESSAGE)
		return
	await _extract_music(_web_read, file_name)


## Asks the browser for one slice of the dropped big file and waits for it.
func _web_read(offset: int, length: int) -> PackedByteArray:
	JavaScriptBridge.eval("window.twaReadSlice(%d, %d);" % [offset, length], true)
	return await _web_slice_read


func _on_web_slice(args: Array) -> void:
	_web_slice_read.emit(JavaScriptBridge.js_buffer_to_packed_byte_array(args[0]))


## Hands AudioManager the saved soundtrack, looping where thbgm.fmt says.
func _prepare_music() -> void:
	var fmt := _music_fmt()
	if fmt.is_empty() or not has_music():
		return
	var streams: Dictionary = {}
	for id in DatMusic.TRACKS:
		streams[id] = DatMusic.make_stream(FileAccess.get_file_as_bytes(_music_path(id)), fmt[DatMusic.TRACKS[id]])
	var audio := get_node_or_null("/root/AudioManager")
	if audio:
		audio.set_music(streams)


## One sprite from a sprite sheet in the player's .dat, enlarged `scale` times with hard
## pixel edges. Returns null only if that .dat isn't loaded, which the file screen doesn't
## let happen.
func sprite(game_id: String, anm_file: String, sprite_id: int, scale: int = 1) -> Texture2D:
	var key := "%s:%s:%d:%d" % [game_id, anm_file, sprite_id, scale]
	if _sprite_textures.has(key):
		return _sprite_textures[key]
	var anm := _anm(game_id, anm_file)
	if anm == null:
		return null
	var image := anm.sprite_image(sprite_id)
	if image == null:
		push_warning("GameData: %s has no sprite %d" % [anm_file, sprite_id])
		return null
	if scale != 1:
		image.resize(image.get_width() * scale, image.get_height() * scale, Image.INTERPOLATE_NEAREST)
	var texture := ImageTexture.create_from_image(image)
	_sprite_textures[key] = texture
	return texture


func _anm(game_id: String, anm_file: String) -> ThAnm:
	var key := "%s:%s" % [game_id, anm_file]
	if _anms.has(key):
		return _anms[key]
	if not archives.has(game_id):
		return null
	var archive: ThDatArchive = archives[game_id]
	var anm := ThAnm.new()
	if not anm.parse(archive.extract(anm_file), archive.format == "THA1"):
		push_warning("GameData: couldn't read %s: %s" % [anm_file, anm.error])
		return null
	_anms[key] = anm
	return anm


## An image built from the .dat files by `build`, loaded from ASSET_CACHE when an earlier
## launch already built it. `key` should carry a version number, bumped whenever the recipe
## changes, so an old saved copy isn't reused.
func cached_image(key: String, build: Callable) -> Image:
	var path := "%s/%s.png" % [ASSET_CACHE, key]
	if FileAccess.file_exists(path):
		var saved := Image.load_from_file(path)
		if saved:
			return saved
	var built: Image = build.call()
	if built:
		DirAccess.make_dir_recursive_absolute(ASSET_CACHE)
		built.save_png(path)
	return built


## Raw file bytes from the .dat files by `build`, kept in ASSET_CACHE the same way (sounds).
func cached_bytes(key: String, build: Callable) -> PackedByteArray:
	var path := "%s/%s" % [ASSET_CACHE, key]
	if FileAccess.file_exists(path):
		return FileAccess.get_file_as_bytes(path)
	var built: PackedByteArray = build.call()
	if not built.is_empty():
		DirAccess.make_dir_recursive_absolute(ASSET_CACHE)
		var file := FileAccess.open(path, FileAccess.WRITE)
		if file:
			file.store_buffer(built)
	return built


## Hands AudioManager PoFV's sound effects, the se_*.wav files in th09.dat as they are.
func _prepare_sounds() -> void:
	var streams: Dictionary = {}
	for sound_name in AudioService.SOUND_NAMES:
		var wav := cached_bytes("sfx_%s.wav" % sound_name,
			func() -> PackedByteArray: return archives["th09"].extract(sound_name + ".wav"))
		var stream := AudioStreamWAV.load_from_buffer(wav) if not wav.is_empty() else null
		if stream:
			streams[sound_name] = stream
		else:
			push_warning("GameData: th09.dat has no %s.wav" % sound_name)
	var audio := get_node_or_null("/root/AudioManager")
	if audio:
		audio.set_sounds(streams)


## Once every file is in: fills in the bullet sprites, Clownpiece's boss sheet and the
## player sheets, and decodes PoFV's bullet and item sheets up front so the first pickup of
## a match doesn't stutter while its sheet is unpacked.
func _prepare_graphics() -> void:
	# Graphics and sounds need th09.dat and th15.dat only; the music is handled on its own.
	for game_id in KNOWN_FILES:
		if not archives.has(game_id):
			return
	var etama := _anm("th09", "etama.anm")
	if etama == null:
		return
	for i in etama.entry_count():
		etama.entry_image(i)
	var th15_images: Dictionary = {}
	for key in BulletSprites.TH15_KEYS:
		th15_images[key] = cached_image("%s_v%d" % [key, BulletSprites.TH15_VERSION],
			func() -> Image: return BulletSprites.cut_th15(_anm("th15", "bullet.anm"), key))
	_filled_resources.clear()
	_filled_resources.append_array(BulletSprites.apply(etama, th15_images))

	var boss_sheet := cached_image("clownpiece_boss_v%d" % ClownpieceBossSheet.VERSION,
		func() -> Image: return ClownpieceBossSheet.build(_anm("th15", "st05enm.anm")))
	var boss := ClownpieceBossSheet.apply(boss_sheet)
	if boss:
		_filled_resources.append(boss)

	for boss_id in BossSheets.RECIPES:
		var pl_file := "pl%s.anm" % BossSheets.RECIPES[boss_id].pl
		var boss_image := cached_image("boss_%s_v%d" % [boss_id, BossSheets.VERSION],
			func() -> Image: return BossSheets.build(boss_id, _anm("th09", pl_file)))
		var boss_data := BossSheets.apply(boss_id, boss_image)
		if boss_data:
			_filled_resources.append(boss_data)

	for character in CharacterSprites.PLAYER_SHEETS:
		var pl: String = CharacterSprites.PLAYER_SHEETS[character]
		var sheet := cached_image("player_%s_v%d" % [character, CharacterSprites.VERSION],
			func() -> Image: return CharacterSprites.build_player_sheet(_anm("th09", "pl%s.anm" % pl), pl))
		var data := CharacterSprites.apply_player_sheet(character, sheet)
		if data:
			_filled_resources.append(data)
		var faces := cached_image("faces_%s_v%d" % [character, CharacterSprites.VERSION],
			func() -> Image: return CharacterSprites.build_faces(_anm("th09", "pl%s.anm" % pl), pl))
		var faces_data := CharacterSprites.apply_faces(character, faces)
		if faces_data:
			_filled_resources.append(faces_data)
	for character in CharacterSprites.SHOTS:
		var shot_pl: String = CharacterSprites.SHOTS[character][0]
		var shot := cached_image("shot_%s_v%d" % [character, CharacterSprites.VERSION],
			func() -> Image: return CharacterSprites.build_shot(_anm("th09", "pl%s.anm" % shot_pl), character))
		var shot_data := CharacterSprites.apply_shot(character, shot)
		if shot_data:
			_filled_resources.append(shot_data)
	var clownpiece_shot := cached_image("shot_clownpiece_th15_v%d" % BulletSprites.TH15_VERSION,
		func() -> Image: return BulletSprites.cut_th15_shot(_anm("th15", "bullet.anm")))
	var clownpiece_data := CharacterSprites.apply_shot("clownpiece", clownpiece_shot)
	if clownpiece_data:
		_filled_resources.append(clownpiece_data)
	for character in CharacterSprites.BANNERS:
		var banner_pl: String = CharacterSprites.PLAYER_SHEETS[character]
		var banner := cached_image("banner_%s_v%d" % [character, CharacterSprites.VERSION],
			func() -> Image: return CharacterSprites.build_banner(_anm("th09", "pl%s.anm" % banner_pl), character))
		var banner_data := CharacterSprites.apply_banner(character, banner)
		if banner_data:
			_filled_resources.append(banner_data)
	for character in CharacterSprites.SPELL_BGS:
		var bg_pl: String = CharacterSprites.PLAYER_SHEETS[character]
		for kind in CharacterSprites.SPELL_BGS[character]:
			var bg := cached_image("spellbg_%s_%s_v%d" % [character, kind, CharacterSprites.VERSION],
				func() -> Image: return CharacterSprites.build_spell_bg(_anm("th09", "pl%s.anm" % bg_pl), character, kind))
			var bg_data := CharacterSprites.apply_spell_bg(character, kind, bg)
			if bg_data:
				_filled_resources.append(bg_data)

	for texture_name in DatTextures.RECIPES:
		var recipe: Array = DatTextures.RECIPES[texture_name]
		var image := cached_image("tex_%s_v%d" % [texture_name, DatTextures.VERSION],
			func() -> Image: return DatTextures.build(texture_name, _anm(recipe[0], recipe[1])))
		DatTextures.apply(texture_name, image)
	# Unpacked .anm files are only needed to build what isn't cached yet.
	for key in _anms.keys():
		if key != "th09:etama.anm":
			_anms.erase(key)
	_prepare_sounds()
	_prepare_music()
	graphics_are_ready = true
	graphics_ready.emit()


## Waits two frames first, so whatever screen is up gets to show "Preparing graphics"
## before the first (uncached) build freezes it for a few seconds.
func _prepare_graphics_soon() -> void:
	await get_tree().process_frame
	await get_tree().process_frame
	_prepare_graphics()


func _clear_graphics_cache() -> void:
	for file in DirAccess.get_files_at(ASSET_CACHE):
		DirAccess.remove_absolute("%s/%s" % [ASSET_CACHE, file])


func _forget_sprites() -> void:
	_anms.clear()
	_sprite_textures.clear()


func _on_files_dropped(paths: PackedStringArray) -> void:
	var music: Array[String] = []
	for path in paths:
		if not DirAccess.dir_exists_absolute(path):
			load_archive_file(path)
			continue
		var found := find_game_files(path)
		if found.is_empty():
			archive_failed.emit("No game files found in %s." % path.get_file())
		for file_path in found:
			if file_path.get_file().to_lower() == "thbgm.dat":
				music.append(file_path)
			else:
				load_archive_file(file_path)
	# Music found in a folder is checked last, once th09.dat's track list is in, and only
	# PoFV's is taken: the others are other games' and are skipped without a word.
	var size := _music_file_size()
	for file_path in music:
		var file := FileAccess.open(file_path, FileAccess.READ)
		if file and file.get_length() == size:
			file.close()
			load_archive_file(file_path)
			break


## The file names a dropped folder is searched for (lower case).
func _wanted_file_names() -> Array[String]:
	var wanted: Array[String] = ["thbgm.dat"]
	for game_id in KNOWN_FILES:
		wanted.append(KNOWN_FILES[game_id][0])
	return wanted


## A dropped folder (a Steam library, say) is searched, subfolders too, for the files by
## name. Every thbgm.dat is returned; `_on_files_dropped` picks PoFV's by its size.
func find_game_files(folder: String) -> Array[String]:
	var wanted := _wanted_file_names()
	var found: Array[String] = []
	var queue: Array = [[folder, 0]]
	var folders_seen: int = 0
	# Bounded, so dropping a whole drive can't hang the game.
	while not queue.is_empty() and folders_seen < SCAN_MAX_FOLDERS:
		var entry: Array = queue.pop_front()
		var dir := DirAccess.open(entry[0])
		if dir == null:
			continue
		folders_seen += 1
		for file_name in dir.get_files():
			if file_name.to_lower() in wanted:
				found.append(entry[0].path_join(file_name))
		if entry[1] < SCAN_MAX_DEPTH:
			for sub in dir.get_directories():
				if not sub.begins_with("."):
					queue.append([entry[0].path_join(sub), entry[1] + 1])
	return found


func _load_cached_files() -> void:
	for game_id in KNOWN_FILES:
		var path := _cache_path(game_id)
		if not FileAccess.file_exists(path):
			continue
		var archive := _open_archive(path, game_id)
		if archive:
			archives[game_id] = archive
		else:
			# A damaged copy would otherwise fail on every launch; ask for the file again.
			DirAccess.remove_absolute(path)
	_prepare_graphics_soon()


## On the web build a dropped file is deleted as soon as the drop callback returns,
## so it is checked and copied into the cache before returning.
func load_archive_file(path: String) -> bool:
	var peek := FileAccess.open(path, FileAccess.READ)
	if peek and DatMusic.is_thbgm(peek.get_buffer(4)):
		var music_size := peek.get_length()
		peek.close()
		# Its track list is in th09.dat, so that has to come first.
		if not archives.has("th09"):
			archive_failed.emit("Drop th09.dat first, then thbgm.dat (it needs th09.dat's track list).")
			return false
		if music_size != _music_file_size():
			archive_failed.emit(OTHER_MUSIC_MESSAGE)
			return false
		_extract_music_from_path(path)
		return true
	peek = null
	var archive := ThDatArchive.new()
	if not archive.open(path):
		archive_failed.emit("%s: %s. Drop th09.dat, th15.dat or thbgm.dat from your game folders." % [path.get_file(), archive.error])
		return false
	var game_id := _identify(archive)
	if game_id.is_empty():
		archive_failed.emit("%s isn't one of the files listed. Drop th09.dat, th15.dat or thbgm.dat from your game folders." % path.get_file())
		return false

	var cached := _cache_path(game_id)
	archives.erase(game_id)  # releases the old cached copy so it can be overwritten
	archive = null
	DirAccess.make_dir_recursive_absolute(CACHE_DIR)
	if DirAccess.copy_absolute(path, cached) == OK:
		archive = _open_archive(cached, game_id)
	if archive == null:
		# Couldn't cache it (storage full or blocked): still usable this session.
		push_warning("GameData: couldn't keep a copy of %s; it will be asked for again next launch" % path)
		archive = _open_archive(path, game_id)
	archives[game_id] = archive
	_forget_sprites()
	graphics_are_ready = false
	_clear_graphics_cache()
	_prepare_graphics_soon()
	archive_loaded.emit(game_id, archive.file_names().size())
	return true


## Which game an opened archive belongs to, or "" if none we use.
func _identify(archive: ThDatArchive) -> String:
	for game_id in KNOWN_FILES:
		if archive.format == KNOWN_FILES[game_id][1] and archive.has_file(KNOWN_FILES[game_id][2]):
			return game_id
	return ""


func _open_archive(path: String, game_id: String) -> ThDatArchive:
	var archive := ThDatArchive.new()
	if archive.open(path) and _identify(archive) == game_id:
		return archive
	return null


func _cache_path(game_id: String) -> String:
	return "%s/%s" % [CACHE_DIR, KNOWN_FILES[game_id][0]]
