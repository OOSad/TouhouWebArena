"""In-between frames for a bank: re-run a picked pose_play result's exact graph (same seed, same batch slot)
with the skeleton moved partway from neutral and a matching start image.
Usage: python interp.py <pick.png> <batch_index> <pose_to.json> <pose_from.json|neutral> <t> <start_image> <out_name>
  neutral = the average of poses/bank_left.json and poses/bank_right.json.
Writes <out_name>.png (the 1024 image) to the output folder.
"""
import json, os, shutil, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from redraw import COMFY, run  # noqa: E402

POSES = os.path.join(HERE, "clownpiece")


def load_pose(path: str) -> dict:
    p = json.loads(open(path).read())
    return p[0] if isinstance(p, list) else p


def neutral() -> dict:
    left, right = load_pose(os.path.join(POSES, "bank_left.json")), load_pose(os.path.join(POSES, "bank_right.json"))
    return blend(left, right, 0.5)


def blend(a: dict, b: dict, t: float) -> dict:
    """Joint positions t of the way from a to b; a joint hidden in either stays hidden."""
    out = json.loads(json.dumps(a))
    ka, kb = a["people"][0]["pose_keypoints_2d"], b["people"][0]["pose_keypoints_2d"]
    k = out["people"][0]["pose_keypoints_2d"]
    for i in range(0, len(k), 3):
        if ka[i + 2] and kb[i + 2]:
            k[i] = ka[i] + (kb[i] - ka[i]) * t
            k[i + 1] = ka[i + 1] + (kb[i + 1] - ka[i + 1]) * t
        else:
            k[i:i + 3] = [0, 0, 0]
    return out


def main() -> None:
    pick, slot, pose_to, pose_from, t, start, out_name = sys.argv[1:8]
    graph = json.loads(Image.open(pick).info["prompt"])
    to = load_pose(pose_to)
    frm = neutral() if pose_from == "neutral" else load_pose(pose_from)
    pose = blend(frm, to, float(t))
    for node in graph.values():
        if node["class_type"] == "OpenposeEditorNode":
            node["inputs"]["POSE_JSON"] = json.dumps(pose)
        if node["class_type"] == "LoadImage":
            node["inputs"]["image"] = start
        if node["class_type"] == "SaveImage":
            node["inputs"]["filename_prefix"] = "interp_tmp"
    save_id = next(k for k, n in graph.items() if n["class_type"] == "SaveImage"
                   and graph[n["inputs"]["images"][0]]["class_type"] == "VAEDecode")
    outs = run(graph, save_node=save_id)
    dst = os.path.join(COMFY, "output", out_name + ".png")
    shutil.copy(outs[int(slot)], dst)
    print(dst)


if __name__ == "__main__":
    main()
