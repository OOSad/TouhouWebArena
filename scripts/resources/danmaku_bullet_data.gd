class_name DanmakuBulletData
extends Resource

## Data Resource defining the appearance, collision shape, and properties of a Danmaku projectile.
## Reusable across spellcards, regular attacks, and boss patterns.

enum ShapeType {
	CIRCLE,
	TEXTURED
}

@export var bullet_id: String = "bullet"
@export var shape_type: ShapeType = ShapeType.TEXTURED
@export var texture: Texture2D = null
@export var tint_color: Color = Color.WHITE
## Frames the sprite cycles through for an animated bullet, starting from the first. Keep them
## in the shared atlas so the bullets still batch. Empty or one frame means a static sprite.
@export var anim_frames: Array[Texture2D] = []
## Seconds each anim_frames entry is shown.
@export var anim_frame_duration: float = 0.05
## Optional pre-colored white texture swapped in by freeze_and_scatter() (e.g. Cirno's
## "Perfect Freeze"). Needed because bullet art with saturated colors baked into its pixels
## (not just a modulate tint) cannot be turned white via modulate alone.
@export var frozen_texture: Texture2D = null
@export var hitbox_radius: float = 6.0
@export var rotate_with_direction: bool = true
## Extra rotation (degrees) applied on top of rotate_with_direction's default "nose points up"
## assumption. Most textures need 0; a texture whose native art already points nose-RIGHT
## (e.g. Sakuya's silver knife) needs -90 to align correctly instead of rendering sideways.
@export var rotation_offset_deg: float = 0.0
## Continuous rotation speed of the sprite in radians/second (e.g. for spinning star bullets)
@export var spin_speed: float = 0.0
@export var base_scale: Vector2 = Vector2.ONE
@export var damage: float = 1.0
@export var can_be_canceled: bool = true
## Maximum lifespan of the bullet in seconds before automatically despawning.
@export var lifetime: float = 7.0
## Purely visual spawn-in: newly spawned bullets start oversized and fully transparent, then
## shrink down to base_scale while fading in to full opacity. Does not delay or affect the
## hitbox/collision, which is active at full size from the first frame.
@export var spawn_fade_in: bool = false
## How much bigger than base_scale the bullet starts at (1.0 = no size change, just a plain fade).
@export var spawn_fade_in_start_scale_mult: float = 1.6
@export var spawn_fade_in_duration: float = 0.18
## ZUN's bullet spawn animation (fire flag 0x2): while fading in, the bullet cannot hit and
## only creeps along at spawn_fade_in_speed_mult of its speed, then sets off once fully
## formed. Rings fired every few frames pile up on the emitter meanwhile, which is the glow
## PoFV shows there.
@export var spawn_fade_in_holds: bool = false
## Fraction of its speed a forming bullet travels at (0 holds it still). Yuuka's big balls
## creep ~100px over their 16 forming frames, under half speed, which is what spreads the six
## overlapping glows on the emitter into PoFV's six-lobed star.
@export var spawn_fade_in_speed_mult: float = 0.0
## Optional soft glow drawn in the bullet's place while it forms (needs spawn_fade_in_holds);
## the real texture takes over as it sets off. Sized to the bullet's own cell, times
## spawn_fade_in_start_scale_mult at the start.
@export var spawn_fade_in_texture: Texture2D = null
## Size the glow shrinks to by the end of forming, as a multiple of the bullet's own cell.
## Above 1.0 keeps it a big soft light all the way out, as PoFV's does, before the bullet
## itself takes over.
@export var spawn_fade_in_texture_end_scale: float = 1.0

