class_name AudioService
extends Node

## AudioService / AudioManager
## Global audio manager managing sound effects playback, voice pooling, single-instance
## channels (such as lasers), and high-frequency debouncing for Touhou Web Arena.

static var instance: AudioService = null

## Sound effects by name. The game ships none: GameData fills this from the player's th09.dat
## at startup (PoFV's own se_*.wav files, byte for byte) through set_sounds().
const SOUND_NAMES: Array[String] = [
	"se_select00", "se_cancel00", "se_ok00", "se_pause", "se_plst00", "se_chargeup",
	"se_pldead00", "se_playerdead", "se_life1", "se_damage00", "se_damage01", "se_enep00",
	"se_enep01", "se_tan00", "se_warning", "se_powerup", "se_lazer00", "se_cat00", "se_gosp",
	"se_cardget", "se_charge00", "se_charge01b", "se_eterase", "se_exattack", "se_extend",
	"se_graze", "se_gun00", "se_invalid", "se_kira00", "se_kira01", "se_kira02", "se_lazer01",
	"se_power0", "se_power1", "se_slash", "se_tan01", "se_tan02", "se_timeout2", "se_timestop0",
]

## name -> AudioStream, empty until the player's th09.dat has been read.
var sounds: Dictionary = {}


## Hands over the sounds read from th09.dat, including to the dedicated players that keep a
## fixed stream (reassigning .stream mid-play would cut them off, so it's done once here).
func set_sounds(streams: Dictionary) -> void:
	sounds = streams
	if _laser_player:
		_laser_player.stream = sounds.get("se_lazer00")
	for p in _fairy_pop_pool:
		p.stream = sounds.get("se_enep00")
	if _exclusive_tan_player:
		_exclusive_tan_player.stream = sounds.get("se_tan00")

# Volume calibrations (in dB) per sound to maintain a balanced, arcade-authentic mix
const VOLUME_OFFSETS: Dictionary = {
	"se_plst00": -4.0,       # Player shooting is continuous; slightly attenuated
	"se_damage00": -2.0,     # Hit impacts can trigger frequently
	"se_damage01": -1.0,     # Critical low-health boss hits
	"se_warning": 1.0,       # Lily White alert chime
	"se_lazer00": -1.5,      # Piercing laser frequency
	"se_select00": -1.0,
	"se_ok00": 0.0,
	"se_cancel00": 0.0,
	"se_chargeup": 0.5,
	"se_charge00": 0.0,
	"se_life1": 1.0,         # Guts survival alarm
	"se_playerdead": 1.0,
	"se_pldead00": 0.0,
	"se_cat00": 0.0,         # Spellcard declaration banner
	"se_tan00": -3.5,        # Bullet barrage shot firing (2x repeat rate)
	"se_slash": 0.5,         # Youmu's Charge Slash (Danmeiken)
	"se_graze": -2.0,        # Bullet graze chime
}

# Minimum cooldown interval (in milliseconds) between rapid-fire triggers of the same sound
const THROTTLE_INTERVALS_MS: Dictionary = {
	"se_damage00": 40,       # Max ~25 hit clicks/sec across all entities
	"se_plst00": 60,         # Minimum gap between repeating shot audio
	"se_select00": 35,       # Fast menu cursor scrolling
	"se_enep00": 70,         # ~70ms debounce: allows consecutive fairy kills (~100-120ms apart) to pop and overlap, while debouncing simultaneous same-frame kills
	"se_cat00": 300,         # Action stop spellcard cast debounce
	"se_tan00": 30,          # Bullet barrage wave debounce (halved to 30ms for 2x repeat rate)
	"se_charge00": 100,      # Debounce rapid charge press/flutter
	"se_kira00": 80,         # Homing bullet launch chime debounce
	"se_exattack": 40,       # Extra attack spawn sound debounce
	"se_graze": 35,          # Rapid bullet skimming graze chime debounce
}

# Master Volume (default 10% linear / -20 dB)
const DEFAULT_MASTER_VOLUME_LINEAR: float = 0.10
const DEFAULT_MASTER_VOLUME_DB: float = -20.0
const DEFAULT_BGM_VOLUME_LINEAR: float = 1.00
const DEFAULT_SFX_VOLUME_LINEAR: float = 0.85

