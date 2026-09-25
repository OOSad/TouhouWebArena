class_name PlayfieldSpellExecutor
extends Node

## Handles spellcard declaration visuals, warnings, defensive shockwaves, screen dimming,
## defeat flash, spell background overlays, and spellcard sequence execution for a Playfield.

const SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/shockwave.tscn")
const HEAVY_SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/heavy_shockwave.tscn")
const DEFEAT_SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/defeat_shockwave.tscn")
const SPELL_BANNER_SCENE: PackedScene = preload("res://scenes/effects/spell_banner.tscn")

var playfield: Node2D = null
var effects_layer: Node2D = null
var defeat_flash: ColorRect = null
var dim_overlay: ColorRect = null
var spell_bg_overlay: Control = null
var _dim_tween: Tween = null

func setup(
	p_playfield: Node2D,
	p_effects_layer: Node2D,
	p_defeat_flash: ColorRect = null,
	p_dim_overlay: ColorRect = null,
	p_spell_bg_overlay: Control = null
) -> void:
	playfield = p_playfield
	effects_layer = p_effects_layer
	defeat_flash = p_defeat_flash
	dim_overlay = p_dim_overlay
	spell_bg_overlay = p_spell_bg_overlay

func reset() -> void:
	if defeat_flash:
		defeat_flash.modulate.a = 0.0
	if _dim_tween and _dim_tween.is_valid():
		_dim_tween.kill()
	if dim_overlay:
		dim_overlay.modulate.a = 0.0

func _get_player() -> Player:
	if playfield and "player" in playfield:
		return playfield.player as Player
	return null

func _get_character_id() -> String:
	if playfield and "character_id" in playfield:
		return playfield.character_id as String
	return "reimu"

func _get_playfield_width() -> float:
	return Playfield.PLAYFIELD_WIDTH if "PLAYFIELD_WIDTH" in Playfield else 600.0

func _get_playfield_height() -> float:
	return Playfield.PLAYFIELD_HEIGHT if "PLAYFIELD_HEIGHT" in Playfield else 960.0

func execute_charge_attack(level: int) -> void:
	var player: Player = _get_player()
	if level == 1 and player and is_instance_valid(player) and player.character_data:
		if player.character_data.charge_attack_scene:
			var charge_atk = player.character_data.charge_attack_scene.instantiate()
			if charge_atk.has_method("setup"):
				charge_atk.setup(player, playfield)
			else:
				var bullets_layer = playfield.get("bullets_layer") if playfield else null
				if bullets_layer:
					bullets_layer.add_child(charge_atk)

## Triggers the spellcard declaration banner and freeze visuals during Action Stop
func trigger_spellcard_declaration(level: int, spellcard_name: String) -> void:
	AudioService.play_spellcard()
	show_spellcard_banner(level, spellcard_name)

## Triggers the expanding heavy defensive shockwave after Action Stop unpauses
func trigger_spellcard_shockwave(level: int) -> void:
	var char_data := CharacterData.get_character(_get_character_id())
	if effects_layer and char_data and char_data.has_spellcard_shockwaves:
		var radius: float = char_data.get_shockwave_radius(level)
		if radius > 0.0:
			var heavy_wave: HeavyShockwave = HEAVY_SHOCKWAVE_SCENE.instantiate()
			var center_pos := Vector2(_get_playfield_width() / 2.0, _get_playfield_height() * 0.5)
			var player: Player = _get_player()
			heavy_wave.setup(
				player.position if player else center_pos,
				radius,
				char_data.get_shockwave_duration(level),
				char_data.get_shockwave_color(),
				playfield,
				char_data.shockwave_speed
			)
			effects_layer.add_child(heavy_wave)

## Combined helper for direct invocation
func trigger_spellcard_visuals(level: int, spellcard_name: String) -> void:
	trigger_spellcard_declaration(level, spellcard_name)
	trigger_spellcard_shockwave(level)

