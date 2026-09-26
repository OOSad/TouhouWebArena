"""Writes pose_playground.json: a drag-in ComfyUI workflow where an OpenPose skeleton (editable by dragging
joints) sets Clownpiece's body pose. Usage: python make_pose_playground.py <out.json>

THE RECIPE (how clownpiece_player.png was made, 2026-09-26). Start here to redo her or make another
character's back-facing 48x48 player sheet with our own art instead of the player's .dat.

Setup, outside the repo: ComfyUI portable (NVIDIA build) with custom nodes ComfyUI-GGUF, ComfyUI_essentials,
ComfyUI-KJNodes, ComfyUI-ultimate-openpose-editor; models NoobAI-XL-v1.1 (checkpoints),
pixel-art-xl (loras), noob_openpose from Laxhar (controlnet, page states no license: see ASSETS_LICENSING).
Run these scripts with ComfyUI's bundled python_embeded\\python.exe while the server runs (redraw.COMFY says
where it lives). Each frame is a 64x64 canvas drawn 16x (1024px) and shrunk back; frames sit at (8, 12).

1. A base sprite in the right pose and style. Clownpiece's came from AutoSprite (clownpiece_raw_sheet.png).
2. Bank start images: sbend.py bends it into PoFV's soft S (row shifts measured from pl00's banks); the
   in-game fallback sheet of tools/generate_clownpiece_player_assets.gd is that. clownpiece_pose.py holds the
   back-view skeleton fitted to her idle (face points off, ears on: that is how OpenPose reads a back view).
3. The owner poses the held banks in this playground (right-click Skeleton -> Open in Openpose Editor ->
   Send pose to ControlNet), rerolling until one is good; denoise 0.75, pose strength 0.8, pixel LoRA 0.8.
   Keepers: clownpiece/pick_bank_left.png and pick_bank_right.png, whose PNG metadata holds the whole graph,
   and their skeletons in clownpiece/bank_*.json.
4. interp.py re-runs a pick's graph (pixel-exact, same seed and batch slot) with the skeleton blended from
   neutral (the average of both banks) at 30/55/80% and the matching S-bend start frame: the ease-in frames.
5. idle_loop.py re-runs it 8 times with the neutral skeleton swaying ~1px on a sine: PoFV idles hold still
   (pl00's centre moves < 0.5px) while details shimmer.
6. redraw.shrink brings every 1024 image to sprite size softly (area average, coverage alpha), matching ZUN's
   measured edge softness. Do not reduce colours or snap to a palette: that made GBA-style crunch.
7. Crop each frame at (8, 13, 56, 61) and lay out 8x3 cells: idle loop, bank left (3 ease + held), bank right.

Failed along the way, so skip them: rotating frames (halos, jaggies), canny redraws of tilted frames,
IP-Adapter style transfer (bleeds colours), txt2img then shrinking, text-only prompting (Gemini exaggerates).
"""
import json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from make_controlnet_playground import Graph, NEGATIVE, ORANGE, PROMPT, YELLOW  # noqa: E402
from pose_gen import bent_pose  # noqa: E402

NOTE = """POSE PLAYGROUND (Clownpiece, OpenPose skeleton)

Click Run. Results land in ComfyUI/output as pose_play_*.png (4 per run).
The small 'Pose preview' box shows the skeleton that was used.

EDIT THE POSE: right-click the green 'Skeleton' box -> Open in Openpose Editor.
Drag the joints, then close the editor (ESC) and Run.
It starts bent into the PoFV bank-left S (head in, hips out, feet trailing).
Seen from behind: her RIGHT side (torch hand) is on YOUR right.
Keep the face dots (nose, eyes) off: without them OpenPose knows it is a back view.

START IMAGE (yellow): the colours it starts from.
  clownpiece_bank_left_start / _bank_right_start / _idle_start

THE DIALS (orange boxes):
  Redraw amount (KSampler 'denoise'): 0.75 default.
    Lower keeps her colours but ignores the skeleton more; 1.0 = drawn from scratch.
  Pose strength (ControlNet 'strength'): 0.8 default; 1.0 strict, 0.5 loose.
  Pixel look (LoRA strength, both numbers): 0.8 default.
  Seed: 'randomize' gives new attempts; 'fixed' to compare dial changes fairly.

Images come out 1024x1024. Send the ones you like to Claude to shrink onto the 48px grid.
Close the black ComfyUI window (or Stop ComfyUI.bat) when done."""
GREEN = ("#353", "#464")


