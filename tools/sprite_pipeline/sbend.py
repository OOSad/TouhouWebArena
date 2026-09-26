"""Bank a player sprite the way PoFV does: bend the body into a soft S instead of rotating it.
Usage: python sbend.py <sheet.png> <cell> <col> <row> <out_dir>

Each pixel row shifts sideways by whole pixels, so nothing blurs. The offsets are Reimu's held
bank measured row by row from pl00.png (sprites 12-15 against idle 0-7, centre of each row),
smoothed and resampled over the height: head and bow lean slightly into the turn, torso holds,
hips swing out the other way, knees come back, feet trail. Values are for banking LEFT in
pixels (negative = left); banking right mirrors them. The ramp eases in like pl00's sprites 8-11.
"""
import os, sys
import numpy as np
from PIL import Image

# Reimu's bank-left offsets, top of sprite to bottom (42 rows with pixels; both banks averaged, the
# right one mirrored; 3-row moving average). The top three rows measure -6.7, -5.2, -3.1, but that
# is her bow's ribbons swinging, not her head: applied to a hat it shears the tip off, so they are
# capped to a head tilt that tapers into the measured head rows.
BANK_LEFT = [-2.0, -1.6, -1.3, -1.2, -0.6, -0.5, -0.7, -0.9, -1.2, -1.1, -0.9, -0.5, -0.3, -0.2,
             -0.4, -0.8, -0.8, -0.5, -0.1, 0.1, 0.3, 0.8, 1.2, 1.6, 1.7, 1.8, 1.9, 1.8, 1.3, 0.6,
             0.3, 0.2, 0.6, 1.5, 2.8, 4.0, 4.8, 5.0, 5.0, 5.0, 5.3, 5.5]
EASE = [0.3, 0.55, 0.8, 1.0, 1.0, 1.0, 1.0, 1.0]  # 8 frames per row, held at full bend
LEAN_DEGREES = 3.0  # whole-body lean into the turn on top of the S, at full bend; keep it subtle
ROT_UP = 8  # rotation happens at 8x with no blending, then each 8x8 block keeps its commonest colour


def lean(frame: Image.Image, degrees: float, pivot: tuple[float, float]) -> Image.Image:
    """Rotate pixel art without inventing colours: every output pixel is one the sprite already has."""
    if abs(degrees) < 0.01:
        return frame
    w, h = frame.size
    big = frame.resize((w * ROT_UP, h * ROT_UP), Image.NEAREST)
    big = big.rotate(degrees, resample=Image.NEAREST, center=(pivot[0] * ROT_UP, pivot[1] * ROT_UP))
    a = np.asarray(big).reshape(h, ROT_UP, w, ROT_UP, 4).transpose(0, 2, 1, 3, 4).reshape(h, w, -1, 4)
    out = np.zeros((h, w, 4), dtype=np.uint8)
    for y in range(h):
        for x in range(w):
            block = a[y, x]
            if (block[:, 3] > 0).sum() * 2 < len(block):
                continue  # mostly empty: stays transparent
            colours, counts = np.unique(block[block[:, 3] > 0], axis=0, return_counts=True)
            out[y, x] = colours[counts.argmax()]
    return Image.fromarray(out, "RGBA")


def bend(frame: Image.Image, amount: float) -> Image.Image:
    """amount: -1..1, negative banks left (Reimu's profile), positive banks right (mirrored)."""
    a = np.asarray(frame.convert("RGBA"))
    rows = np.where(a[..., 3].any(axis=1))[0]
    top, bottom = rows[0], rows[-1]
    profile = np.array(BANK_LEFT)
    out = np.zeros_like(a)
    for y in range(a.shape[0]):
        if y < top or y > bottom:
            continue
        t = (y - top) / max(bottom - top, 1) * (len(profile) - 1)
        shift = int(round(np.interp(t, np.arange(len(profile)), profile) * -amount))
        out[y] = np.roll(a[y], shift, axis=0)
        if shift > 0:
            out[y, :shift] = 0
        elif shift < 0:
            out[y, shift:] = 0
    # Lean into the turn: banking left tips the top left (counter-clockwise), about the body's middle.
    pivot = (a.shape[1] / 2, (top + bottom) / 2)
    return lean(Image.fromarray(out, "RGBA"), LEAN_DEGREES * -amount, pivot)


def main() -> None:
    sheet, cell, col, row, out_dir = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
    frame = Image.open(sheet).convert("RGBA").crop((col * cell, row * cell, col * cell + cell, row * cell + cell))
    os.makedirs(out_dir, exist_ok=True)
    out = Image.new("RGBA", (cell * 8, cell * 3), (0, 0, 0, 0))
    for i in range(8):
        out.paste(frame, (i * cell, 0))
        out.paste(bend(frame, -EASE[i]), (i * cell, cell))
        out.paste(bend(frame, EASE[i]), (i * cell, cell * 2))
    out.save(os.path.join(out_dir, "sbend_sheet.png"))
    print(os.path.join(out_dir, "sbend_sheet.png"))


if __name__ == "__main__":
    main()