func show_spellcard_banner(level: int, spellcard_name: String) -> void:
	if effects_layer == null:
		return
	var char_data := CharacterData.get_character(_get_character_id())
	if char_data and char_data.spell_banner_texture:
		var banner: SpellBanner = SPELL_BANNER_SCENE.instantiate()
		effects_layer.add_child(banner)
		banner.setup(char_data.spell_banner_texture)
		banner.play_banner(0.55)
	else:
		var banner := Label.new()
		banner.process_mode = Node.PROCESS_MODE_ALWAYS
		banner.text = spellcard_name
		banner.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		banner.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		banner.custom_minimum_size = Vector2(_get_playfield_width(), 48)
		banner.position = Vector2(0.0, _get_playfield_height() * 0.38)
		banner.add_theme_font_size_override("font_size", 28)
		banner.add_theme_color_override("font_color", Color(1.0, 0.95, 0.4) if level == 4 else Color(1.0, 1.0, 1.0))
		banner.add_theme_color_override("font_outline_color", Color(0.1, 0.05, 0.15, 1.0))
		banner.add_theme_constant_override("outline_size", 6)
		effects_layer.add_child(banner)
		
		var tw := playfield.create_tween() if playfield else create_tween()
		tw.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
		tw.tween_property(banner, "position:y", banner.position.y - 30.0, 1.2).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		tw.parallel().tween_property(banner, "modulate:a", 0.0, 1.2).set_delay(0.5)
		tw.tween_callback(banner.queue_free)

