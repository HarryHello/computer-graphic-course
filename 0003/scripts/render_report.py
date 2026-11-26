#!/usr/bin/env python3
"""Render report figures and measure the same scene with and without a BVH."""
import hashlib
import json
import statistics
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
binary = root / "build" / "CGAssignment3"
images = root / "report" / "images"
images.mkdir(parents=True, exist_ok=True)

recipes = [
    ("sky", "sky", 640, 32, []),
    ("sphere", "sphere", 640, 64, []),
    ("materials", "materials", 640, 256, []),
    ("pinhole", "defocus", 640, 256, ["--aperture", "0"]),
    ("defocus", "defocus", 640, 256, []),
    ("random", "random", 800, 256, []),
    ("motion-still", "motion", 640, 128, ["--shutter", "0"]),
    ("motion", "motion", 640, 128, []),
    ("textures", "textures", 640, 128, []),
    ("noise", "noise", 640, 128, []),
    ("image", "image", 640, 128, []),
    ("light", "light", 640, 1024, []),
    ("cornell", "cornell", 480, 2048, []),
    ("smoke", "smoke", 480, 1024, []),
    ("final", "final", 600, 2048, []),
]
results = {"renders": [], "benchmark": []}


def run(scene, width, samples, output, extra):
    args = ["--scene", scene, "--width", str(width), "--samples", str(samples),
            "--depth", "40", "--seed", "2026", "--threads", "8", "--output", str(output), *extra]
    process = subprocess.run([str(binary), *args], check=True, capture_output=True, text=True)
    record = json.loads(process.stdout)
    record["threads"] = 8
    record["sha256"] = hashlib.sha256(output.read_bytes()).hexdigest()
    return record


def checkpoint():
    (root / "report" / "results.json").write_text(
        json.dumps(results, ensure_ascii=False, indent=2) + "\n")


for name, scene, width, samples, extra in recipes:
    record = run(scene, width, samples, images / f"{name}.png", extra)
    record["figure"] = name
    record["overrides"] = extra
    results["renders"].append(record)
    checkpoint()
    print(f"{name}: {record['render_s']:.3f} s", flush=True)

# Alternate the order across repetitions, and include construction time separately.
for repetition in range(3):
    for bvh in ([True, False] if repetition % 2 == 0 else [False, True]):
        record = run("random", 320, 16, root / "build" / f"benchmark-{bvh}.png",
                     [] if bvh else ["--no-bvh"])
        record["repetition"] = repetition + 1
        results["benchmark"].append(record)
        checkpoint()
        print(f"benchmark {repetition + 1} bvh={bvh}: {record['render_s']:.3f} s", flush=True)

assert len({record["sha256"] for record in results["benchmark"]}) == 1
results["benchmark_summary"] = {
    "median_bvh_s": statistics.median(r["render_s"] for r in results["benchmark"] if r["bvh"]),
    "median_list_s": statistics.median(r["render_s"] for r in results["benchmark"] if not r["bvh"]),
    "identical_images": True,
}
checkpoint()
print(results["benchmark_summary"], flush=True)
