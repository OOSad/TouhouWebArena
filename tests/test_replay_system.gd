extends SceneTree

const ReplayManagerScript = preload("res://scripts/global/replay_manager.gd")

func _init() -> void:
	print("--- BEGIN REPLAY SYSTEM TEST ---")
	
	var rm = ReplayManagerScript.new()
	root.add_child(rm)
	
	# 1. Test Recording Lifecycle
	rm.start_recording(
		"Reimu Player",
		"Marisa Player",
		"reimu",
		"marisa",
		"bamboo_road",
		"res://assets/music/02_spring_lane.ogg",
		12345
	)
	
	assert(rm.is_recording == true, "ReplayManager should be recording")
	
	# Record simulated player frames
	rm.record_player_state(0.1, 1, Vector2(100, 200), 0, false, false, true, 0.0, 1.0)
	rm.record_player_state(0.1, 2, Vector2(500, 200), 0, false, false, true, 0.0, 1.0)
	
	# Record events
	rm.record_event({"t": 0.5, "type": "shot_fired", "p": 1})
	rm.record_event({"t": 1.2, "type": "health_update", "p": 2, "hp": 4.0})
	rm.record_event({"t": 2.5, "type": "round_ended", "winner": 1, "round": 1})
	
	var recorded_data: Dictionary = rm.finish_recording(1, 2, 0, 45.5)
	assert(rm.is_recording == false, "ReplayManager should stop recording")
	assert(recorded_data.is_empty() == false, "Recorded data must not be empty")
	assert(recorded_data["winner_num"] == 1, "Winner num must match")
	assert(recorded_data["duration"] == 45.5, "Duration must match")
	assert(recorded_data["events"].size() == 5, "Should have 5 recorded events (2 states + 3 events)")
	print("PASS: Recording lifecycle and event capture verified")
	
	# 2. Test Saving to Slot
	var test_slot: int = 10
	var save_success: bool = rm.save_to_slot(test_slot, recorded_data)
	assert(save_success == true, "Save to slot 10 should succeed")
	
	var summary: Dictionary = rm.get_slot_summary(test_slot)
	assert(summary["is_empty"] == false, "Slot 10 should not be empty")
	assert(summary["p1_name"] == "Reimu Player", "Slot 10 P1 name match")
	assert(summary["p2_name"] == "Marisa Player", "Slot 10 P2 name match")
	assert(summary["p1_char"] == "reimu", "Slot 10 P1 char match")
	assert(summary["p2_char"] == "marisa", "Slot 10 P2 char match")
	print("PASS: Slot saving and metadata extraction verified")
	
	# 3. Test Loading Slot and Playback Control
	var load_data: Dictionary = rm.load_from_slot(test_slot)
	assert(load_data.is_empty() == false, "Loaded data must not be empty")
	assert(load_data["stage_id"] == "bamboo_road", "Stage ID in loaded data match")
	
	# Test playback state manipulation without full scene change
	var playback_start: bool = rm.start_playback(test_slot, self)
	assert(playback_start == true, "start_playback on slot 10 should succeed")
	assert(rm.is_replaying == true, "is_replaying must be true")
	assert(rm.playback_speed == 1.0, "Default playback speed is 1.0")
	
	# Playback speed stays locked to 1.0 (lean, desync-proof)
	assert(rm.cycle_speed() == 1.0, "Playback speed remains locked at 1.0x")
	
	# Pause toggle
	assert(rm.toggle_pause() == true, "Toggle pause to true")
	assert(rm.toggle_pause() == false, "Toggle pause back to false")
	
	rm.stop_playback(null)
	assert(rm.is_replaying == false, "Playback stopped")
	print("PASS: Playback state management and pause controls verified")
	
	# 4. Test Slot Deletion
	var del_success: bool = rm.delete_slot(test_slot)
	assert(del_success == true, "delete_slot should succeed")
	
	var deleted_summary: Dictionary = rm.get_slot_summary(test_slot)
	assert(deleted_summary["is_empty"] == true, "Slot 10 should be empty after deletion")
	print("PASS: Slot deletion verified")
	
	# 5. Safe handling of invalid slots
	assert(rm.get_slot_summary(0)["is_empty"] == true, "Slot 0 should safely be empty")
	assert(rm.get_slot_summary(11)["is_empty"] == true, "Slot 11 should safely be empty")
	assert(rm.load_from_slot(99).is_empty() == true, "Slot 99 load should return empty dict")
	assert(rm.start_playback(99, self) == false, "start_playback on empty slot returns false")
	print("PASS: Boundary and error handling verified")
	
	rm.queue_free()
	print("--- ALL REPLAY SYSTEM TESTS PASSED SUCCESSFULLY! ---")
	quit(0)

