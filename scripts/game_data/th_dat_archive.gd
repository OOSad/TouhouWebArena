class_name ThDatArchive
extends RefCounted
## Reads a Touhou data archive that the player supplies from their own copy.
## Nothing from these archives ships with the game; this only unpacks it at runtime.
## Ported from thtk (https://github.com/thpatch/thtk). Two formats:
##
## "PBGZ" (thdat08: TH08, TH09): every entry is LZSS compressed, then starts with "edz" +
## a type letter that selects the cipher parameters for the rest of the entry.
##
## "THA1" (thdat95: TH9.5 onwards, here TH15+): the header is encrypted too. Each entry
## is encrypted with one of eight parameter sets picked from its name, then LZSS
## compressed unless storing it compressed wouldn't have saved anything.

const HEADER_SIZE: int = 16

# Entry type letter -> [key, step, block, limit]. th09 table from thtk dattypes.h.
const TH09_CRYPT_PARAMS: Dictionary = {
	"M": [0x1b, 0x37, 0x40, 0x2800],  # .msg
	"T": [0x51, 0xe9, 0x40, 0x3000],  # .txt
	"A": [0xc1, 0x51, 0x400, 0x400],  # .anm
	"J": [0x03, 0x19, 0x400, 0x400],  # .jpg
	"E": [0xab, 0xcd, 0x200, 0x1000], # .ecl
	"W": [0x12, 0x34, 0x400, 0x400],  # .wav
	"-": [0x35, 0x97, 0x80, 0x2800],  # everything else
	"*": [0x99, 0x37, 0x400, 0x1000],
}

# [key, step, block, limit], picked by the entry name's byte sum & 7. TH14 onwards
# (th14_crypt_params in thtk dattypes.h).
const TH14_CRYPT_PARAMS: Array = [
	[0x1b, 0x73, 0x100, 0x3800],
	[0x12, 0x43, 0x200, 0x3e00],
	[0x35, 0x79, 0x400, 0x3c00],
	[0x03, 0x91, 0x80, 0x6400],
	[0xab, 0xdc, 0x80, 0x7000],
	[0x51, 0x9e, 0x100, 0x4000],
	[0xc1, 0x15, 0x400, 0x2c00],
	[0x99, 0x7d, 0x80, 0x4400],
]

const LZSS_DICT_MASK: int = 0x1fff
const LZSS_MIN_MATCH: int = 3

var error: String = ""
## "PBGZ" or "THA1" once opened.
var format: String = ""

# Kept open so entries are read on demand instead of holding the whole archive in memory.
var _file: FileAccess
# name -> {offset, size, zsize}, in archive order.
var _entries: Dictionary = {}


## Parses the archive's file table. Returns false and sets `error` if it isn't one.
func open(path: String) -> bool:
	_entries.clear()
	format = ""
	_file = FileAccess.open(path, FileAccess.READ)
	if _file == null:
		error = "couldn't be opened"
		return false
	var length := _file.get_length()
	if length < HEADER_SIZE:
		error = "not a Touhou data archive"
		return false
	var header := _file.get_buffer(HEADER_SIZE)
	if header.slice(0, 4).get_string_from_ascii() == "PBGZ":
		format = "PBGZ"
		return _open_pbgz(header, length)
	header = decrypt(header, 0x1b, 0x37, HEADER_SIZE, HEADER_SIZE)
	if header.slice(0, 4).get_string_from_ascii() == "THA1":
		format = "THA1"
		return _open_tha1(header, length)
	error = "not a Touhou data archive"
	return false


func _open_pbgz(header: PackedByteArray, length: int) -> bool:
	var fields := decrypt(header.slice(4), 0x1b, 0x37, HEADER_SIZE - 4, 0x400)
	var count: int = (fields.decode_u32(0) - 123456) & 0xffffffff
	var table_offset: int = (fields.decode_u32(4) - 345678) & 0xffffffff
	var table_size: int = (fields.decode_u32(8) - 567891) & 0xffffffff
	if table_offset >= length or count <= 0 or count > 100000:
		error = "archive header is damaged"
		return false
	_file.seek(table_offset)
	var table := unlzss(decrypt(_file.get_buffer(length - table_offset), 0x3e, 0x9b, 0x80, 0x400), table_size)
	# Entries: name, NUL, then offset / size / zero.
	return _read_table(table, count, table_offset, false)


func _open_tha1(header: PackedByteArray, length: int) -> bool:
	var table_size: int = (header.decode_u32(4) - 123456789) & 0xffffffff
	var table_zsize: int = (header.decode_u32(8) - 987654321) & 0xffffffff
	var count: int = (header.decode_u32(12) - 135792468) & 0xffffffff
	if table_zsize >= length or count <= 0 or count > 100000:
		error = "archive header is damaged"
		return false
	var table_offset := length - table_zsize
	_file.seek(table_offset)
	var table := unlzss(decrypt(_file.get_buffer(table_zsize), 0x3e, 0x9b, 0x80, table_zsize), table_size)
	# Entries: name padded with NULs to the next multiple of 4, then offset / size / zero.
	return _read_table(table, count, table_offset, true)


