"""Draw a sprite frame fresh, with its pose pinned by a canny ControlNet built from an existing frame.
Usage: python cn.py <sheet.png> <cell_w> <cell_h> <col> <row> <out_prefix> <seed> <tilt_deg> <cn_strength> "<prompt>" [count] [lora] [denoise]
tilt_deg leans the guide: positive = top towards the right (bank right), negative = bank left.
denoise below 1 also starts from the (equally tilted) frame itself, so colours carry over.
Same grid as redraw.py (64x64 canvas, 16x up), so results shrink back onto exact sprite pixels.
Writes <out_prefix>_control.png, <out_prefix>_<n>.png and <out_prefix>_sheet.png to the output folder.
"""
import os, sys
import cv2
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from gen import NEG, QUALITY  # noqa: E402
from redraw import BG, CANVAS, COMFY, UP, run, shrink, contact_sheet  # noqa: E402

PIVOT = (32, 36)  # hitbox-ish centre of the 64x64 canvas, in sprite pixels


def control_image(sheet: str, cw: int, ch: int, col: int, row: int, tilt: float) -> tuple[str, Image.Image]:
    frame = Image.open(sheet).convert("RGBA").crop((col * cw, row * ch, col * cw + cw, row * ch + ch))
    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.alpha_composite(frame, ((CANVAS - cw) // 2, CANVAS - ch - 4))
    # Edges are found at 4x, where colour steps are still sharp, then the edge map is scaled up and
    # re-thinned. A 16x smooth upscale spreads every step over ~16 px and Canny finds nothing.
    mid = canvas.resize((CANVAS * 4, CANVAS * 4), Image.LANCZOS)
    if tilt:
        mid = mid.rotate(-tilt, resample=Image.BICUBIC, center=(PIVOT[0] * 4, PIVOT[1] * 4))
    white = Image.new("RGBA", mid.size, (255, 255, 255, 255))
    white.alpha_composite(mid)
    grey = cv2.cvtColor(np.asarray(white.convert("RGB")), cv2.COLOR_RGB2GRAY)
    small_edges = cv2.Canny(grey, 40, 120)
    up = cv2.resize(small_edges, (CANVAS * UP, CANVAS * UP), interpolation=cv2.INTER_LINEAR)
    up = cv2.GaussianBlur(up, (0, 0), 2.0)
    edges = cv2.Canny(up, 10, 30)
    name = "cn_control.png"
    Image.fromarray(edges).convert("RGB").save(os.path.join(COMFY, "input", name))
    # Start image: the frame, tilted the same way, on the background grey, with hard pixel edges.
    init = canvas.resize((CANVAS * UP, CANVAS * UP), Image.NEAREST)
    if tilt:
        init = init.rotate(-tilt, resample=Image.BICUBIC, center=(PIVOT[0] * UP, PIVOT[1] * UP))
    grey_bg = Image.new("RGBA", init.size, BG + (255,))
    grey_bg.alpha_composite(init)
    grey_bg.convert("RGB").save(os.path.join(COMFY, "input", "cn_init.png"))
    return name, canvas


def workflow(control: str, prefix: str, seed: int, strength: float, pos: str, count: int, lora: float,
             denoise: float) -> dict:
    nodes = {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "NoobAI-XL-v1.1.safetensors"}},
        "8": {"class_type": "LoraLoader",
              "inputs": {"model": ["1", 0], "clip": ["1", 1], "lora_name": "pixel-art-xl.safetensors",
                         "strength_model": lora, "strength_clip": lora}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 1], "text": QUALITY + pos}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 1], "text": NEG}},
        "9": {"class_type": "LoadImage", "inputs": {"image": control}},
        "10": {"class_type": "ControlNetLoader",
               "inputs": {"control_net_name": "noob_sdxl_controlnet_canny.fp16.safetensors"}},
        "11": {"class_type": "ControlNetApplyAdvanced",
               "inputs": {"positive": ["2", 0], "negative": ["3", 0], "control_net": ["10", 0], "image": ["9", 0],
                          "strength": strength, "start_percent": 0.0, "end_percent": 0.85}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": CANVAS * UP, "height": CANVAS * UP,
                                                           "batch_size": count}},
        "5": {"class_type": "KSampler",
              "inputs": {"model": ["8", 0], "positive": ["11", 0], "negative": ["11", 1], "latent_image": ["4", 0],
                         "seed": seed, "steps": 28, "cfg": 5.0, "sampler_name": "euler_ancestral",
                         "scheduler": "normal", "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": prefix + "_raw"}},
    }
    if denoise < 1.0:
        nodes["12"] = {"class_type": "LoadImage", "inputs": {"image": "cn_init.png"}}
        nodes["13"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["12", 0], "vae": ["1", 2]}}
        nodes["14"] = {"class_type": "RepeatLatentBatch", "inputs": {"samples": ["13", 0], "amount": count}}
        nodes["5"]["inputs"]["latent_image"] = ["14", 0]
        nodes["5"]["inputs"]["denoise"] = denoise
    return nodes


def main() -> None:
    sheet, cw, ch, col, row, prefix = sys.argv[1], *map(int, sys.argv[2:6]), sys.argv[6]
    seed, tilt, strength, pos = int(sys.argv[7]), float(sys.argv[8]), float(sys.argv[9]), sys.argv[10]
    count = int(sys.argv[11]) if len(sys.argv) > 11 else 4
    lora = float(sys.argv[12]) if len(sys.argv) > 12 else 0.8
    denoise = float(sys.argv[13]) if len(sys.argv) > 13 else 1.0
    control, original = control_image(sheet, cw, ch, col, row, tilt)
    out_dir = os.path.join(COMFY, "output")
    Image.open(os.path.join(COMFY, "input", control)).save(os.path.join(out_dir, f"{prefix}_control.png"))
    outs = run(workflow(control, prefix, seed, strength, pos, count, lora, denoise))
    sprites = [shrink(p) for p in outs]
    for n, s in enumerate(sprites, 1):
        s.save(os.path.join(out_dir, f"{prefix}_{n}.png"))
    print(contact_sheet([original.copy()] + sprites, os.path.join(out_dir, f"{prefix}_sheet.png")))


if __name__ == "__main__":
    main()
