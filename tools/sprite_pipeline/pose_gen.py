"""Draw a sprite frame with its body pose pinned by the OpenPose ControlNet.
Usage: python pose_gen.py <start_image in ComfyUI/input> <out_prefix> <seed> <denoise> <pose_strength> "<prompt>" [count] [bend]
bend (-1..1) bends the starting skeleton along sbend.BANK_LEFT (negative = bank left) before drawing.
Writes <out_prefix>_pose.png, <out_prefix>_<n>.png sprites and <out_prefix>_sheet.png to the output folder.
"""
import json, os, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from clownpiece_pose import CANVAS, FRAME_AT, KEYPOINTS, SCALE  # noqa: E402
from gen import NEG, QUALITY  # noqa: E402
from redraw import BG, COMFY, run, shrink, contact_sheet  # noqa: E402
from sbend import BANK_LEFT  # noqa: E402

BODY_TOP, BODY_BOTTOM = 2, 46  # rows of the 48x48 idle frame that have pixels, as sbend measures them


def bent_pose(bend: float) -> str:
    flat = []
    for i in range(18):
        p = KEYPOINTS[i]
        if p is None:
            flat += [0, 0, 0]
            continue
        t = (p[1] - BODY_TOP) / (BODY_BOTTOM - BODY_TOP) * (len(BANK_LEFT) - 1)
        dx = np.interp(t, np.arange(len(BANK_LEFT)), BANK_LEFT) * -bend
        flat += [(p[0] + dx + FRAME_AT[0]) * SCALE, (p[1] + FRAME_AT[1]) * SCALE, 1]
    return json.dumps({"people": [{"pose_keypoints_2d": flat}], "canvas_width": CANVAS, "canvas_height": CANVAS})


def workflow(start: str, prefix: str, seed: int, denoise: float, strength: float, pos: str, count: int,
             pose: str) -> dict:
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "NoobAI-XL-v1.1.safetensors"}},
        "8": {"class_type": "LoraLoader",
              "inputs": {"model": ["1", 0], "clip": ["1", 1], "lora_name": "pixel-art-xl.safetensors",
                         "strength_model": 0.8, "strength_clip": 0.8}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 1], "text": QUALITY + pos}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 1], "text": NEG}},
        "9": {"class_type": "OpenposeEditorNode",
              "inputs": {"show_body": True, "show_face": False, "show_hands": False, "resolution_x": -1,
                         "pose_marker_size": 4, "face_marker_size": 3, "hand_marker_size": 2, "hands_scale": 1.0,
                         "body_scale": 1.0, "head_scale": 1.0, "overall_scale": 1.0, "scalelist_behavior": "poses",
                         "match_scalelist_method": "loop extend", "only_scale_pose_index": 99, "POSE_JSON": pose}},
        "10": {"class_type": "ControlNetLoader", "inputs": {"control_net_name": "noob_openpose.safetensors"}},
        "11": {"class_type": "ControlNetApplyAdvanced",
               "inputs": {"positive": ["2", 0], "negative": ["3", 0], "control_net": ["10", 0], "image": ["9", 0],
                          "strength": strength, "start_percent": 0.0, "end_percent": 1.0}},
        "12": {"class_type": "LoadImage", "inputs": {"image": start}},
        "13": {"class_type": "VAEEncode", "inputs": {"pixels": ["12", 0], "vae": ["1", 2]}},
        "14": {"class_type": "RepeatLatentBatch", "inputs": {"samples": ["13", 0], "amount": count}},
        "5": {"class_type": "KSampler",
              "inputs": {"model": ["8", 0], "positive": ["11", 0], "negative": ["11", 1], "latent_image": ["14", 0],
                         "seed": seed, "steps": 28, "cfg": 5.0, "sampler_name": "euler_ancestral",
                         "scheduler": "normal", "denoise": denoise}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": prefix + "_raw"}},
        "15": {"class_type": "SaveImage", "inputs": {"images": ["9", 0], "filename_prefix": prefix + "_pose"}},
    }


def main() -> None:
    start, prefix, seed = sys.argv[1], sys.argv[2], int(sys.argv[3])
    denoise, strength, pos = float(sys.argv[4]), float(sys.argv[5]), sys.argv[6]
    count = int(sys.argv[7]) if len(sys.argv) > 7 else 4
    bend = float(sys.argv[8]) if len(sys.argv) > 8 else 0.0
    outs = run(workflow(start, prefix, seed, denoise, strength, pos, count, bent_pose(bend)))
    sprites = [shrink(p) for p in outs]
    out_dir = os.path.join(COMFY, "output")
    for n, s in enumerate(sprites, 1):
        s.save(os.path.join(out_dir, f"{prefix}_{n}.png"))
    original = Image.open(os.path.join(COMFY, "input", start)).convert("RGBA").resize((64, 64), Image.NEAREST)
    print(contact_sheet([original] + sprites, os.path.join(out_dir, f"{prefix}_sheet.png")))


if __name__ == "__main__":
    main()
