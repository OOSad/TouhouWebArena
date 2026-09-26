class_name MainMenuSettings
extends Node

## The main menu's Settings modal: Audio tab (master / music / sound volume) and Controls tab
## (UDoALG vs PoFV control style, with the key list relabelled to match).

signal closed

const TAB_ACTIVE := Color(1.0, 0.9, 0.4, 1.0)
const TAB_IDLE := Color(0.7, 0.75, 0.85, 1.0)

var modal: PanelContainer
var _close_btn: Button
var _tab_audio_btn: Button
var _tab_controls_btn: Button
var _audio_section: VBoxContainer
var _controls_section: VBoxContainer
var _master_slider: HSlider
var _master_val_label: Label
var _bgm_slider: HSlider
var _bgm_val_label: Label
var _sfx_slider: HSlider
var _sfx_val_label: Label
var _control_style_btn: Button
var _key_shoot_label: Label
var _desc_shoot_label: Label
var _key_charge_label: Label
var _desc_charge_label: Label
var _key_bomb_label: Label
var _desc_bomb_label: Label
var _key_cancel_label: Label

var _last_sfx_preview_time: int = 0

func setup(menu: Control) -> void:
	modal = menu.get_node_or_null("%SettingsModal")
	_close_btn = menu.get_node_or_null("%CloseSettingsBtn")
	_tab_audio_btn = menu.get_node_or_null("%TabAudioBtn")
	_tab_controls_btn = menu.get_node_or_null("%TabControlsBtn")
	_audio_section = menu.get_node_or_null("%AudioSection")
	_controls_section = menu.get_node_or_null("%ControlsSection")
	_master_slider = menu.get_node_or_null("%MasterSlider")
	_master_val_label = menu.get_node_or_null("%MasterValLabel")
	_bgm_slider = menu.get_node_or_null("%BgmSlider")
	_bgm_val_label = menu.get_node_or_null("%BgmValLabel")
	_sfx_slider = menu.get_node_or_null("%SfxSlider")
	_sfx_val_label = menu.get_node_or_null("%SfxValLabel")
	_control_style_btn = menu.get_node_or_null("%ControlStyleBtn")
	_key_shoot_label = menu.get_node_or_null("%KeyShoot")
	_desc_shoot_label = menu.get_node_or_null("%DescShoot")
	_key_charge_label = menu.get_node_or_null("%KeyCharge")
	_desc_charge_label = menu.get_node_or_null("%DescCharge")
	_key_bomb_label = menu.get_node_or_null("%KeyBomb")
	_desc_bomb_label = menu.get_node_or_null("%DescBomb")
	_key_cancel_label = menu.get_node_or_null("%KeyCancel")

	if _close_btn:
		_close_btn.pressed.connect(_on_close_pressed)
		_close_btn.focus_entered.connect(_play_focus_sound)
	if _tab_audio_btn:
		_tab_audio_btn.pressed.connect(_on_tab_audio_pressed)
	if _tab_controls_btn:
		_tab_controls_btn.pressed.connect(_on_tab_controls_pressed)
	if _control_style_btn:
		_control_style_btn.pressed.connect(_on_control_style_btn_pressed)
		_control_style_btn.focus_entered.connect(_play_focus_sound)
	if _master_slider:
		_master_slider.value_changed.connect(_on_master_slider_changed)
	if _bgm_slider:
		_bgm_slider.value_changed.connect(_on_bgm_slider_changed)
	if _sfx_slider:
		_sfx_slider.value_changed.connect(_on_sfx_slider_changed)
	if modal:
		modal.visible = false

func is_open() -> bool:
	return modal != null and modal.visible

func open() -> void:
	_sync_sliders()
	_sync_control_style_ui()
	_show_tab(true)
	if modal:
		modal.visible = true
	if _master_slider:
		_master_slider.grab_focus()

## Hides the modal and tells the menu, which puts focus back on its Settings button.
func close() -> void:
	if modal:
		modal.visible = false
	closed.emit()

func _on_close_pressed() -> void:
	AudioService.play_cancel()
	close()

