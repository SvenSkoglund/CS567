"""Builds a four-screen page from one session's log files, to show the data in a video.

Usage:
    python analysis/data_page.py P95

The tables quote the saved files directly. Writes video/checkpoint5-data.html.
"""
import csv
import glob
import html
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "PhysicsSandbox", "StudyData")
OUTPUT = os.path.join(ROOT, "video", "checkpoint5-data.html")
SCENES = ("scene_same_mass", "scene_buoyancy", "scene_ramp", "scene_lever")
PHASE_NAMES = {
    "training": "Training", "pretest": "Quiz 1", "heaviness": "Lifting pair", "posttest": "Quiz 2", "presence": "Rating",
    "scene_same_mass": "Task: compare", "scene_buoyancy": "Task: tank", "scene_ramp": "Task: ramp", "scene_lever": "Task: lever",
}

PAGE = """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Saved session data</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Libre+Baskerville:wght@700&family=Public+Sans:wght@400;600;700&display=swap">
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
html, body { height: 100%; background: #14202E; overflow: hidden; }
section { position: absolute; left: 0; top: 0; width: 1920px; height: 1080px; transform-origin: top left; display: none;
  background: #F6F3EC; color: #14202E; font-family: 'Public Sans', Arial, sans-serif; padding: 44px 64px; overflow: hidden; }
h2 { font-family: 'Libre Baskerville', Georgia, serif; font-size: 42px; line-height: 1.15; }
.file { font: 22px Consolas, monospace; color: #A8470D; margin: 10px 0 18px; }
.narrow { width: 1320px; }
table { border-collapse: collapse; font-size: 21px; }
th { text-align: left; padding: 6px 28px 6px 0; color: #5A6673; border-bottom: 2px solid #14202E; }
td { padding: 4px 28px 4px 0; border-bottom: 1px solid #DDD6C8; white-space: nowrap; }
td.num, th.num { text-align: right; }
pre { font: 25px/1.5 Consolas, monospace; white-space: pre-wrap; background: #14202E; color: #F6F3EC; padding: 26px 32px; border-radius: 14px; }
.lead { font-size: 26px; color: #2E3B4A; margin-bottom: 18px; }
h3 { font-size: 26px; margin: 22px 0 10px; }
#hint { position: fixed; left: 50%; bottom: 16px; transform: translateX(-50%); padding: 10px 18px; border-radius: 8px; background: #14202E; color: #F6F3EC; font: 16px Arial, sans-serif; transition: opacity .3s; }
:fullscreen #hint { opacity: 0; }
</style>
</head>
<body>
BODY
<div id="hint">Press F for full screen. Arrow keys change screens.</div>
<script>
var slides = document.querySelectorAll("section"), current = 0;
function fit() {
  var scale = Math.min(innerWidth / 1920, innerHeight / 1080);
  slides.forEach(function (s) {
    s.style.transform = "translate(" + (innerWidth - 1920 * scale) / 2 + "px," + (innerHeight - 1080 * scale) / 2 + "px) scale(" + scale + ")";
  });
}
function show(n) {
  current = Math.max(0, Math.min(slides.length - 1, n));
  slides.forEach(function (s, i) { s.style.display = i === current ? "block" : "none"; });
}
addEventListener("resize", fit);
addEventListener("keydown", function (e) {
  if (e.key === "ArrowRight" || e.key === "PageDown" || e.key === " ") show(current + 1);
  if (e.key === "ArrowLeft" || e.key === "PageUp") show(current - 1);
  if (e.key === "f" || e.key === "F") document.documentElement.requestFullscreen();
});
fit();
show(0);
</script>
</body>
</html>
"""


def table(headers, rows, numeric=()):
    head = "".join(f'<th class="num">{h}</th>' if h in numeric else f"<th>{h}</th>" for h in headers)
    body = ""
    for row in rows:
        cells = "".join(f'<td class="num">{html.escape(str(v))}</td>' if h in numeric else f"<td>{html.escape(str(v))}</td>"
                        for h, v in zip(headers, row))
        body += f"<tr>{cells}</tr>\n"
    return f'<table class="narrow"><tr>{head}</tr>\n{body}</table>'


def seconds(ms):
    return f"{float(ms) / 1000:.1f}" if ms else ""


def verdict(row):
    return {"1": "correct", "0": "wrong"}.get(row["correct"], "")


def screen(title, file_name, content):
    return f'<section>\n<h2>{title}</h2>\n<p class="file">{html.escape(file_name)}</p>\n{content}\n</section>'


def main():
    tag = sys.argv[1].upper() if len(sys.argv) > 1 else "P95"
    prefix = sorted(glob.glob(os.path.join(DATA, f"{tag}_*_session.txt")))[-1][: -len("_session.txt")]
    name = os.path.basename(prefix)
    with open(prefix + "_responses.csv", newline="", encoding="utf-8") as f:
        responses = list(csv.DictReader(f))
    with open(prefix + "_interactions.csv", newline="", encoding="utf-8") as f:
        interactions = list(csv.DictReader(f))
    session = open(prefix + "_session.txt", encoding="utf-8").read().strip()
    analysis = subprocess.run([sys.executable, os.path.join(ROOT, "analysis", "summarize.py")], capture_output=True, text=True, check=True).stdout.strip()

    answers = table(
        ["Part", "Item", "Answer", "Result", "Seconds"],
        [[PHASE_NAMES.get(r["phase"], r["phase"]), r["item"], r["choice"][:30], verdict(r), seconds(r["rt_ms"])] for r in responses],
        numeric=("Seconds",))

    grabs = table(
        ["Part", "Object", "Mass (kg)", "Hold (s)", "Hand rose (cm)", "Peak offset (cm)", "Gain", "Lag (ms)"],
        [[PHASE_NAMES.get(r["phase"], r["phase"]), r["object"], r["mass_kg"], seconds(r["hold_ms"]),
          f"{float(r['max_hand_lift_m']) * 100:.0f}", f"{float(r['max_offset_m']) * 100:.1f}", r["gain"], r["lag_ms"]] for r in interactions],
        numeric=("Mass (kg)", "Hold (s)", "Hand rose (cm)", "Peak offset (cm)", "Gain", "Lag (ms)"))

    measures = table(
        ["Task", "Item", "Measures"],
        [[PHASE_NAMES[r["phase"]], r["item"], r["measures"].replace(";", "   ")[:88]] for r in responses if r["phase"] in SCENES])

    body = "\n".join([
        screen(f"Every answer is logged: {len(responses)} rows", f"{name}_responses.csv", answers),
        screen(f"Every grab is logged: {len(interactions)} rows", f"{name}_interactions.csv", grabs),
        screen("Session details and task measures", f"{name}_session.txt, and the measures column of the responses file",
               f"<pre>{html.escape(session)}</pre>\n<h3>Task measures</h3>\n{measures}"),
        screen("From the log to the study's measures", "python analysis/summarize.py", f"<pre>{html.escape(analysis)}</pre>"),
    ])
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(PAGE.replace("BODY", body))
    print("wrote", OUTPUT, "from", name)


if __name__ == "__main__":
    main()
