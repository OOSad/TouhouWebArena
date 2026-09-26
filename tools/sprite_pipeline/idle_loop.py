"""8-frame idle loop in PoFV's manner: the body holds still (pl00's idle centre moves < 0.5 px) while
details shimmer. Each frame re-runs the idle pick's exact graph (same seed and batch slot) with the neutral
skeleton swayed about a pixel along a looping sine; the redraw varies hair, wings and ribbons by itself.
Usage: python idle_loop.py <pick.png> <batch_index> <start_image> <out_prefix>
"""
import json, math, os, shutil, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from interp import neutral  # noqa: E402
from redraw import COMFY, run  # noqa: E402

FRAMES = 8
# The editor canvas (512x768) is stretched onto the 1024x1024 image, which is 64 sprite pixels across:
# one sprite pixel is 8 pose units across and 12 down.
PX_X, PX_Y = 8.0, 12.0
# Sway per joint in sprite pixels: (dx amplitude, dy amplitude, phase offset in turns).
SWAY = {
    4: (0.0, 1.0, 0.00), 7: (0.0, 1.0, 0.50),   # wrists rise and fall in turn
    3: (0.0, 0.5, 0.00), 6: (0.0, 0.5, 0.50),   # elbows follow at half
    9: (0.7, 0.0, 0.25), 12: (0.7, 0.0, 0.25),  # knees drift together
    10: (1.2, 0.0, 0.35), 13: (1.2, 0.0, 0.35),  # ankles drift a little further and later
}


def frame_pose(i: int) -> dict:
    pose = neutral()
    k = pose["people"][0]["pose_keypoints_2d"]
    for joint, (ax, ay, phase) in SWAY.items():
        if k[joint * 3 + 2]:
            s = math.sin(2 * math.pi * (i / FRAMES + phase))
            k[joint * 3] += ax * s * PX_X
            k[joint * 3 + 1] += ay * s * PX_Y
    return pose


def main() -> None:
    pick, slot, start, prefix = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
    base = json.loads(Image.open(pick).info["prompt"])
    for i in range(FRAMES):
        graph = json.loads(json.dumps(base))
        for node in graph.values():
            if node["class_type"] == "OpenposeEditorNode":
                node["inputs"]["POSE_JSON"] = json.dumps(frame_pose(i))
            if node["class_type"] == "LoadImage":
                node["inputs"]["image"] = start
            if node["class_type"] == "SaveImage":
                node["inputs"]["filename_prefix"] = "idle_tmp"
        save_id = next(k for k, n in graph.items() if n["class_type"] == "SaveImage"
                       and graph[n["inputs"]["images"][0]]["class_type"] == "VAEDecode")
        outs = run(graph, save_node=save_id)
        dst = os.path.join(COMFY, "output", f"{prefix}_{i}.png")
        shutil.copy(outs[slot], dst)
        print(dst)


if __name__ == "__main__":
    main()