## Displays the authentic Touhou 09 PoFV spellcard warning alert and red playfield wash on the target playfield during an Action Stop
func show_spellcard_warning(spell_name: String, level: int, rank: int = 1) -> void:
	if effects_layer == null:
		return
	
	var warning_container := Control.new()
	warning_container.name = "SpellcardWarningContainer"
	warning_container.process_mode = Node.PROCESS_MODE_ALWAYS
	warning_container.custom_minimum_size = Vector2(_get_playfield_width(), _get_playfield_height())
	warning_container.position = Vector2.ZERO
	
	# 1. Full-Playfield Red Flash & Atmospheric Wash Overlay
	var red_wash := ColorRect.new()
	red_wash.name = "RedWashOverlay"
	red_wash.custom_minimum_size = Vector2(_get_playfield_width(), _get_playfield_height())
	red_wash.size = Vector2(_get_playfield_width(), _get_playfield_height())
	red_wash.position = Vector2.ZERO
	red_wash.mouse_filter = Control.MOUSE_FILTER_IGNORE
	red_wash.color = Color(1.0, 0.05, 0.05, 0.65) # Initial vivid red flash
	warning_container.add_child(red_wash)
	
	# Sub-container for the text elements centered vertically in the middle of the playfield
	var text_block := Control.new()
	text_block.name = "WarningTextBlock"
	text_block.custom_minimum_size = Vector2(_get_playfield_width(), 300)
	text_block.position = Vector2(0.0, _get_playfield_height() * 0.38)
	warning_container.add_child(text_block)
	
	var font: Font = load("res://Cirno.ttf") if ResourceLoader.exists("res://Cirno.ttf") else null
	
	# 2. Full-Width Hollow WARNING Banner (Dual-layer: dark offset shadow + hollow bright red stroke)
	var warn_scale := Vector2(_get_playfield_width() / 250.0, 1.35)
	
	var warn_shadow := Label.new()
	warn_shadow.name = "WarningLabelShadow"
	warn_shadow.text = "WARNING"
	if font:
		warn_shadow.add_theme_font_override("font", font)
	warn_shadow.add_theme_font_size_override("font_size", 100)
	warn_shadow.add_theme_color_override("font_color", Color(0.4, 0.0, 0.0, 0.0))
	warn_shadow.add_theme_color_override("font_outline_color", Color(0.65, 0.0, 0.0, 0.75))
	warn_shadow.add_theme_constant_override("outline_size", 6)
	warn_shadow.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	warn_shadow.custom_minimum_size = Vector2(250.0, 110.0)
	warn_shadow.scale = warn_scale
	warn_shadow.position = Vector2(4.0, 4.0)
	text_block.add_child(warn_shadow)
	
	var warn_lbl := Label.new()
	warn_lbl.name = "WarningLabel"
	warn_lbl.text = "WARNING"
	if font:
		warn_lbl.add_theme_font_override("font", font)
	warn_lbl.add_theme_font_size_override("font_size", 100)
	warn_lbl.add_theme_color_override("font_color", Color(1.0, 0.05, 0.05, 0.18))
	warn_lbl.add_theme_color_override("font_outline_color", Color(1.0, 0.15, 0.15, 1.0))
	warn_lbl.add_theme_constant_override("outline_size", 6)
	warn_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	warn_lbl.custom_minimum_size = Vector2(250.0, 110.0)
	warn_lbl.scale = warn_scale
	warn_lbl.position = Vector2.ZERO
	text_block.add_child(warn_lbl)
	
	# 3. Subtitle: "Spell Card Attack Begin Immediately After!"
	var sub_y := 148.0
	
	var sub_shadow := Label.new()
	sub_shadow.name = "SubLabelShadow"
	sub_shadow.text = "Spell Card Attack Begin Immediately After!"
	if font:
		sub_shadow.add_theme_font_override("font", font)
	sub_shadow.add_theme_font_size_override("font_size", 24)
	sub_shadow.add_theme_color_override("font_color", Color(0.5, 0.0, 0.0, 0.8))
	sub_shadow.add_theme_color_override("font_outline_color", Color(0.25, 0.0, 0.0, 0.9))
	sub_shadow.add_theme_constant_override("outline_size", 4)
	sub_shadow.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub_shadow.custom_minimum_size = Vector2(_get_playfield_width(), 30.0)
	sub_shadow.position = Vector2(-2.0, sub_y + 2.0)
	text_block.add_child(sub_shadow)
	
	var sub_lbl := Label.new()
	sub_lbl.name = "SubLabel"
	sub_lbl.text = "Spell Card Attack Begin Immediately After!"
	if font:
		sub_lbl.add_theme_font_override("font", font)
	sub_lbl.add_theme_font_size_override("font_size", 24)
	sub_lbl.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0, 1.0))
	sub_lbl.add_theme_color_override("font_outline_color", Color(0.8, 0.05, 0.05, 1.0))
	sub_lbl.add_theme_constant_override("outline_size", 6)
	sub_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub_lbl.custom_minimum_size = Vector2(_get_playfield_width(), 30.0)
	sub_lbl.position = Vector2.ZERO + Vector2(0.0, sub_y)
	text_block.add_child(sub_lbl)
	
	# 4. Class & Rank Line: "Class: [Fairy/Witch/Dragon] Level [Rank]"
	var tier_name: String = "Fairy"
	if level == 3:
		tier_name = "Witch"
	elif level >= 4:
		tier_name = "Dragon"
	var class_rank_text: String = "Class: %s Level %d" % [tier_name, rank]
	var rank_y := 194.0
	
	var rank_shadow := Label.new()
	rank_shadow.name = "RankLabelShadow"
	rank_shadow.text = class_rank_text
	if font:
		rank_shadow.add_theme_font_override("font", font)
	rank_shadow.add_theme_font_size_override("font_size", 28)
	rank_shadow.add_theme_color_override("font_color", Color(0.5, 0.0, 0.0, 0.8))
	rank_shadow.add_theme_color_override("font_outline_color", Color(0.25, 0.0, 0.0, 0.9))
	rank_shadow.add_theme_constant_override("outline_size", 5)
	rank_shadow.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	rank_shadow.custom_minimum_size = Vector2(_get_playfield_width() - 40.0, 36.0)
	rank_shadow.position = Vector2(-2.0, rank_y + 2.0)
	text_block.add_child(rank_shadow)
	
	var rank_lbl := Label.new()
	rank_lbl.name = "RankLabel"
	rank_lbl.text = class_rank_text
	if font:
		rank_lbl.add_theme_font_override("font", font)
	rank_lbl.add_theme_font_size_override("font_size", 28)
	rank_lbl.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0, 1.0))
	rank_lbl.add_theme_color_override("font_outline_color", Color(0.9, 0.08, 0.08, 1.0))
	rank_lbl.add_theme_constant_override("outline_size", 7)
	rank_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	rank_lbl.custom_minimum_size = Vector2(_get_playfield_width() - 40.0, 36.0)
	rank_lbl.position = Vector2.ZERO + Vector2(0.0, rank_y)
	text_block.add_child(rank_lbl)
	
	effects_layer.add_child(warning_container)
	
	# Animation: flash settling into atmospheric red wash, then smooth fade-out on unpaused tween
	var tw := playfield.create_tween() if playfield else create_tween()
	tw.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	tw.tween_property(red_wash, "color:a", 0.42, 0.10).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_interval(0.50)
	tw.tween_property(warning_container, "modulate:a", 0.0, 0.20).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_callback(warning_container.queue_free)