def main() -> None:
    g = Graph()
    g.add("Note", (-900, -420), [NOTE], size=(560, 660), title="READ ME")
    ckpt = g.add("CheckpointLoaderSimple", (-300, -420), ["NoobAI-XL-v1.1.safetensors"],
                 outputs=[("MODEL", "MODEL"), ("CLIP", "CLIP"), ("VAE", "VAE")], size=(320, 100))
    lora = g.add("LoraLoader", (-300, -260), ["pixel-art-xl.safetensors", 0.8, 0.8],
                 inputs=[("model", "MODEL"), ("clip", "CLIP")], outputs=[("MODEL", "MODEL"), ("CLIP", "CLIP")],
                 size=(320, 130), title="Pixel look (LoRA strength)", color=ORANGE)
    pos = g.add("CLIPTextEncode", (60, -420), [PROMPT], inputs=[("clip", "CLIP")],
                outputs=[("CONDITIONING", "CONDITIONING")], size=(420, 200), title="What to draw")
    neg = g.add("CLIPTextEncode", (60, -190), [NEGATIVE], inputs=[("clip", "CLIP")],
                outputs=[("CONDITIONING", "CONDITIONING")], size=(420, 150), title="What to avoid")
    skel = g.add("OpenposeEditorNode", (-300, -100),
                 [True, False, False, -1, 4, 3, 2, 1.0, 1.0, 1.0, 1.0, "poses", "loop extend", 99, bent_pose(-1.0)],
                 inputs=[("POSE_KEYPOINT", "POSE_KEYPOINT")],
                 outputs=[("POSE_IMAGE", "IMAGE"), ("POSE_KEYPOINT", "POSE_KEYPOINT"), ("POSE_JSON", "STRING")],
                 size=(320, 420), title="Skeleton (right-click -> Open in Openpose Editor)", color=GREEN)
    start = g.add("LoadImage", (-300, 350), ["clownpiece_bank_left_start.png", "image"],
                  outputs=[("IMAGE", "IMAGE"), ("MASK", "MASK")], size=(320, 340),
                  title="Start image (colours)", color=YELLOW)
    preview = g.add("PreviewImage", (1180, -420), inputs=[("images", "IMAGE")], size=(260, 300),
                    title="Pose preview")
    cnl = g.add("ControlNetLoader", (60, -10), ["noob_openpose.safetensors"],
                outputs=[("CONTROL_NET", "CONTROL_NET")], size=(420, 60))
    cna = g.add("ControlNetApplyAdvanced", (60, 80), [0.8, 0.0, 1.0],
                inputs=[("positive", "CONDITIONING"), ("negative", "CONDITIONING"), ("control_net", "CONTROL_NET"),
                        ("image", "IMAGE"), ("vae", "VAE")],
                outputs=[("positive", "CONDITIONING"), ("negative", "CONDITIONING")], size=(420, 190),
                title="Pose strength", color=ORANGE)
    enc = g.add("VAEEncode", (60, 320), inputs=[("pixels", "IMAGE"), ("vae", "VAE")],
                outputs=[("LATENT", "LATENT")], size=(200, 50))
    rep = g.add("RepeatLatentBatch", (280, 320), [4], inputs=[("samples", "LATENT")],
                outputs=[("LATENT", "LATENT")], size=(200, 60), title="Attempts per run")
    ks = g.add("KSampler", (540, -420), [1301, "randomize", 28, 5.0, "euler_ancestral", "normal", 0.75],
               inputs=[("model", "MODEL"), ("positive", "CONDITIONING"), ("negative", "CONDITIONING"),
                       ("latent_image", "LATENT")],
               outputs=[("LATENT", "LATENT")], size=(320, 270), title="Seed / Redraw amount (denoise)", color=ORANGE)
    dec = g.add("VAEDecode", (540, -110), inputs=[("samples", "LATENT"), ("vae", "VAE")],
                outputs=[("IMAGE", "IMAGE")], size=(200, 50))
    save = g.add("SaveImage", (540, -20), ["pose_play"], inputs=[("images", "IMAGE")], size=(600, 640))

    g.link(ckpt, 0, lora, 0)
    g.link(ckpt, 1, lora, 1)
    g.link(lora, 1, pos, 0)
    g.link(lora, 1, neg, 0)
    g.link(pos, 0, cna, 0)
    g.link(neg, 0, cna, 1)
    g.link(cnl, 0, cna, 2)
    g.link(skel, 0, cna, 3)
    g.link(skel, 0, preview, 0)
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