var master_volume: float = DEFAULT_MASTER_VOLUME_LINEAR:
	set(val):
		master_volume = clampf(val, 0.0, 1.0)
		_apply_master_volume()

var bgm_volume: float = DEFAULT_BGM_VOLUME_LINEAR:
	set(val):
		bgm_volume = clampf(val, 0.0, 1.0)
		_apply_bgm_volume()

var sfx_volume: float = DEFAULT_SFX_VOLUME_LINEAR:
	set(val):
		sfx_volume = clampf(val, 0.0, 1.0)
		_apply_sfx_volume()

# Internal playback state
var _sfx_pool: Array[AudioStreamPlayer] = []
var _sfx_pool_index: int = 0
const POOL_SIZE: int = 12

var _laser_player: AudioStreamPlayer = null
var _fairy_pop_pool: Array[AudioStreamPlayer] = []
var _fairy_pop_index: int = 0
const FAIRY_POP_POOL_SIZE: int = 4
var _exclusive_tan_player: AudioStreamPlayer = null
var _last_played_time: Dictionary = {}

# Background Music (BGM)
var _bgm_player: AudioStreamPlayer = null
## Track id (e.g. "02_spring_lane") -> looping stream, from the player's thbgm.dat via GameData.
## Music is still asked for by its old path ("res://assets/music/02_spring_lane.ogg"); the file
## name is the id, so stage data and saved replays keep working.
var music: Dictionary = {}
var _current_bgm_path: String = ""
# A fade-out in progress (stop_bgm) and the volume to restore if it's cancelled.
var _bgm_fade_tween: Tween = null
var _bgm_fade_volume_db: float = 0.0
const SETTINGS_FILE_PATH: String = "user://settings.cfg"

func _init() -> void:
	if instance == null:
		instance = self
	_setup_players()
	_apply_master_volume()
	_apply_bgm_volume()
	_apply_sfx_volume()

func _enter_tree() -> void:
	if instance == null:
		instance = self
	_apply_master_volume()
	_apply_bgm_volume()
	_apply_sfx_volume()

func _exit_tree() -> void:
	if instance == self:
		instance = null

## Calibration offsets (in dB) applied to buses to equalize punchy sound effects against background music
const BGM_BUS_CALIBRATION_DB: float = 4.0
const SFX_BUS_CALIBRATION_DB: float = -5.0

func _apply_master_volume() -> void:
	var master_idx := AudioServer.get_bus_index("Master")
	if master_idx >= 0:
		var db: float = linear_to_db(master_volume) if master_volume > 0.0001 else -80.0
		AudioServer.set_bus_volume_db(master_idx, db)

func _apply_bgm_volume() -> void:
	var bgm_idx := AudioServer.get_bus_index("BGM")
	if bgm_idx >= 0:
		var db: float = (linear_to_db(bgm_volume) + BGM_BUS_CALIBRATION_DB) if bgm_volume > 0.0001 else -80.0
		AudioServer.set_bus_volume_db(bgm_idx, db)

func _apply_sfx_volume() -> void:
	var sfx_idx := AudioServer.get_bus_index("SFX")
	if sfx_idx >= 0:
		var db: float = (linear_to_db(sfx_volume) + SFX_BUS_CALIBRATION_DB) if sfx_volume > 0.0001 else -80.0
		AudioServer.set_bus_volume_db(sfx_idx, db)

func save_settings() -> void:
	var config := ConfigFile.new()
	var _err := config.load(SETTINGS_FILE_PATH)
	config.set_value("audio", "master_volume", master_volume)
	config.set_value("audio", "bgm_volume", bgm_volume)
	config.set_value("audio", "sfx_volume", sfx_volume)
	config.save(SETTINGS_FILE_PATH)

func load_settings() -> void:
	var config := ConfigFile.new()
	var err := config.load(SETTINGS_FILE_PATH)
	if err == OK:
		if config.has_section_key("audio", "master_volume"):
			master_volume = float(config.get_value("audio", "master_volume", master_volume))
		if config.has_section_key("audio", "bgm_volume"):
			bgm_volume = float(config.get_value("audio", "bgm_volume", bgm_volume))
		if config.has_section_key("audio", "sfx_volume"):
			sfx_volume = float(config.get_value("audio", "sfx_volume", sfx_volume))