## Flashes the entire playfield red for a couple of frames upon lethal KO
func play_defeat_flash() -> void:
	if defeat_flash:
		defeat_flash.process_mode = Node.PROCESS_MODE_ALWAYS
		defeat_flash.modulate.a = 1.0
		var tree: SceneTree = playfield.get_tree() if playfield and playfield.is_inside_tree() else null
		var tw: Tween = tree.create_tween() if tree else (playfield.create_tween() if playfield and playfield.is_inside_tree() else null)
		if tw:
			tw.tween_interval(0.05)
			tw.tween_property(defeat_flash, "modulate:a", 0.0, 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

## Fades this field's whole rendered view toward target_alpha of a black overlay
func set_dim(target_alpha: float, duration: float) -> void:
	if dim_overlay == null:
		return
	if _dim_tween and _dim_tween.is_valid():
		_dim_tween.kill()
	dim_overlay.process_mode = Node.PROCESS_MODE_ALWAYS
	if duration <= 0.0:
		dim_overlay.modulate.a = target_alpha
		return
	var tree: SceneTree = playfield.get_tree() if playfield and playfield.is_inside_tree() else null
	_dim_tween = tree.create_tween() if tree else (playfield.create_tween() if playfield and playfield.is_inside_tree() else null)
	if _dim_tween == null:
		dim_overlay.modulate.a = target_alpha
		return
	_dim_tween.tween_property(dim_overlay, "modulate:a", target_alpha, duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

## Spawns the cosmetic faint red shockwave bursting out from the player's defeat position
func spawn_defeat_shockwave(death_pos: Vector2) -> void:
	if effects_layer:
		var wave = DEFEAT_SHOCKWAVE_SCENE.instantiate()
		wave.setup(death_pos)
		effects_layer.add_child(wave)

func spawn_hit_shockwave(landing_pos: Vector2) -> void:
	if effects_layer:
		var heavy_wave: HeavyShockwave = HEAVY_SHOCKWAVE_SCENE.instantiate()
		heavy_wave.setup(landing_pos)
		effects_layer.add_child(heavy_wave)

func execute_spellcard(spell_data: SpellcardData, rank: int, custom_origin: Vector2 = Vector2.ZERO) -> void:
	if spell_data == null or playfield == null:
		return
	var origin: Vector2 = custom_origin
	if origin == Vector2.ZERO:
		origin = Vector2(_get_playfield_width() / 2.0, 110.0)
	
	var r_seed: int = playfield.get("_round_seed") if "_round_seed" in playfield else 99991
	var cast_count: int = playfield.get("_spellcard_cast_count") if "_spellcard_cast_count" in playfield else 0
	var cast_seed: int = r_seed + cast_count * 6271
	if "_spellcard_cast_count" in playfield:
		playfield.set("_spellcard_cast_count", cast_count + 1)
	spell_data.execute_sequence(playfield, origin, rank, cast_seed)

func activate_spell_bg(character_name: String) -> void:
	if spell_bg_overlay == null and playfield:
		spell_bg_overlay = playfield.get_node_or_null("%SpellBackgroundOverlay")
	var c_data := CharacterData.get_character(character_name)
	if spell_bg_overlay and c_data:
		spell_bg_overlay.activate(c_data)

func deactivate_spell_bg(duration: float = 0.8) -> void:
	if spell_bg_overlay == null and playfield:
		spell_bg_overlay = playfield.get_node_or_null("%SpellBackgroundOverlay")
	if spell_bg_overlay:
		spell_bg_overlay.deactivate(duration)

