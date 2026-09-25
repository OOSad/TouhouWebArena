class_name DanmakuStep
extends Resource

## Abstract base class for a discrete step in a Danmaku pattern or spellcard timeline.

## Optional sync hook. SpellcardData hands a step its own seeded RandomNumberGenerator
## immediately before execute(), so randomness a step treats as part of the attack's
## identity - where a big Extra Attack lands, which way it is tossed - rolls the same on
## both netplay clients. A step opts in by calling take_sync_rng() on its first line.
## Steps that only jitter individual bullets can ignore it and keep drawing from the
## global RNG; that jitter is deliberately left unsynced.
var _sync_rng: RandomNumberGenerator = null

func set_sync_rng(rng: RandomNumberGenerator) -> void:
	_sync_rng = rng

## Reads and clears the pending sync RNG. Steps must call this on their first line,
## before any await: step resources are shared between both playfields, so holding the
## RNG in a local is what stops a second, concurrent execute() swapping it out mid-pattern.
## Returns null outside a seeded pattern, which means "use the global RNG".
func take_sync_rng() -> RandomNumberGenerator:
	var rng: RandomNumberGenerator = _sync_rng
	_sync_rng = null
	return rng

func execute(_playfield: Node2D, _origin: Vector2, _rank: int) -> void:
	pass


## Optional per-wave angle. SpellcardData with random_wave_angle hands every step of a wave
## the same angle (ZUN rolls one RAND_ANGLE per wave and fires several rings from it). Same
## take-on-the-first-line rule as the sync RNG. Returns 0.0 when none was set.
var _wave_angle: float = 0.0

func set_wave_angle(angle: float) -> void:
	_wave_angle = angle

func take_wave_angle() -> float:
	var angle: float = _wave_angle
	_wave_angle = 0.0
	return angle