## Compressed sizes aren't stored: each entry runs up to the next one, the last up to the table.
func _read_table(table: PackedByteArray, count: int, table_offset: int, padded_names: bool) -> bool:
	var ptr := 0
	var ordered: Array[Dictionary] = []
	for i in count:
		var name_end := table.find(0, ptr)
		var fields := name_end + 1
		if padded_names:
			var name_length := name_end - ptr
			fields = ptr + name_length + (4 - name_length % 4)
		if name_end < 0 or fields + 12 > table.size():
			error = "archive file table is damaged"
			return false
		ordered.append({
			"name": table.slice(ptr, name_end).get_string_from_ascii(),
			"offset": table.decode_u32(fields),
			"size": table.decode_u32(fields + 4),
		})
		ptr = fields + 12
	for i in ordered.size():
		var next_offset: int = ordered[i + 1].offset if i + 1 < ordered.size() else table_offset
		ordered[i].zsize = next_offset - ordered[i].offset
		_entries[ordered[i].name] = ordered[i]
	error = ""
	return true


func file_names() -> PackedStringArray:
	return PackedStringArray(_entries.keys())


func has_file(name: String) -> bool:
	return _entries.has(name)


## Unpacks one file. Returns an empty array and sets `error` on failure.
func extract(name: String) -> PackedByteArray:
	if not _entries.has(name):
		error = "no file named %s" % name
		return PackedByteArray()
	var entry: Dictionary = _entries[name]
	_file.seek(entry.offset)
	var stored := _file.get_buffer(entry.zsize)
	if format == "THA1":
		var p: Array = TH14_CRYPT_PARAMS[_name_key(name)]
		var decrypted := decrypt(stored, p[0], p[1], p[2], p[3])
		return decrypted if entry.zsize == entry.size else unlzss(decrypted, entry.size)

	var raw := unlzss(stored, entry.size)
	if raw.size() < 4 or raw.slice(0, 3).get_string_from_ascii() != "edz":
		error = "%s is damaged" % name
		return PackedByteArray()
	var type := char(raw[3])
	if not TH09_CRYPT_PARAMS.has(type):
		error = "%s has an unknown type" % name
		return PackedByteArray()
	var q: Array = TH09_CRYPT_PARAMS[type]
	return decrypt(raw.slice(4), q[0], q[1], q[2], q[3])


## Which of the eight THA1 parameter sets a file uses: its name's byte sum, low 3 bits.
static func _name_key(name: String) -> int:
	var total := 0
	for b in name.to_ascii_buffer():
		total += b
	return total & 7


## thtk th_decrypt: XOR with a stepping key while de-interleaving each block,
## applied only to the first `limit` bytes.
static func decrypt(data: PackedByteArray, key: int, step: int, block: int, limit: int) -> PackedByteArray:
	var out := data.duplicate()
	var size := data.size()
	if size < block >> 2:
		size = 0
	else:
		size -= (int(size % block < block >> 2) * size) % block + size % 2
	if limit % block != 0:
		limit += block - limit % block
	var end := mini(size, limit)
	var increment := (block >> 1) + (block & 1)

	var pos := 0
	while pos < end:
		if end - pos < block:
			block = end - pos
			increment = (block >> 1) + (block & 1)
		var o := block - 1
		var i := pos
		while o > 0:
			out[pos + o] = data[i] ^ key
			out[pos + o - 1] = data[i + increment] ^ ((key + step * increment) & 0xff)
			o -= 2
			i += 1
			key = (key + step) & 0xff
		if block & 1:
			out[pos + o] = data[i] ^ key
			key = (key + step) & 0xff
		key = (key + step * increment) & 0xff
		pos += block
	return out


## thtk th_unlzss: 8 KiB dictionary, MSB-first bits. 1 = literal byte,
## 0 = 13-bit dictionary offset (0 ends the stream) + 4-bit length.
static func unlzss(src: PackedByteArray, out_size: int) -> PackedByteArray:
	var out := PackedByteArray()
	out.resize(out_size)
	var dict := PackedByteArray()
	dict.resize(LZSS_DICT_MASK + 1)
	var head := 1
	var written := 0
	var bitbuf := 0
	var bits := 0
	var p := 0
	var n := src.size()

	while written < out_size:
		# The longest token is 18 bits, so top up before decoding it.
		while bits < 18:
			bitbuf = (bitbuf << 8) | (src[p] if p < n else 0)
			p += 1
			bits += 8
		bits -= 1
		if (bitbuf >> bits) & 1:
			bits -= 8
			var c := (bitbuf >> bits) & 0xff
			out[written] = c
			written += 1
			dict[head] = c
			head = (head + 1) & LZSS_DICT_MASK
		else:
			bits -= 13
			var match_offset := (bitbuf >> bits) & LZSS_DICT_MASK
			if match_offset == 0:
				break
			bits -= 4
			var match_len := ((bitbuf >> bits) & 0xf) + LZSS_MIN_MATCH
			for k in match_len:
				if written >= out_size:
					break
				var c := dict[(match_offset + k) & LZSS_DICT_MASK]
				out[written] = c
				written += 1
				dict[head] = c
				head = (head + 1) & LZSS_DICT_MASK
		bitbuf &= (1 << bits) - 1

	if written < out_size:
		out.resize(written)
	return out
