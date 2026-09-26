"""Writes controlnet_playground.json: a drag-in ComfyUI workflow for redrawing a sprite frame inside
its outline (canny ControlNet), starting from the frame itself. Usage: python make_controlnet_playground.py <out.json>
"""
import json, sys

PROMPT = ("masterpiece, best quality, newest, absurdres, highres, pixel art, clownpiece, touhou, 1girl, solo, chibi, "
          "fairy wings, jester cap, polka dot hat, american flag dress, american flag legwear, holding torch, "
          "blonde hair, very long hair, from behind, full body, flying, simple background, grey background")
NEGATIVE = ("worst quality, low quality, lowres, bad anatomy, bad hands, extra digits, missing fingers, "
            "jpeg artifacts, signature, watermark, text, blurry, multiple views, cropped, out of frame")
NOTE = """CONTROLNET PLAYGROUND (Clownpiece)

Click Run. Results land in ComfyUI/output as controlnet_play_*.png (4 per run).

WHICH FRAME: pick the same frame in both yellow boxes:
  clownpiece_idle_...        upright
  clownpiece_bank_left_...   held bank left (the S-bend)
  clownpiece_bank_right_...  held bank right
  (_outline = the shape guide, _start = the colours to start from)

THE DIALS (orange boxes):
  Redraw amount (KSampler 'denoise'): how far it may stray from the start image.
    0.5 = touch-up, 0.6 = best so far, 0.75 = looser/noisier, 1.0 = ignores the start image.
  Outline strength (ControlNet 'strength'): how hard it must follow the outline.
    0.5 = loose, 0.7 = best so far, 1.0 = strict.
  Outline until (ControlNet 'end_percent'): when it stops following the outline.
    0.85 default; lower lets the last steps clean up details freely.
  Pixel look (LoRA strength, both numbers): 0 = normal anime, 0.8 default, 1.2 = heavier pixels.
  Seed: 'randomize' gives new attempts each Run; set it to 'fixed' to compare dial changes fairly.

The images come out big (1024x1024). Send the ones you like to Claude to shrink onto the 48px grid.
Close the black ComfyUI window (or run Stop ComfyUI.bat) when done."""


class Graph:
    def __init__(self):
        self.nodes, self.links, self.next_id = [], [], 0

    def add(self, ntype, pos, widgets=(), inputs=(), outputs=(), size=(320, 120), title=None, color=None):
        self.next_id += 1
        n = {"id": self.next_id, "type": ntype, "pos": list(pos), "size": list(size), "flags": {},
             "order": self.next_id, "mode": 0,
             "inputs": [{"name": name, "type": t, "link": None} for name, t in inputs],
             "outputs": [{"name": name, "type": t, "links": []} for name, t in outputs],
             "properties": {"Node name for S&R": ntype}, "widgets_values": list(widgets)}
        if title:
            n["title"] = title
        if color:
            n["color"], n["bgcolor"] = color
        self.nodes.append(n)
        return n

    def link(self, src, src_slot, dst, dst_slot):
        lid = len(self.links) + 1
        ltype = src["outputs"][src_slot]["type"]
        self.links.append([lid, src["id"], src_slot, dst["id"], dst_slot, ltype])
        src["outputs"][src_slot]["links"].append(lid)
        dst["inputs"][dst_slot]["link"] = lid


YELLOW = ("#553", "#664")
ORANGE = ("#653", "#864")


