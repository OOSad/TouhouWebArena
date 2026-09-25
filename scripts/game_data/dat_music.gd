class_name DatMusic
extends RefCounted
## PoFV's soundtrack from the player's own thbgm.dat.
##
## thbgm.dat (440 MB) is raw 44.1 kHz 16-bit stereo audio, every track back to back behind a
## 16-byte "ZWAV" header. th09.dat's thbgm.fmt lists each track: 52-byte records of
## {name[16], offset, unknown, loop start (bytes), length (bytes), WAVEFORMATEX + 2 pad}.
## Only the tracks the game plays are cut out, compressed to QOA (about a fifth of the size)
## and kept in user://; thbgm.dat itself is never stored, which matters on the web build,
## where saved files live in memory.

const MAGIC: String = "ZWAV"
const FMT_FILE: String = "thbgm.fmt"
const RECORD_SIZE: int = 52
const BYTES_PER_FRAME: int = 4  # 16-bit stereo

## Bump when changing how tracks are built, so saved copies aren't reused.
const VERSION: int = 1

## Track id -> its entry in thbgm.fmt. The ids are the file names the game has always used
## for its music (res://assets/music/<id>.ogg), so stage data and saved replays keep working.
const TRACKS: Dictionary = {
	"01_flower_reflecting_mound": "th09_00.wav",       # No.1 Higan Retour
	"02_spring_lane": "th09_01.wav",                   # No.2 Colorful Path
	"04_flowering_night": "th09_02.wav",               # No.4 Flowering Night
	"06_lunatic_eyes_invisible_full_moon": "th08_12.wav",  # No.6 Invisible Full Moon
	"07_adventure_of_the_lovestruck_tomboy": "th09_05.wav",  # No.7
	"10_ancient_temple": "th07_10_b.wav",              # No.5 Ancient Temple
	"13_gensokyo_past_and_present": "th09_13.wav",     # No.13 Flower Land
}


## Track id from any of the paths the game refers to music by.
static func track_id(path: String) -> String:
	return path.get_file().get_basename()


## thbgm.fmt -> {name: {offset, loop_start, length, mix_rate, stereo}} (offsets in bytes).
static func parse_fmt(fmt: PackedByteArray) -> Dictionary:
	var tracks: Dictionary = {}
	var i := 0
	while i + RECORD_SIZE <= fmt.size():
		var name := fmt.slice(i, i + 16).get_string_from_ascii()
		if name.is_empty():
			break
		tracks[name] = {
			"offset": fmt.decode_u32(i + 16),
			"loop_start": fmt.decode_u32(i + 24),
			"length": fmt.decode_u32(i + 28),
			"stereo": fmt.decode_u16(i + 34) == 2,
			"mix_rate": fmt.decode_u32(i + 36),
		}
		i += RECORD_SIZE
	return tracks


## True if `header` (the file's first bytes) is thbgm.dat's.
static func is_thbgm(header: PackedByteArray) -> bool:
	return header.size() >= 4 and header.slice(0, 4).get_string_from_ascii() == MAGIC


## Compresses one track's raw audio (as cut from thbgm.dat) to QOA and returns the QOA data.
static func compress(pcm: PackedByteArray, entry: Dictionary) -> PackedByteArray:
	var stream := AudioStreamWAV.load_from_buffer(_wav(pcm, entry), {"compress/mode": 2})
	return stream.data if stream else PackedByteArray()


## A looping stream from saved QOA data and the track's thbgm.fmt entry.
static func make_stream(qoa: PackedByteArray, entry: Dictionary) -> AudioStreamWAV:
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_QOA
	stream.mix_rate = entry.mix_rate
	stream.stereo = entry.stereo
	stream.data = qoa
	stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
	stream.loop_begin = entry.loop_start / BYTES_PER_FRAME
	stream.loop_end = entry.length / BYTES_PER_FRAME
	return stream


## Wraps raw 16-bit PCM in a WAV header, which AudioStreamWAV.load_from_buffer expects.
static func _wav(pcm: PackedByteArray, entry: Dictionary) -> PackedByteArray:
	var channels := 2 if entry.stereo else 1
	var header := PackedByteArray()
	header.resize(44)
	header.encode_u32(0, 0x46464952)  # "RIFF"
	header.encode_u32(4, 36 + pcm.size())
	header.encode_u32(8, 0x45564157)  # "WAVE"
	header.encode_u32(12, 0x20746d66)  # "fmt "
	header.encode_u32(16, 16)
	header.encode_u16(20, 1)  # PCM
	header.encode_u16(22, channels)
	header.encode_u32(24, entry.mix_rate)
	header.encode_u32(28, entry.mix_rate * channels * 2)
	header.encode_u16(32, channels * 2)
	header.encode_u16(34, 16)
	header.encode_u32(36, 0x61746164)  # "data"
	header.encode_u32(40, pcm.size())
	return header + pcm