func _setup_players() -> void:
	# Ensure BGM and SFX buses exist in AudioServer
	var bgm_idx := AudioServer.get_bus_index("BGM")
	if bgm_idx < 0:
		AudioServer.add_bus()
		bgm_idx = AudioServer.get_bus_count() - 1
		AudioServer.set_bus_name(bgm_idx, "BGM")
		AudioServer.set_bus_send(bgm_idx, "Master")

	var sfx_idx := AudioServer.get_bus_index("SFX")
	if sfx_idx < 0:
		AudioServer.add_bus()
		sfx_idx = AudioServer.get_bus_count() - 1
		AudioServer.set_bus_name(sfx_idx, "SFX")
		AudioServer.set_bus_send(sfx_idx, "Master")

	if not _sfx_pool.is_empty():
		return
	# Instantiate general SFX pool
	for i in range(POOL_SIZE):
		var player := AudioStreamPlayer.new()
		player.bus = "SFX"
		player.max_polyphony = 2
		add_child(player)
		_sfx_pool.append(player)
	
	# Dedicated polyphonic player for se_lazer00 (allows lasers to overlap naturally)
	_laser_player = AudioStreamPlayer.new()
	_laser_player.bus = "SFX"
	_laser_player.max_polyphony = 12
	_laser_player.stream = sounds.get("se_lazer00")
	add_child(_laser_player)
	
	# Dedicated multi-voice pool for se_enep00 (fairy/spirit death pop)
	# Streams are pre-assigned once so playback never reassigns .stream (which stops Godot audio players)
	for i in range(FAIRY_POP_POOL_SIZE):
		var p := AudioStreamPlayer.new()
		p.bus = "SFX"
		p.stream = sounds.get("se_enep00")
		add_child(p)
		_fairy_pop_pool.append(p)
	
	# Dedicated single-voice player for rapid non-overlapping se_tan00 cadence (e.g.
	# Sakuya's Time Stop Fan dagger formation) - each new play cuts off the previous one
	# instead of stacking, unlike the round-robin general SFX pool.
	_exclusive_tan_player = AudioStreamPlayer.new()
	_exclusive_tan_player.bus = "SFX"
	_exclusive_tan_player.stream = sounds.get("se_tan00")
	add_child(_exclusive_tan_player)
	
	# Dedicated background music player
	_bgm_player = AudioStreamPlayer.new()
	_bgm_player.bus = "BGM"
	_bgm_player.max_polyphony = 1
	add_child(_bgm_player)


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_setup_players()
	load_settings()
	_apply_master_volume()
	_apply_bgm_volume()
	_apply_sfx_volume()

# -----------------------------------------------------------------------------
# Core Playback Methods
# -----------------------------------------------------------------------------

## Plays a sound effect by key name with optional volume and pitch offsets.
func _play_sfx_internal(sound_name: String, volume_db: float = 0.0, pitch_scale: float = 1.0, bypass_throttle: bool = false) -> void:
	if not sounds.has(sound_name):
		# Silent until th09.dat has been read (the file screen comes first); a name that
		# isn't a sound at all is a real mistake.
		if sound_name not in SOUND_NAMES:
			push_warning("[AudioManager] Sound '%s' not registered." % sound_name)
		return
	
	var now_ms: int = Time.get_ticks_msec()
	
	# Check throttling for rapid-fire SFX (unless explicitly bypassed)
	if not bypass_throttle and THROTTLE_INTERVALS_MS.has(sound_name):
		var min_gap: int = THROTTLE_INTERVALS_MS[sound_name]
		var last_time: int = _last_played_time.get(sound_name, 0)
		if now_ms - last_time < min_gap:
			return
	_last_played_time[sound_name] = now_ms
	
	# Special handling for single-instance laser audio
	if sound_name == "se_lazer00":
		_play_laser_internal(volume_db, pitch_scale)
		return
	
	# Special handling for single-instance fairy/enemy death pop audio
	if sound_name == "se_enep00":
		_play_fairy_pop_internal(volume_db, pitch_scale)
		return
	
	var stream: AudioStream = sounds[sound_name]
	var base_volume: float = VOLUME_OFFSETS.get(sound_name, 0.0)
	
	# Pick next available pool player via round-robin
	if _sfx_pool.is_empty():
		return
	var player: AudioStreamPlayer = _sfx_pool[_sfx_pool_index]
	_sfx_pool_index = (_sfx_pool_index + 1) % POOL_SIZE
	
	if not is_inside_tree():
		return
	
	player.stream = stream
	player.volume_db = base_volume + volume_db
	player.pitch_scale = pitch_scale
	player.play()

