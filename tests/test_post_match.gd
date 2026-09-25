extends SceneTree

const VictoryDialogueDB = preload("res://scripts/resources/victory_dialogue_db.gd")
const PostMatch = preload("res://scenes/post_match/post_match.gd")

func _init() -> void:
	print("--- BEGIN POST-MATCH SCREEN TEST ---")
	
	# 1. Test VictoryDialogueDB
	var reimu_quote := VictoryDialogueDB.get_random_quote("reimu", "marisa")
	assert(reimu_quote.has("text") and not reimu_quote["text"].is_empty(), "Quote must have non-empty text")
	assert(reimu_quote.has("winner_expression"), "Quote must specify winner expression")
	assert(reimu_quote["winner_expression"] >= 0 and reimu_quote["winner_expression"] <= 8, "Expression must be in 0..8")
	print("PASS: VictoryDialogueDB returned valid quote: '", reimu_quote["text"].substr(0, 35), "...' (expression %d)" % reimu_quote["winner_expression"])
	
	var marisa_quote := VictoryDialogueDB.get_random_quote("marisa", "reimu")
	assert(marisa_quote.has("text") and not marisa_quote["text"].is_empty(), "Marisa quote must have non-empty text")
	assert(marisa_quote.has("winner_expression"), "Marisa quote must specify winner expression")
	print("PASS: Marisa vs Reimu authentic quote returned: '", marisa_quote["text"].substr(0, 35), "...'")
	
	# 2. Test Portrait Slicing. Faces come from the player's th09.dat: build every
	# character's faces the way GameData does at startup, from the copy the file screen saves.
	var archive := ThDatArchive.new()
	assert(archive.open("user://game_files/th09.dat"), "Needs th09.dat: run the game once and drop it on the file screen")
	var kept: Array[CharacterData] = []
	for character in CharacterSprites.PLAYER_SHEETS:
		var pl: String = CharacterSprites.PLAYER_SHEETS[character]
		var anm := ThAnm.new()
		anm.parse(archive.extract("pl%s.anm" % pl))
		kept.append(CharacterSprites.apply_faces(character, CharacterSprites.build_faces(anm, pl)))
	var reimu_tex := PostMatch.get_portrait_texture("reimu", 2)
	assert(reimu_tex != null, "Portrait texture must not be null")
	assert(reimu_tex.get_size() == Vector2(256, 320), "Portrait texture must be 256x320")
	print("PASS: Reimu portrait AtlasTexture sliced at size: ", reimu_tex.get_size())
	
	var marisa_tex := PostMatch.get_portrait_texture("marisa", 8)
	assert(marisa_tex != null, "Marisa portrait must not be null")
	assert(marisa_tex.get_size() == Vector2(256, 320), "Marisa defeated portrait sliced at 256x320")
	print("PASS: Marisa defeated portrait sliced successfully")
	
	# 3. Test Scene Instantiation & Node Setup
	var pm_scene := load("res://scenes/post_match/post_match.tscn") as PackedScene
	assert(pm_scene != null, "PostMatch scene must load successfully")
	var pm: PostMatch = pm_scene.instantiate()
	root.add_child(pm)
	pm._ready()
	
	assert(pm.p1_portrait != null, "P1Portrait node must exist")
	assert(pm.p2_portrait != null, "P2Portrait node must exist")
	assert(pm.dialogue_box != null, "DialogueBox node must exist")
	assert(pm.p1_badge != null, "P1 badge node must exist")
	assert(pm.p2_badge != null, "P2 badge node must exist")
	print("PASS: All critical UI nodes exist, winner/loser badges present")
	
	# Verify cursor stability (all cursor labels stay visible=true so HBox layout never collapses)
	for cur in pm.p1_cursor_labels:
		assert(cur.visible == true, "P1 cursor labels must remain visible=true to avoid HBox collapse")
	for cur in pm.p2_cursor_labels:
		assert(cur.visible == true, "P2 cursor labels must remain visible=true to avoid HBox collapse")
	print("PASS: Cursor layout stability verified (no HBox collapse)")
	
	# 4. Check Winner & Loser Visual Modulates
	# Default setup: P1 Reimu (Winner), P2 Marisa (Loser)
	assert(pm.p1_portrait.modulate == Color.WHITE, "Winner portrait must be fully lit (Color.WHITE)")
	assert(pm.p2_portrait.modulate.v < 0.6, "Loser portrait must be darkened")
	assert(pm.p2_portrait.flip_h == true, "P2 portrait must be flipped horizontally to face inward")
	print("PASS: Winner is fully lit and Loser is darkened with inward horizontal flip")
	
	# Test flipping match result (P2 Winner, P1 Loser)
	pm.setup_match(2, 1, "reimu", "marisa")
	assert(pm.p2_portrait.modulate == Color.WHITE, "P2 must be fully lit when Winner")
	assert(pm.p1_portrait.modulate.v < 0.6, "P1 must be darkened when Loser")
	print("PASS: P2 Winner setup verified with darkened P1 Loser")
	
	# Test Youmu P2 Winner vs Marisa P1 Loser (verifying Youmu's portraits and dialogue)
	pm.setup_match(2, 1, "Marisa Kirisame", "Youmu Konpaku")
	assert(pm.p1_character == "marisa", "P1 character must normalize to marisa")
	assert(pm.p2_character == "youmu", "P2 character must normalize to youmu")
	assert(pm.p1_name_label.text == "1P: MARISA", "1P label must be MARISA")
	assert(pm.p2_name_label.text == "2P: YOUMU", "2P label must be YOUMU")
	assert(pm.speaker_label.text == "[ YOUMU KONPAKU ]", "Speaker label must be YOUMU KONPAKU")
	assert("investigate" in pm.dialogue_text_label.text, "Dialogue must be Youmu's authentic quote against Marisa")
	assert(pm.p2_portrait.texture != null, "Youmu portrait texture must not be null")
	print("PASS: Youmu P2 Winner vs Marisa P1 Loser properly displays Youmu on results screen!")
	
	# 5. Handshake Logic
	pm.p1_selected_option = PostMatch.MenuOption.REMATCH
	pm.p2_selected_option = PostMatch.MenuOption.REMATCH
	pm.p1_confirmed = true
	pm.p2_confirmed = true
	pm.is_resolving_handshake = false
	pm._check_handshake()
	assert(pm.is_resolving_handshake == true, "Both rematch must resolve handshake")
	print("PASS: Both Rematch resolves handshake")
	
	pm.p1_selected_option = PostMatch.MenuOption.CHANGE_CHARACTER
	pm.p2_selected_option = PostMatch.MenuOption.REMATCH
	pm.is_resolving_handshake = false
	pm._check_handshake()
	assert(pm.is_resolving_handshake == true, "Change Character overrides Rematch")
	print("PASS: Change Character handshake priority verified")
	
	# Single player confirmation test (no waiting for other player if selecting Change Character or Main Menu)
	pm.p1_selected_option = PostMatch.MenuOption.CHANGE_CHARACTER
	pm.p1_confirmed = true
	pm.p2_confirmed = false
	pm.is_resolving_handshake = false
	pm._check_handshake()
	assert(pm.is_resolving_handshake == true, "Single player Change Character must immediately resolve handshake")
	
	pm.p1_selected_option = PostMatch.MenuOption.MAIN_MENU
	pm.p1_confirmed = true
	pm.p2_confirmed = false
	pm.is_resolving_handshake = false
	pm._check_handshake()
	assert(pm.is_resolving_handshake == true, "Single player Main Menu must immediately resolve handshake")
	print("PASS: Single player Change Character and Main Menu immediate resolution verified")
	
	# 6. Test Remote RPC Callbacks
	pm.is_resolving_handshake = false
	pm.p1_confirmed = false
	pm.p2_confirmed = false
	pm._on_remote_cursor_updated(2, PostMatch.MenuOption.MAIN_MENU)
	assert(pm.p2_selected_option == PostMatch.MenuOption.MAIN_MENU, "P2 cursor should update from remote callback")
	pm._on_remote_confirmed(2, PostMatch.MenuOption.MAIN_MENU)
	assert(pm.p2_confirmed == true, "P2 confirmed flag should be true from remote callback")
	print("PASS: Remote cursor and confirm callbacks properly update state")
	
	pm.queue_free()
	print("--- ALL POST-MATCH TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
