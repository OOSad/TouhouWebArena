"""Queue text-to-image jobs on a local ComfyUI server and wait for results.
Usage: python gen.py <out_prefix> <seed> "<positive prompt>" [count] [pixel_lora_strength]
A pixel_lora_strength above 0 applies pixel-art-xl.safetensors from models/loras.
"""
import json, sys, time, urllib.request

HOST = "http://127.0.0.1:8188"
NEG = ("worst quality, low quality, lowres, bad anatomy, bad hands, extra digits, "
       "missing fingers, jpeg artifacts, signature, watermark, text, blurry, "
       "multiple views, cropped, out of frame")
QUALITY = "masterpiece, best quality, newest, absurdres, highres, "


def workflow(prefix: str, seed: int, pos: str, count: int, lora: float) -> dict:
    model, clip = ["1", 0], ["1", 1]
    nodes = {
        "1": {"class_type": "CheckpointLoaderSimple",
              "inputs": {"ckpt_name": "NoobAI-XL-v1.1.safetensors"}},
    }
    if lora > 0:
        nodes["8"] = {"class_type": "LoraLoader",
                      "inputs": {"model": model, "clip": clip, "lora_name": "pixel-art-xl.safetensors",
                                 "strength_model": lora, "strength_clip": lora}}
        model, clip = ["8", 0], ["8", 1]
    nodes.update({
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": clip, "text": QUALITY + pos}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": clip, "text": NEG}},
        "4": {"class_type": "EmptyLatentImage",
              "inputs": {"width": 832, "height": 1216, "batch_size": count}},
        "5": {"class_type": "KSampler",
              "inputs": {"model": model, "positive": ["2", 0], "negative": ["3", 0],
                         "latent_image": ["4", 0], "seed": seed, "steps": 28, "cfg": 5.0,
                         "sampler_name": "euler_ancestral", "scheduler": "normal",
                         "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": prefix}},
    })
    return nodes


def post(path: str, body: dict) -> dict:
    req = urllib.request.Request(HOST + path, data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(req))


def main() -> None:
    prefix, seed, pos = sys.argv[1], int(sys.argv[2]), sys.argv[3]
    count = int(sys.argv[4]) if len(sys.argv) > 4 else 4
    lora = float(sys.argv[5]) if len(sys.argv) > 5 else 0.0
    pid = post("/prompt", {"prompt": workflow(prefix, seed, pos, count, lora)})["prompt_id"]
    t0 = time.time()
    while True:
        hist = json.load(urllib.request.urlopen(f"{HOST}/history/{pid}"))
        if pid in hist:
            status = hist[pid].get("status", {})
            if status.get("status_str") == "error":
                print("ERROR", json.dumps(status)[:2000])
                sys.exit(1)
            for img in hist[pid]["outputs"]["7"]["images"]:
                print(img["filename"])
            print(f"done in {time.time() - t0:.1f}s")
            return
        time.sleep(1)


if __name__ == "__main__":
    main()