func _play_laser_internal(volume_db: float = 0.0, pitch_scale: float = 1.0) -> void:
	if _laser_player == null or not is_inside_tree():
		return
	
	var base_volume: float = VOLUME_OFFSETS.get("se_lazer00", -1.5)
	_laser_player.volume_db = base_volume + volume_db
	_laser_player.pitch_scale = pitch_scale
	_laser_player.play()

func _play_fairy_pop_internal(volume_db: float = 0.0, pitch_scale: float = 1.0) -> void:
	if _fairy_pop_pool.is_empty() or not is_inside_tree():
		return
	
	var player: AudioStreamPlayer = _fairy_pop_pool[_fairy_pop_index]
	_fairy_pop_index = (_fairy_pop_index + 1) % FAIRY_POP_POOL_SIZE
	
	var base_volume: float = VOLUME_OFFSETS.get("se_enep00", 0.0)
	player.volume_db = base_volume + volume_db
	player.pitch_scale = pitch_scale
	player.play()

func _play_tan_exclusive_internal(volume_db: float = 0.0, pitch_scale: float = 1.0) -> void:
	if _exclusive_tan_player == null or not is_inside_tree():
		return

	var base_volume_tan: float = VOLUME_OFFSETS.get("se_tan00", 0.0)
	_exclusive_tan_player.volume_db = base_volume_tan + volume_db
	_exclusive_tan_player.pitch_scale = pitch_scale
	_exclusive_tan_player.play()

# -----------------------------------------------------------------------------
# Static Convenience API
# -----------------------------------------------------------------------------

## Generic sound playback with optional volume, pitch, and throttle bypass
static func play_sfx(sound_name: String, volume_db: float = 0.0, pitch_scale: float = 1.0, bypass_throttle: bool = false) -> void:
	if instance != null:
		instance._play_sfx_internal(sound_name, volume_db, pitch_scale, bypass_throttle)

## Menu item highlighted or cursor moved
static func play_select() -> void:
	play_sfx("se_select00")

## Menu cancel or back out (usually X key)
static func play_cancel() -> void:
	play_sfx("se_cancel00")

## Menu confirm or enter selection (usually Z key)
static func play_confirm() -> void:
	play_sfx("se_ok00")

## Player primary shot volley
static func play_player_shot() -> void:
	play_sfx("se_plst00")

## Entity hit impact. Plays se_damage01 if target has substantial health and is <= 30% HP.
static func play_damage_hit(is_substantial_low_health: bool = false) -> void:
	if is_substantial_low_health:
		play_sfx("se_damage01")
	else:
		play_sfx("se_damage00")

## Enemy fairy or spirit defeated
static func play_enemy_death() -> void:
	play_sfx("se_enep00")

## Lily White mid-boss defeated
static func play_lily_death() -> void:
	play_sfx("se_enep01")

## Player hit or defeated
## - fatal: true when HP reaches 0 (se_playerdead)
## - guts: true when surviving on last 0.5 HP (se_life1)
## - otherwise normal hit (se_pldead00)
static func play_player_hit(fatal: bool, guts: bool = false) -> void:
	if fatal:
		play_sfx("se_playerdead")
	elif guts:
		play_sfx("se_life1")
	else:
		play_sfx("se_pldead00")

## Boss defeated, dispelled, or timeout disappearance (PoFV authentic se_tan00)
static func play_boss_defeat() -> void:
	play_sfx("se_tan00")

## Spellcard declared (Action Stop Level 2, 3, 4)
static func play_spellcard() -> void:
	play_sfx("se_cat00")

## Danmaku bullet barrage wave fired (e.g. Lily White retreat barrage)
static func play_danmaku_shot() -> void:
	play_sfx("se_tan00")

