class_name BossData
extends Resource

## Modular Boss Data Resource for Touhou Web Arena.
## Configures stats, movement, hopping parameters, bounds, and identity
## for Level 4 Boss character entities.

enum DepartureMode {
	ATTACK_COUNT,
	DURATION,
	HYBRID,
}

enum AttackSelectionMode {
	RANDOM,
	SEQUENTIAL,
}

@export_group("Identity")
@export var character_id: String = "reimu"
@export var boss_name: String = "Reimu Hakurei"
@export var sprite_sheet: Texture2D
@export var primary_color: Color = Color(1.0, 0.35, 0.45, 1.0)
@export var hframes: int = 4
@export var vframes: int = 3
@export var frame_duration: float = 0.08
@export var sprite_scale: Vector2 = Vector2(2.33, 2.33)

@export_group("Combat Stats")
## Max health calibrated to approx 7 seconds of uninterrupted Marisa fire (~26.7 DPS)
@export var max_health: float = 180.0
## Departure criteria: by attack quota, fixed duration, or hybrid
@export var departure_mode: DepartureMode = DepartureMode.ATTACK_COUNT
## Minimum attack count before departure (PoFV rolls 5-9 attacks)
@export var attacks_min: int = 5
## Maximum attack count before departure (PoFV rolls 5-9 attacks)
@export var attacks_max: int = 9
## If true, randomly rolls attack count between attacks_min and attacks_max instead of scaling with rank
@export var randomize_attack_count: bool = true
## Fallback safety duration before forced departure
@export var duration_safety: float = 24.0
## Duration on the playfield before leaving on its own (seconds) (used in DURATION or HYBRID mode)
@export var duration: float = 12.0
## If true, rolls a random duration between duration_min and duration_max instead of using the fixed duration above
@export var randomize_duration: bool = false
## Minimum duration on the playfield when randomize_duration is true (seconds)
@export var duration_min: float = 12.0
## Maximum duration on the playfield when randomize_duration is true (seconds)
@export var duration_max: float = 12.0
## Hitbox collision radius calibrated for sprite scale
@export var collision_radius: float = 32.0

@export_group("Movement & Hopping")
## Offset relative to entrance_target_pos where the boss begins swooping in from offscreen
@export var entrance_start_offset: Vector2 = Vector2(170.0, -260.0)
## Initial hover position in the upper-center playfield
@export var entrance_target_pos: Vector2 = Vector2(300.0, 150.0)
## Time to swoop in from offscreen (seconds)
@export var entrance_duration: float = 0.85
## Minimum wait time between hops (seconds)
@export var hop_interval_min: float = 1.3
## Maximum wait time between hops (seconds)
@export var hop_interval_max: float = 1.7
## Duration of a single hop motion (seconds)
@export var hop_duration: float = 0.65
## Minimum horizontal hop distance (pixels) - longer jumps across playfield
@export var hop_distance_min: float = 90.0
## Maximum horizontal hop distance (pixels)
@export var hop_distance_max: float = 160.0
## Bounding box within which the boss is allowed to roam (X: 140..460, Y: 110..210)
## Keeps the boss strictly in the upper screen and away from screen edges
@export var roam_bounds: Rect2 = Rect2(140.0, 110.0, 320.0, 100.0)
## Subtle vertical bobbing amplitude while hovering idle (pixels)
@export var bob_amplitude: float = 4.0
## Bobbing frequency (cycles per second)
@export var bob_frequency: float = 3.0

@export_group("Attacks & Danmaku")
## Attack selection strategy: pure random (with repeats allowed) or fixed sequential order
@export var selection_mode: AttackSelectionMode = AttackSelectionMode.RANDOM
## Rate of Danmaku spell attacks in seconds
@export var attack_rate: float = 1.6
## Delay before first attack after entering the playfield (seconds)
@export var initial_attack_delay: float = 0.4
## Recovery delay after casting before starting the next hop (seconds)
@export var post_cast_delay: float = 0.3
## Settle delay after landing a hop before casting the next attack (seconds)
@export var post_hop_delay: float = 0.25
## Modular spellcard attack patterns executed by the boss
@export var attack_patterns: Array[SpellcardData] = []

