class_name SpellcardData
extends Resource

## Data Resource defining a complete Spellcard timeline sequence.
## Contains an ordered sequence of DanmakuSteps, repetition rules, and rank scaling.

@export var spellcard_id: String = "spellcard"
@export var spellcard_name: String = "Fantasy Seal"
@export_range(2, 4) var level: int = 2

@export_group("Wave Repetition & Rank Scaling")
## Number of wave repetitions at Rank 1
@export var waves_min: int = 1
## Number of wave repetitions at Rank 16
@export var waves_max: int = 3
## Delay in seconds between successive wave iterations
@export var wave_interval: float = 0.70
## Optional stepped wave counts: rank below wave_rank_breaks[i] fires wave_counts[i] waves,
## anything higher the last entry. Overrides the waves_min / waves_max lerp when set.
@export var wave_rank_breaks: PackedInt32Array = PackedInt32Array()
@export var wave_counts: PackedInt32Array = PackedInt32Array()
## Nudge each later wave's origin a little (off for ECL-exact cards, which repeat in place)
@export var jitter_wave_origin: bool = true
## Roll one angle per wave and hand it to every step (see DanmakuStep.set_wave_angle)
@export var random_wave_angle: bool = false
## Fire from the victim's mirror image across the centre line, x clamped to
## +-mirror_origin_clamp of centre, at mirror_origin_y (ZUN's F0 = -PLAYER_X helper spawn)
@export var mirror_player_origin: bool = false
@export var mirror_origin_y: float = 266.7
@export var mirror_origin_clamp: float = 166.7

@export_group("Timeline Steps")
## The sequential steps executed during each wave of this spellcard
@export var steps: Array[DanmakuStep] = []

## Asynchronously executes the spellcard sequence on the target playfield.
## pattern_seed, when >= 0, makes the sequence reproducible: it is derived from the
## match's synced round seed by the caller (Playfield.execute_spellcard,
## BossCharacter._trigger_attack), so both netplay clients walk the same waves and hand
## each step the same RNG. Leave it at -1 for a local match or a test.
func execute_sequence(playfield: Node2D, origin: Vector2, rank: int, pattern_seed: int = -1) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var rng: RandomNumberGenerator = null
	if pattern_seed >= 0:
		rng = RandomNumberGenerator.new()
		rng.seed = pattern_seed
	
	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var wave_count: int = int(round(lerpf(float(waves_min), float(waves_max), t)))
	if not wave_counts.is_empty():
		var r: int = clampi(rank, 1, 16)
		wave_count = wave_counts[wave_counts.size() - 1]
		for i in range(mini(wave_rank_breaks.size(), wave_counts.size())):
			if r < wave_rank_breaks[i]:
				wave_count = wave_counts[i]
				break
	
	if mirror_player_origin:
		var victim: Node2D = playfield.get("player")
		if victim and is_instance_valid(victim):
			origin = Vector2(300.0 - clampf(victim.position.x - 300.0, -mirror_origin_clamp, mirror_origin_clamp), mirror_origin_y)
	
	for w in range(wave_count):
		if not is_instance_valid(playfield):
			return
		
		# Slight horizontal jitter for successive waves near top center. This moves the
		# whole wave, so it is "from where the attack comes" rather than bullet jitter.
		var wave_origin := origin
		if w > 0 and jitter_wave_origin:
			wave_origin.x += rng.randf_range(-30.0, 30.0) if rng else randf_range(-30.0, 30.0)
			wave_origin.y += rng.randf_range(-10.0, 10.0) if rng else randf_range(-10.0, 10.0)
		
		var wave_angle: float = 0.0
		if random_wave_angle:
			wave_angle = (rng.randf() if rng else randf()) * TAU
		
		for step in steps:
			if step == null:
				continue
			if not is_instance_valid(playfield):
				return
			# Each step gets its own generator rather than sharing this one: a step that
			# awaits must not have its draw sequence interleaved with anything else.
			if rng:
				var step_rng := RandomNumberGenerator.new()
				step_rng.seed = rng.randi()
				step.set_sync_rng(step_rng)
			if random_wave_angle:
				step.set_wave_angle(wave_angle)
			await step.execute(playfield, wave_origin, rank)
		
		if w < wave_count - 1 and wave_interval > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(wave_interval).timeout

