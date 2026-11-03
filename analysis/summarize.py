"""Summarize experiment-mode data into the study's dependent variables.

Usage:
    python analysis/summarize.py [folder]          every session in the folder
    python analysis/summarize.py [folder] P07      one participant

The folder defaults to PhysicsSandbox/StudyData. Pilot and self-test runs are reported
but left out of the study totals, which is why the run type is printed for every session.
"""
import csv
import glob
import os
import sys
from collections import defaultdict

SCENES = ("scene_same_mass", "scene_buoyancy", "scene_ramp", "scene_lever")


def load(folder, only):
    sessions = defaultdict(list)
    for path in sorted(glob.glob(os.path.join(folder, "*_responses.csv"))):
        tag = os.path.basename(path).split("_")[0]
        if only and tag != only:
            continue
        with open(path, newline="", encoding="utf-8") as f:
            sessions[os.path.basename(path)[: -len("_responses.csv")]] = list(csv.DictReader(f))
    return sessions


def score(rows, phase):
    answered = [r for r in rows if r["phase"] == phase and r["correct"] != ""]
    return sum(r["correct"] == "1" for r in answered), len(answered)


def presence(rows):
    values = [int(r["choice"]) for r in rows if r["phase"] == "presence"]
    if not values:
        return None, None
    # The first four items are presence; the fifth is comfort.
    head = values[:4]
    return sum(head) / len(head), values[4] if len(values) > 4 else None


def task_seconds(rows):
    return sum(float(r["completion_ms"] or 0) for r in rows if r["phase"] in SCENES) / 1000


def describe(name, rows):
    if not rows:
        print(f"{name}: no responses")
        return
    print(f"{name}  run_type={rows[0]['run_type']}  group={rows[0].get('group', rows[0].get('order'))}  rows={len(rows)}")
    falls = sum(int(r["falls"] or 0) for r in rows)
    unfinished = [f"{r['phase']}/{r['item']}={r['outcome']}" for r in rows if r["outcome"] in ("skipped", "timeout")]
    for block in sorted({r["block"] for r in rows if r["block"] not in ("", "0")}):
        part = [r for r in rows if r["block"] == block]
        pre = score(part, "pretest")
        post = score(part, "posttest")
        heavy = score(part, "heaviness")
        scene = [score(part, s) for s in SCENES]
        pres, comfort = presence(part)
        print(f"  block {block}: cue {part[0]['cue'].upper():3} set {part[0]['set']}"
              f"  quiz {pre[0]}/{pre[1]} -> {post[0]}/{post[1]}"
              f"  predictions {sum(s[0] for s in scene)}/{sum(s[1] for s in scene)}"
              f"  heaviness {heavy[0]}/{heavy[1]}"
              f"  presence {pres if pres is None else round(pres, 2)}  comfort {comfort}"
              f"  task time {task_seconds(part):.0f} s")
    print(f"  falls: {falls}   unfinished tasks: {', '.join(unfinished) if unfinished else 'none'}")


def group_table(sessions):
    for cue in ("on", "off"):
        group = [s for s in sessions if any(r["cue"] == cue for r in s)]
        if not group:
            print(f"  cue {cue.upper():3} (0 people): no runs yet")
            continue
        rows = [r for s in group for r in s if r["cue"] == cue]
        pre, post, heavy = score(rows, "pretest"), score(rows, "posttest"), score(rows, "heaviness")
        same = sum(r["choice"] == "They felt the same" for r in rows if r["phase"] == "heaviness")
        ratings = [p for p, _ in (presence(s) for s in group) if p is not None]
        mean_presence = f"{sum(ratings) / len(ratings):.2f}" if ratings else "n/a"
        print(f"  cue {cue.upper():3} ({len(group)} people): pre {pre[0]}/{pre[1]}  post {post[0]}/{post[1]}"
              f"  mean quiz gain {(post[0] - pre[0]) / len(group):+.2f}"
              f"  heaviness {heavy[0]}/{heavy[1]}  felt the same {same}  presence {mean_presence}")


def totals(sessions):
    study = [rows for rows in sessions.values() if rows and rows[0]["run_type"] == "study"]
    print(f"\nStudy participants: {len(study)}")
    if study:
        group_table(study)
        return
    print("No study data yet; pilot and self-test runs are not counted.")
    pilots = [rows for rows in sessions.values() if rows and rows[0]["run_type"] == "pilot"]
    if pilots:
        print("\nSample of the study summary, from pilot runs only (not study data):")
        group_table(pilots)


def main():
    args = sys.argv[1:]
    only = args.pop() if args and args[-1].upper().startswith("P") and not os.path.isdir(args[-1]) else None
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    folder = args[0] if args else os.path.join(root, "PhysicsSandbox", "StudyData")
    sessions = load(folder, only)
    if not sessions:
        print(f"No sessions found in {folder}")
        return
    for name, rows in sessions.items():
        describe(name, rows)
    if not only:
        totals(sessions)


if __name__ == "__main__":
    main()