def main() -> None:
    g = Graph()
    g.add("Note", (-900, -420), [NOTE], size=(560, 620), title="READ ME")
    ckpt = g.add("CheckpointLoaderSimple", (-300, -420), ["NoobAI-XL-v1.1.safetensors"],
                 outputs=[("MODEL", "MODEL"), ("CLIP", "CLIP"), ("VAE", "VAE")], size=(320, 100))
    lora = g.add("LoraLoader", (-300, -260), ["pixel-art-xl.safetensors", 0.8, 0.8],
                 inputs=[("model", "MODEL"), ("clip", "CLIP")], outputs=[("MODEL", "MODEL"), ("CLIP", "CLIP")],
                 size=(320, 130), title="Pixel look (LoRA strength)", color=ORANGE)
    pos = g.add("CLIPTextEncode", (60, -420), [PROMPT], inputs=[("clip", "CLIP")],
                outputs=[("CONDITIONING", "CONDITIONING")], size=(420, 200), title="What to draw")
    neg = g.add("CLIPTextEncode", (60, -190), [NEGATIVE], inputs=[("clip", "CLIP")],
                outputs=[("CONDITIONING", "CONDITIONING")], size=(420, 150), title="What to avoid")
    guide = g.add("LoadImage", (-300, -80), ["clownpiece_bank_left_outline.png", "image"],
                  outputs=[("IMAGE", "IMAGE"), ("MASK", "MASK")], size=(320, 340),
                  title="Outline guide (pick a frame)", color=YELLOW)
    start = g.add("LoadImage", (-300, 290), ["clownpiece_bank_left_start.png", "image"],
                  outputs=[("IMAGE", "IMAGE"), ("MASK", "MASK")], size=(320, 340),
                  title="Start image (same frame)", color=YELLOW)
    cnl = g.add("ControlNetLoader", (60, -10), ["noob_sdxl_controlnet_canny.fp16.safetensors"],
                outputs=[("CONTROL_NET", "CONTROL_NET")], size=(420, 60))
    cna = g.add("ControlNetApplyAdvanced", (60, 80), [0.7, 0.0, 0.85],
                inputs=[("positive", "CONDITIONING"), ("negative", "CONDITIONING"), ("control_net", "CONTROL_NET"),
                        ("image", "IMAGE"), ("vae", "VAE")],
                outputs=[("positive", "CONDITIONING"), ("negative", "CONDITIONING")], size=(420, 190),
                title="Outline strength / Outline until", color=ORANGE)
    enc = g.add("VAEEncode", (60, 320), inputs=[("pixels", "IMAGE"), ("vae", "VAE")],
                outputs=[("LATENT", "LATENT")], size=(200, 50))
    rep = g.add("RepeatLatentBatch", (280, 320), [4], inputs=[("samples", "LATENT")],
                outputs=[("LATENT", "LATENT")], size=(200, 60), title="Attempts per run")
    ks = g.add("KSampler", (540, -420), [1201, "randomize", 28, 5.0, "euler_ancestral", "normal", 0.6],
               inputs=[("model", "MODEL"), ("positive", "CONDITIONING"), ("negative", "CONDITIONING"),
                       ("latent_image", "LATENT")],
               outputs=[("LATENT", "LATENT")], size=(320, 270), title="Seed / Redraw amount (denoise)", color=ORANGE)
    dec = g.add("VAEDecode", (540, -110), inputs=[("samples", "LATENT"), ("vae", "VAE")],
                outputs=[("IMAGE", "IMAGE")], size=(200, 50))
    save = g.add("SaveImage", (540, -20), ["controlnet_play"], inputs=[("images", "IMAGE")], size=(600, 640))

    g.link(ckpt, 0, lora, 0)
    g.link(ckpt, 1, lora, 1)
    g.link(lora, 1, pos, 0)
    g.link(lora, 1, neg, 0)
    g.link(pos, 0, cna, 0)
    g.link(neg, 0, cna, 1)
    g.link(cnl, 0, cna, 2)
    g.link(guide, 0, cna, 3)
    g.link(ckpt, 2, cna, 4)
    g.link(start, 0, enc, 0)
    g.link(ckpt, 2, enc, 1)
    g.link(enc, 0, rep, 0)
    g.link(lora, 0, ks, 0)
    g.link(cna, 0, ks, 1)
    g.link(cna, 1, ks, 2)
    g.link(rep, 0, ks, 3)
    g.link(ks, 0, dec, 0)
    g.link(ckpt, 2, dec, 1)
    g.link(dec, 0, save, 0)

    wf = {"last_node_id": g.next_id, "last_link_id": len(g.links), "nodes": g.nodes, "links": g.links,
          "groups": [], "config": {}, "extra": {}, "version": 0.4}
    json.dump(wf, open(sys.argv[1], "w", encoding="utf-8"), indent=1)
    print("written", sys.argv[1])


if __name__ == "__main__":
    main()
