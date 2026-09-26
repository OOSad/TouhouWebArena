"""Redraw an existing sprite frame with img2img, keeping its exact pixel grid.
Usage: python redraw.py <sheet.png> <cell_w> <cell_h> <col> <row> <out_prefix> <seed> <denoise> <lora> "<prompt>" [count] [style]
style > 0 also uses the frame as an IP-Adapter style reference (needs ComfyUI_IPAdapter_plus).
The frame is padded to 64x64, blown up 16x (1024x1024) with hard pixel edges, redrawn, then
shrunk back 16x with one colour per 16x16 block, so every redrawn pixel maps onto one sprite pixel.
Writes <out_prefix>_<n>.png sprites and <out_prefix>_sheet.png (original first) into the output folder.
"""
import json, os, sys, time, urllib.request
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
# ComfyUI install to talk to and read/write images in; set COMFYUI_DIR to use another one.
COMFY = os.environ.get("COMFYUI_DIR") or os.path.join(
    os.path.expanduser("~"), "Desktop", "Local AI Art", "ComfyUI_windows_portable", "ComfyUI")
sys.path.insert(0, HERE)
from gen import HOST, NEG, QUALITY, post  # noqa: E402  (gen.py only runs main() as a script)

CANVAS, UP = 64, 16
BG = (190, 190, 190)
COLORS = 32


def prep(sheet: str, cw: int, ch: int, col: int, row: int) -> tuple[Image.Image, str]:
    frame = Image.open(sheet).convert("RGBA").crop((col * cw, row * ch, col * cw + cw, row * ch + ch))
    canvas = Image.new("RGBA", (CANVAS, CANVAS), BG + (255,))
    canvas.alpha_composite(frame, ((CANVAS - cw) // 2, CANVAS - ch - 4))
    big = canvas.convert("RGB").resize((CANVAS * UP, CANVAS * UP), Image.NEAREST)
    name = "redraw_input.png"
    big.save(os.path.join(COMFY, "input", name))
    return canvas, name


def workflow(image: str, prefix: str, seed: int, denoise: float, lora: float, pos: str, count: int,
             style: float) -> dict:
    """style > 0 also shows the frame to the IP-Adapter as a style reference at that weight."""
    nodes = {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "NoobAI-XL-v1.1.safetensors"}},
        "8": {"class_type": "LoraLoader",
              "inputs": {"model": ["1", 0], "clip": ["1", 1], "lora_name": "pixel-art-xl.safetensors",
                         "strength_model": lora, "strength_clip": lora}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 1], "text": QUALITY + pos}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 1], "text": NEG}},
        "9": {"class_type": "LoadImage", "inputs": {"image": image}},
        "10": {"class_type": "VAEEncode", "inputs": {"pixels": ["9", 0], "vae": ["1", 2]}},
        "11": {"class_type": "RepeatLatentBatch", "inputs": {"samples": ["10", 0], "amount": count}},
        "5": {"class_type": "KSampler",
              "inputs": {"model": ["8", 0], "positive": ["2", 0], "negative": ["3", 0],
                         "latent_image": ["11", 0], "seed": seed, "steps": 28, "cfg": 5.0,
                         "sampler_name": "euler_ancestral", "scheduler": "normal", "denoise": denoise}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": prefix}},
    }
    if style > 0:
        nodes["12"] = {"class_type": "IPAdapterModelLoader",
                       "inputs": {"ipadapter_file": "ip_adapter_Noobtest_800000.bin"}}
        nodes["13"] = {"class_type": "CLIPVisionLoader",
                       "inputs": {"clip_name": "CLIP-ViT-H-14-laion2B-s32B-b79K.safetensors"}}
        nodes["14"] = {"class_type": "IPAdapterAdvanced",
                       "inputs": {"model": ["8", 0], "ipadapter": ["12", 0], "image": ["9", 0],
                                  "clip_vision": ["13", 0], "weight": style, "weight_type": "style transfer",
                                  "combine_embeds": "concat", "start_at": 0.0, "end_at": 1.0,
                                  "embeds_scaling": "V only"}}
        nodes["5"]["inputs"]["model"] = ["14", 0]
    return nodes


def run(wf: dict, save_node: str = "7") -> list[str]:
    pid = post("/prompt", {"prompt": wf})["prompt_id"]
    while True:
        hist = json.load(urllib.request.urlopen(f"{HOST}/history/{pid}"))
        if pid in hist:
            if hist[pid].get("status", {}).get("status_str") == "error":
                raise SystemExit("ERROR " + json.dumps(hist[pid]["status"])[:2000])
            return [os.path.join(COMFY, "output", i["filename"]) for i in hist[pid]["outputs"][save_node]["images"]]
        time.sleep(1)