## Danmaku individual bullet spawn sound (e.g. Youmu descending strips; bypasses throttle for each spawned bullet)
static func play_bullet_spawn() -> void:
	play_sfx("se_tan00", 0.0, 1.0, true)

## Rapid non-overlapping se_tan00 cadence for staggered spawns that appear faster than the
## sound's own length (e.g. Sakuya's Time Stop Fan dagger formation) - each new play cuts off
## the previous one on a dedicated single-voice channel instead of stacking via the SFX pool.
static func play_tan_exclusive() -> void:
	if instance != null:
		instance._play_tan_exclusive_internal()

## Warning siren right when Lily White appears
static func play_lily_warning() -> void:
	play_sfx("se_warning")

## Sudden death escalation alert (PoFV authentic se_timeout2)
static func play_sudden_death_alert() -> void:
	play_sfx("se_timeout2")

## Player starts charging spell bar (se_charge00)
static func play_charge_start() -> void:
	play_sfx("se_charge00")

## Spell bar crossing thresholds (25%, 50%, 75%, 100%)
static func play_charge_threshold() -> void:
	play_sfx("se_chargeup")

## Marisa Extra Attack and Fixed Green Lasers (polyphonic overlapping)
static func play_laser() -> void:
	if instance != null:
		instance._play_laser_internal()
	else:
		play_sfx("se_lazer00")

## Dark Spirit / Phantom spawn (Youmu Extra Attack)
static func play_dark_spirit_spawn() -> void:
	play_sfx("se_gosp")

## Youmu Level 1 Charge Attack slash wave (se_slash)
static func play_slash() -> void:
	play_sfx("se_slash")

## Authentic Touhou graze sound effect when skimming bullets (se_graze)
static func play_graze() -> void:
	play_sfx("se_graze")

## Pause key pressed (Esc)
static func play_pause() -> void:
	play_sfx("se_pause")

## Lily White defeat powerup pickup item
static func play_powerup() -> void:
	play_sfx("se_powerup")

## Authentic Touhou 09 cardget pickup sound (Level 4 Spellcard drop, Extra Attack drop)
static func play_pickup_cardget() -> void:
	play_sfx("se_cardget")

## Authentic Touhou item collection chime (Bullet drop)
static func play_pickup_power() -> void:
	play_sfx("se_power0")

## Gauge Max collection chime (G pickup)
static func play_pickup_gauge_max() -> void:
	play_sfx("se_powerup")

## Dynamically sets master volume (linear 0.0 to 1.0, e.g. 0.01 = 1%)
static func set_master_volume(linear_vol: float) -> void:
	if instance != null:
		instance.master_volume = linear_vol
	else:
		var master_idx := AudioServer.get_bus_index("Master")
		if master_idx >= 0:
			var db: float = linear_to_db(clampf(linear_vol, 0.0, 1.0)) if linear_vol > 0.0001 else -80.0
			AudioServer.set_bus_volume_db(master_idx, db)

## Gets master volume (linear 0.0 to 1.0)
static func get_master_volume() -> float:
	if instance != null:
		return instance.master_volume
	return DEFAULT_MASTER_VOLUME_LINEAR

## Dynamically sets BGM volume (linear 0.0 to 1.0, e.g. 0.80 = 80%)
static func set_bgm_volume(linear_vol: float) -> void:
	if instance != null:
		instance.bgm_volume = linear_vol
	else:
		var bgm_idx := AudioServer.get_bus_index("BGM")
		if bgm_idx >= 0:
			var db: float = (linear_to_db(clampf(linear_vol, 0.0, 1.0)) + BGM_BUS_CALIBRATION_DB) if linear_vol > 0.0001 else -80.0
			AudioServer.set_bus_volume_db(bgm_idx, db)

## Gets BGM volume (linear 0.0 to 1.0)
static func get_bgm_volume() -> float:
	if instance != null:
		return instance.bgm_volume
	return DEFAULT_BGM_VOLUME_LINEAR

## Dynamically sets SFX volume (linear 0.0 to 1.0, e.g. 1.0 = 100%)
static func set_sfx_volume(linear_vol: float) -> void:
	if instance != null:
		instance.sfx_volume = linear_vol
	else:
		var sfx_idx := AudioServer.get_bus_index("SFX")
		if sfx_idx >= 0:
			var db: float = (linear_to_db(clampf(linear_vol, 0.0, 1.0)) + SFX_BUS_CALIBRATION_DB) if linear_vol > 0.0001 else -80.0
			AudioServer.set_bus_volume_db(sfx_idx, db)