func _sync_sliders() -> void:
	_set_slider(_master_slider, _master_val_label, AudioService.get_master_volume())
	_set_slider(_bgm_slider, _bgm_val_label, AudioService.get_bgm_volume())
	_set_slider(_sfx_slider, _sfx_val_label, AudioService.get_sfx_volume())

func _set_slider(slider: HSlider, value_label: Label, linear: float) -> void:
	if slider == null:
		return
	var val: float = round(linear * 100.0)
	slider.set_value_no_signal(val)
	if value_label:
		value_label.text = "%d%%" % int(val)

func _show_tab(is_audio: bool) -> void:
	if _audio_section:
		_audio_section.visible = is_audio
	if _controls_section:
		_controls_section.visible = not is_audio
	if _tab_audio_btn:
		_tab_audio_btn.add_theme_color_override("font_color", TAB_ACTIVE if is_audio else TAB_IDLE)
	if _tab_controls_btn:
		_tab_controls_btn.add_theme_color_override("font_color", TAB_ACTIVE if not is_audio else TAB_IDLE)

func _on_tab_audio_pressed() -> void:
	AudioService.play_select()
	_show_tab(true)
	if _master_slider:
		_master_slider.grab_focus()

func _on_tab_controls_pressed() -> void:
	AudioService.play_select()
	_sync_control_style_ui()
	_show_tab(false)
	if _control_style_btn:
		_control_style_btn.grab_focus()
	elif _close_btn:
		_close_btn.grab_focus()

func _game_manager() -> Node:
	return get_node_or_null("/root/GameManager") if is_inside_tree() else null

func _on_control_style_btn_pressed() -> void:
	AudioService.play_confirm()
	var gm := _game_manager()
	var current: String = gm.get_control_style() if (gm and gm.has_method("get_control_style")) else "udoalg"
	var new_style := "pofv" if current == "udoalg" else "udoalg"
	if gm and gm.has_method("set_control_style"):
		gm.set_control_style(new_style)
	_sync_control_style_ui()

func _sync_control_style_ui() -> void:
	var gm := _game_manager()
	var is_pofv: bool = gm.is_pofv_controls() if (gm and gm.has_method("is_pofv_controls")) else false
	if _control_style_btn:
		_control_style_btn.text = "PoFV-Style (Classic Touhou 09)" if is_pofv else "UDoALG-Style (Hold Z Shoot)"
	if _key_shoot_label:
		_key_shoot_label.text = "Tap Z" if is_pofv else "Z"
	if _desc_shoot_label:
		_desc_shoot_label.text = "Shoot (Mash for continuous stream)" if is_pofv else "Shoot / Select Item (Hold to fire)"
	if _key_charge_label:
		_key_charge_label.text = "Hold Z" if is_pofv else "Hold X"
	if _desc_charge_label:
		_desc_charge_label.text = "Charge Spell Gauge (Lv 1-4)"
	if _key_bomb_label:
		_key_bomb_label.text = "X" if is_pofv else "C"
	if _desc_bomb_label:
		_desc_bomb_label.text = "Panic Bomb (Highest Tier Spell)"
	if _key_cancel_label:
		_key_cancel_label.text = "Esc" if is_pofv else "Esc  /  X"

func _on_master_slider_changed(val: float) -> void:
	AudioService.set_master_volume(val / 100.0)
	if _master_val_label:
		_master_val_label.text = "%d%%" % int(val)
	AudioService.save_audio_settings()

func _on_bgm_slider_changed(val: float) -> void:
	AudioService.set_bgm_volume(val / 100.0)
	if _bgm_val_label:
		_bgm_val_label.text = "%d%%" % int(val)
	AudioService.save_audio_settings()

func _on_sfx_slider_changed(val: float) -> void:
	AudioService.set_sfx_volume(val / 100.0)
	if _sfx_val_label:
		_sfx_val_label.text = "%d%%" % int(val)
	AudioService.save_audio_settings()

	# Play subtle test chime debounced to max ~80ms
	var now: int = Time.get_ticks_msec()
	if now - _last_sfx_preview_time > 80:
		_last_sfx_preview_time = now
		AudioService.play_select()

func _play_focus_sound() -> void:
	AudioService.play_select()