def shrink(path: str) -> Image.Image:
    """ZUN-style soft shrink. Each 16x16 block becomes the average colour of the character inside it
    (background excluded, so edges don't pick up grey), with alpha = how much of the block she covers.
    No colour reduction, partial alpha at the outline. Measured against pl00/pl01 idles: ZUN has
    21-28% semi-transparent edge pixels and ~87 neighbour contrast; this gives ~32% and ~90, where the
    old one-colour-per-block shrink (shrink_crunchy) gave 0% and 115-141."""
    a = np.asarray(Image.open(path).convert("RGB")).astype(float)
    bg = np.median(np.concatenate([a[0], a[:, 0], a[:, -1]]), axis=0)
    m = np.clip((np.abs(a - bg).max(axis=2) - 12) / 30, 0, 1)  # soft foreground coverage per big pixel
    blocks = m.reshape(CANVAS, UP, CANVAS, UP)
    weight = blocks.sum(axis=(1, 3))[..., None]
    rgb = (a * m[..., None]).reshape(CANVAS, UP, CANVAS, UP, 3).sum(axis=(1, 3)) / np.maximum(weight, 1e-6)
    alpha = np.clip((blocks.mean(axis=(1, 3)) - 0.08) / 0.84, 0, 1)  # drop faint haze, keep true edge coverage
    return Image.fromarray(np.dstack([rgb, alpha * 255]).round().astype(np.uint8), "RGBA")


def shrink_crunchy(path: str) -> Image.Image:
    """Old shrink, kept for comparison: each 16x16 block becomes the median colour of its centre,
    hard alpha, reduced to COLORS colours. Reads as GBA-style pixel art, not ZUN."""
    a = np.asarray(Image.open(path).convert("RGB"), dtype=np.int16)
    bg = np.median(np.concatenate([a[0], a[:, 0], a[:, -1]]), axis=0)
    q = UP // 4
    centres = a.reshape(CANVAS, UP, CANVAS, UP, 3)[:, q:UP - q, :, q:UP - q].transpose(0, 2, 1, 3, 4)
    rgb = np.median(centres.reshape(CANVAS, CANVAS, -1, 3), axis=2)
    solid = np.abs(rgb - bg).max(axis=2) > 20
    # Drop speckle: pixels with at most one solid neighbour are background noise, not sprite.
    padded = np.pad(solid, 1)
    neighbours = sum(np.roll(np.roll(padded, dy, 0), dx, 1) for dy in (-1, 0, 1) for dx in (-1, 0, 1)
                     if dy or dx)[1:-1, 1:-1]
    solid &= neighbours >= 2
    fg = Image.fromarray(rgb[solid].astype(np.uint8)[None], "RGB")
    reduced = np.asarray(fg.quantize(COLORS, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"))[0]
    out = np.zeros((CANVAS, CANVAS, 4), dtype=np.uint8)
    out[solid, :3], out[solid, 3] = reduced, 255
    return Image.fromarray(out, "RGBA")


def main() -> None:
    sheet, cw, ch, col, row, prefix = sys.argv[1], *map(int, sys.argv[2:6]), sys.argv[6]
    seed, denoise, lora, pos = int(sys.argv[7]), float(sys.argv[8]), float(sys.argv[9]), sys.argv[10]
    count = int(sys.argv[11]) if len(sys.argv) > 11 else 4
    style = float(sys.argv[12]) if len(sys.argv) > 12 else 0.0
    original, name = prep(sheet, cw, ch, col, row)
    outs = run(workflow(name, prefix, seed, denoise, lora, pos, count, style))
    sprites = [shrink(p) for p in outs]
    out_dir = os.path.join(COMFY, "output")
    for n, s in enumerate(sprites, 1):
        s.save(os.path.join(out_dir, f"{prefix}_{n}.png"))
    print(contact_sheet([original] + sprites, os.path.join(out_dir, f"{prefix}_sheet.png")))


def contact_sheet(sprites: list, path: str) -> str:
    """Sprites side by side at 6x on grey; the first is the prepped original, whose canvas colour is cleared."""
    scale, pad = 6, 8
    grey = (150, 145, 140, 255)
    sheet_img = Image.new("RGBA", (len(sprites) * (CANVAS * scale + pad) + pad, CANVAS * scale + 2 * pad), grey)
    for n, s in enumerate(sprites):
        s = s.copy()
        if n == 0:
            a = np.asarray(s).copy()
            a[np.all(a[:, :, :3] == BG, axis=2)] = 0
            s = Image.fromarray(a, "RGBA")
        sheet_img.alpha_composite(s.resize((CANVAS * scale, CANVAS * scale), Image.NEAREST),
                                  (pad + n * (CANVAS * scale + pad), pad))
    sheet_img.save(path)
    return path

if __name__ == "__main__":
    main()