## Gets SFX volume (linear 0.0 to 1.0)
static func get_sfx_volume() -> float:
	if instance != null:
		return instance.sfx_volume
	return DEFAULT_SFX_VOLUME_LINEAR

## Saves audio configuration to user://settings.cfg
static func save_audio_settings() -> void:
	if instance != null:
		instance.save_settings()

# -----------------------------------------------------------------------------
# Background Music (BGM) Methods
# -----------------------------------------------------------------------------

## Plays a specific background music track by resource path or AudioStream.
func play_bgm_track(path_or_res: Variant, volume_db: float = 0.0, loop: bool = true) -> void:
	if _bgm_player == null:
		_setup_players()
	if _bgm_player == null:
		return

	var stream: AudioStream = null
	var path_str: String = ""
	if path_or_res is AudioStream:
		stream = path_or_res
	elif path_or_res is String:
		path_str = path_or_res
		stream = music.get(DatMusic.track_id(path_str))

	if stream == null:
		# Silent until thbgm.dat has been read (the file screen comes first).
		if not music.is_empty():
			push_warning("[AudioManager] No such music: %s" % str(path_or_res))
		return

	_cancel_bgm_fade()
	if _bgm_player.playing and _current_bgm_path == path_str:
		return

	# Tracks loop from thbgm.fmt's loop point; a one-shot request plays it through once.
	if stream is AudioStreamWAV and not loop:
		stream = stream.duplicate()
		stream.loop_mode = AudioStreamWAV.LOOP_DISABLED

	_current_bgm_path = path_str
	_bgm_player.stream = stream
	_bgm_player.volume_db = volume_db
	_bgm_player.play()


## Hands over the soundtrack read from thbgm.dat.
func set_music(streams: Dictionary) -> void:
	music = streams


## Plays a random track from the soundtrack.
func play_random_bgm(volume_db: float = 0.0) -> void:
	if music.is_empty():
		return
	var ids: Array = music.keys()
	play_bgm_track("%s.ogg" % ids[randi() % ids.size()], volume_db, true)

## Stops currently playing background music with optional fade-out duration.
func stop_bgm(fade_duration: float = 0.5) -> void:
	if _bgm_player == null or not _bgm_player.playing:
		return
	_cancel_bgm_fade()
	if fade_duration <= 0.0:
		_bgm_player.stop()
		return
	var start_vol := _bgm_player.volume_db
	_bgm_fade_volume_db = start_vol
	_bgm_fade_tween = create_tween()
	_bgm_fade_tween.tween_property(_bgm_player, "volume_db", start_vol - 30.0, fade_duration)
	_bgm_fade_tween.tween_callback(func():
		_bgm_fade_tween = null
		if _bgm_player:
			_bgm_player.stop()
			_bgm_player.volume_db = start_vol
	)


## Stops a fade-out in progress and puts the volume back. Called before new music starts:
## leaving a match fades its music out, and the next scene (the main menu) starts its own
## within that fade, so the fade's final stop() would otherwise cut the new music off.
func _cancel_bgm_fade() -> void:
	if _bgm_fade_tween and _bgm_fade_tween.is_valid():
		_bgm_fade_tween.kill()
		if _bgm_player:
			_bgm_player.volume_db = _bgm_fade_volume_db
	_bgm_fade_tween = null

## Static helper to play a specific music track
static func play_music(path: String, volume_db: float = 0.0) -> void:
	if instance != null:
		instance.play_bgm_track(path, volume_db)

## Static helper to play a random music track from the music pool
static func play_random_music(volume_db: float = 0.0) -> void:
	if instance != null:
		instance.play_random_bgm(volume_db)

## Static helper to stop the background music
static func stop_music(fade_duration: float = 0.5) -> void:
	if instance != null:
		instance.stop_bgm(fade_duration)

## Returns whether background music is actively playing
static func is_music_playing() -> bool:
	if instance != null and instance._bgm_player != null:
		return instance._bgm_player.playing
	return false

