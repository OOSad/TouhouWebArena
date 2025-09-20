extends Control

# Node references
var nickname_input: LineEdit
var online_match_button: Button
var keybinds_button: Button
var buttons: Array
var current_focus_index: int = 0

func _ready():
	# Get node references
	nickname_input = $RightPanel/NicknameRow/NicknameInput
	online_match_button = $RightPanel/OnlineMatchButton
	keybinds_button = $RightPanel/KeybindsButton
	
	# Set up buttons array for navigation
	buttons = [online_match_button, keybinds_button]
	
	# Connect button signals
	online_match_button.pressed.connect(_on_OnlineMatchButton_pressed)
	keybinds_button.pressed.connect(_on_KeybindsButton_pressed)
	
	# Set initial focus
	nickname_input.grab_focus()
	
	# Hide Keybinds button as requested
	keybinds_button.visible = false

func _input(event):
	# Handle keyboard navigation
	if event.is_action_pressed("ui_down"):
		if nickname_input.has_focus():
			buttons[current_focus_index].grab_focus()
		else:
			current_focus_index = (current_focus_index + 1) % buttons.size()
			buttons[current_focus_index].grab_focus()
		get_tree().set_input_as_handled()
	
	elif event.is_action_pressed("ui_up"):
		if nickname_input.has_focus():
			# If in nickname input, move to last button
			current_focus_index = buttons.size() - 1
			buttons[current_focus_index].grab_focus()
		else:
			current_focus_index = (current_focus_index - 1) % buttons.size()
			if current_focus_index < 0:
				current_focus_index = buttons.size() - 1
			buttons[current_focus_index].grab_focus()
		get_tree().set_input_as_handled()
	
	elif event.is_action_pressed("ui_accept"):
		if online_match_button.has_focus():
			_on_OnlineMatchButton_pressed()
		elif keybinds_button.has_focus():
			_on_KeybindsButton_pressed()
		get_tree().set_input_as_handled()
	
	elif event.is_action_pressed("ui_cancel"):
		if buttons[current_focus_index].has_focus():
			nickname_input.grab_focus()
		get_tree().set_input_as_handled()

func _on_OnlineMatchButton_pressed():
	var nickname = nickname_input.text.strip_edges()
	if nickname == "":
		nickname = "Anonymous Fairy"
	
	print("Online Match pressed with nickname: ", nickname)
	# TODO: Implement matchmaking queue and transition to queue screen

func _on_KeybindsButton_pressed():
	print("Keybinds pressed")
	# TODO: Implement keybinds menu (ignored for now as requested)