extends SceneTree

func process_silhouette(src_jpg_path: String, dst_png_path: String) -> void:
	var bytes = FileAccess.get_file_as_bytes(src_jpg_path)
	if bytes.is_empty():
		var f_err = FileAccess.open("scratch/proc_err.txt", FileAccess.WRITE)
		f_err.store_string("Empty bytes from: %s\n" % src_jpg_path)
		f_err.close()
		return
	
	var img = Image.new()
	var err = img.load_jpg_from_buffer(bytes)
	if err != OK:
		var f_err = FileAccess.open("scratch/proc_err.txt", FileAccess.WRITE)
		f_err.store_string("Failed to parse jpg: %d\n" % err)
		f_err.close()
		return
	
	if img.get_width() != 896 or img.get_height() != 1200:
		img.resize(896, 1200, Image.INTERPOLATE_LANCZOS)
	
	img.convert(Image.FORMAT_RGBA8)
	var w = img.get_width()
	var h = img.get_height()
	
	# Keying:
	# White background threshold: pixels >= 0.965 brightness fade to 0.0 alpha
	# Full ink threshold: pixels <= 0.25 reach 1.0 alpha
	var white_thresh = 0.965
	var ink_thresh = 0.22
	
	for y in range(h):
		for x in range(w):
			var c = img.get_pixel(x, y)
			var lum = 0.299 * c.r + 0.587 * c.g + 0.114 * c.b
			var min_ch = minf(c.r, minf(c.g, c.b))
			var b = maxf(lum, min_ch)
			
			var a = 0.0
			if b >= white_thresh:
				a = 0.0
			elif b <= ink_thresh:
				a = 1.0
			else:
				var t = (white_thresh - b) / (white_thresh - ink_thresh)
				# Smooth cubic ease for natural sumi-e watercolor feathering
				a = clampf(t * t * (3.0 - 2.0 * t), 0.0, 1.0)
			
			c.a = a
			img.set_pixel(x, y, c)
	
	err = img.save_png(dst_png_path)
	var f = FileAccess.open("scratch/proc_log.txt", FileAccess.READ_WRITE)
	if not f:
		f = FileAccess.open("scratch/proc_log.txt", FileAccess.WRITE)
	else:
		f.seek_end()
	f.store_string("Saved %s -> err %d, exists: %s\n" % [dst_png_path, err, FileAccess.file_exists(dst_png_path)])
	f.close()

func _init() -> void:
	var merlin_src = "C:/Users/Shrine/.gemini/antigravity/brain/0110c44b-b280-4e54-9677-342482d1a92e/merlin_silhouette_1790132775094.jpg"
	var lunasa_src = "C:/Users/Shrine/.gemini/antigravity/brain/0110c44b-b280-4e54-9677-342482d1a92e/lunasa_silhouette_1790132804474.jpg"
	
	var merlin_dst = "assets/ui/menu_characters/merlin.png"
	var lunasa_dst = "assets/ui/menu_characters/lunasa.png"
	
	process_silhouette(merlin_src, merlin_dst)
	process_silhouette(lunasa_src, lunasa_dst)
	
	quit(0)

