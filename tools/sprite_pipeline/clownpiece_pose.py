"""Back-view OpenPose skeleton for Clownpiece's 48x48 idle frame, on the 1024 canvas the other guides use
(64x64 canvas at 16x, frame at (8, 12)). Prints the POSE_JSON string.

Seen from behind, her own right side is on the viewer's right (the torch hand). The face points (nose, eyes)
are left out, so only the ears mark the head: that is how OpenPose tells a back view from a front one.
"""
import json

# (x, y) in the 48x48 frame, by COCO-18 index; None = not visible.
KEYPOINTS = {
    0: None,         # nose
    1: (24, 18),     # neck
    2: (29, 19),     # right shoulder (viewer's right)
    3: (32, 22),     # right elbow
    4: (34, 24),     # right wrist, holding the torch
    5: (19, 19),     # left shoulder
    6: (17, 23),     # left elbow
    7: (16, 27),     # left wrist
    8: (27, 31),     # right hip
    9: (27, 38),     # right knee
    10: (26, 45),    # right ankle
    11: (21, 31),    # left hip
    12: (21, 38),    # left knee
    13: (21, 45),    # left ankle
    14: None,        # right eye
    15: None,        # left eye
    16: (28, 13),    # right ear
    17: (20, 13),    # left ear
}
FRAME_AT, SCALE, CANVAS = (8, 12), 16, 1024


def pose_json() -> str:
    flat = []
    for i in range(18):
        p = KEYPOINTS[i]
        if p is None:
            flat += [0, 0, 0]
        else:
            flat += [(p[0] + FRAME_AT[0]) * SCALE, (p[1] + FRAME_AT[1]) * SCALE, 1]
    return json.dumps({"people": [{"pose_keypoints_2d": flat}], "canvas_width": CANVAS, "canvas_height": CANVAS})


if __name__ == "__main__":
    print(pose_json())
