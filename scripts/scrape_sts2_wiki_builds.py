#!/usr/bin/env python3
"""
Fetch character build pages from slaythespire-2.com and write structured data + markdown.

    python scripts/scrape_sts2_wiki_builds.py

Requires network. Output (under data/build_guides/<character>/):
  wiki_builds.json, wiki_builds.md
  (e.g. build_guides/ironclad/wiki_builds.json)
"""
from __future__ import annotations

import html as html_lib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, urlopen

BASE = "https://slaythespire-2.com"
GUIDES_DIR = Path(__file__).resolve().parent.parent / "data" / "build_guides"

# (display character name, subfolder under build_guides/, build URL slugs)
SCRAPE_TARGETS: list[tuple[str, str, list[str]]] = [
    (
        "Ironclad",
        "ironclad",
        [
            "ironclad-strength-build",
            "ironclad-barricade-build",
            "ironclad-exhaust-build",
            "ironclad-bloodletting-build",
            "ironclad-strike-build",
            "ironclad-self-wound-build",
        ],
    ),
    (
        "Silent",
        "silent",
        [
            "silent-poison-build",
            "silent-sly-build",
            "silent-grand-finale-build",
            "silent-shiv-build",
            "silent-envenom-build",
            "silent-combo-build",
        ],
    ),
    (
        "Defect",
        "defect",
        [
            "defect-lightning-build",
            "defect-claw-build",
            "defect-frost-build",
            "defect-dark-orb-build",
            "defect-creative-ai-build",
        ],
    ),
    (
        "Regent",
        "regent",
        [
            "regent-forge-build",
            "regent-star-burst-build",
            "regent-void-form-build",
            "regent-bombardment-build",
        ],
    ),
    (
        "Necrobinder",
        "necrobinder",
        [
            "necrobinder-osty-build",
            "necrobinder-soul-build",
            "necrobinder-doom-build",
            "necrobinder-reaper-build",
        ],
    ),
]

H2 = re.compile(r'<h2 class="mb-5 text-2xl font-bold">([^<]+)</h2>')

_SLUG_SPECIAL: dict[str, str] = {
    "pacts-end": "Pact's End",
    "times-up": "Time's Up",
    "banshees-cry": "Banshee's Cry",
    "monarchs-gaze": "Monarch's Gaze",
    "strike-necrobinder": "Strike",
    "multi-cast": "Multi-Cast",
    "all-for-one": "All for One",
    "charge": "CHARGE!!",
    "guards": "GUARDS!!!",
    "begone": "BEGONE!",
    "expect-a-fight": "Expect a Fight",
    "ftl": "FTL",
    "null": "Null",
    "fight-me": "Fight Me!",
    "creative-ai": "Creative AI",
    "decisions-decisions": "Decisions, Decisions",
    "beat-into-shape": "Beat into Shape",
    "end-of-days": "End of Days",
    "dolly-s-mirror": "Dolly's Mirror",
}


def slug_to_display_name(slug: str) -> str:
    s = slug.strip().lower()
    if s in _SLUG_SPECIAL:
        return _SLUG_SPECIAL[s]
    parts = s.split("-")
    return " ".join(p.capitalize() for p in parts)


def fetch_html(url: str) -> str:
    req = Request(url, headers={"User-Agent": "Mozilla/5.0 BoberInSpire-build-scraper/1.0"})
    with urlopen(req, timeout=90) as resp:
        return resp.read().decode("utf-8", errors="replace")


def slice_after_h2(html: str, title: str) -> tuple[int, int]:
    needle = f'<h2 class="mb-5 text-2xl font-bold">{title}</h2>'
    start = html.find(needle)
    if start < 0:
        return -1, -1
    body_start = start + len(needle)
    next_h2 = H2.search(html, body_start)
    end = next_h2.start() if next_h2 else len(html)
    return body_start, end


def parse_title_description(html: str) -> tuple[str, str]:
    m = re.search(r'<h1[^>]*>([^<]+)</h1>\s*<p[^>]*>([^<]+)</p>', html)
    if m:
        return html_lib.unescape(m.group(1).strip()), html_lib.unescape(m.group(2).strip())
    return "", ""


def parse_build_tier(html: str) -> str:
    m = re.search(
        r'font-bold text-primary">([SABCD])</div><div class="mt-1 text-xs uppercase tracking-wider text-muted-foreground">Tier Rank',
        html,
    )
    return m.group(1).upper() if m else ""


def parse_core_cards(section_html: str) -> list[dict[str, str]]:
    out: list[dict[str, str]] = []
    seen: set[str] = set()
    for sm in re.finditer(r'<span class="font-medium">([^<]+)</span>', section_html):
        name = html_lib.unescape(sm.group(1).strip())
        key = name.lower()
        if name and key not in seen:
            seen.add(key)
            out.append({"name": name, "slug": ""})
    for am in re.finditer(r'href="/cards/([^"]+)"', section_html):
        slug = am.group(1).strip().lower()
        if not slug:
            continue
        frag = section_html[am.end() : am.end() + 600]
        alt_m = re.search(r'<img[^>]+alt="([^"]+)"', frag)
        if alt_m:
            name = html_lib.unescape(alt_m.group(1).strip())
        else:
            link_m = re.search(r">([^<]+)</a>", frag)
            name = (
                html_lib.unescape(link_m.group(1).strip())
                if link_m
                else slug_to_display_name(slug)
            )
        key = name.lower()
        if key not in seen:
            seen.add(key)
            out.append({"name": name, "slug": slug})
    return out


def normalize_priority_label(label: str) -> str:
    t = label.strip().lower().replace(" ", "_")
    if t == "must_pick":
        return "must_pick"
    if t in ("high", "medium", "low"):
        return t
    return t


def parse_acquisition(section_html: str) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    pattern = re.compile(
        r'<span[^>]*>(Must Pick|High|Medium|Low)</span>'
        r'<div class="min-w-0"><a[^>]*href="/cards/([^"]+)"[^>]*>([^<]+)</a>'
        r'<p class="mt-0\.5 text-sm text-muted-foreground">([^<]*)</p>',
    )
    for m in pattern.finditer(section_html):
        slug = m.group(2).strip().lower()
        name = html_lib.unescape(m.group(3).strip()) or slug_to_display_name(slug)
        note = html_lib.unescape(m.group(4).strip())
        rows.append(
            {
                "priority": normalize_priority_label(m.group(1)),
                "name": name,
                "slug": slug,
                "note": note,
            }
        )
    return rows


def parse_flex_cards(section_html: str) -> list[dict[str, str]]:
    out: list[dict[str, str]] = []
    seen: set[str] = set()
    for m in re.finditer(r'href="/cards/([^"]+)"', section_html):
        slug = m.group(1).strip().lower()
        if not slug:
            continue
        name = slug_to_display_name(slug)
        key = slug
        if key not in seen:
            seen.add(key)
            out.append({"name": name, "slug": slug})
    return out


def parse_relic_columns(section_html: str) -> dict[str, list[dict[str, str]]]:
    result: dict[str, list[dict[str, str]]] = {"best": [], "good": [], "avoid": []}
    borders = [
        ("border-green-500/30", "best"),
        ("border-yellow-500/30", "good"),
        ("border-red-500/30", "avoid"),
    ]
    row_re = re.compile(
        r'<div class="text-sm font-semibold">([^<]+)</div>'
        r'<p class="text-xs text-muted-foreground">([^<]*)</p>',
    )
    for idx, (border, key) in enumerate(borders):
        start = section_html.find(f"rounded-xl border {border} bg-card p-5")
        if start < 0:
            continue
        end = len(section_html)
        for _b, _ in borders[idx + 1 :]:
            j = section_html.find(f"rounded-xl border {_b} bg-card p-5", start + 1)
            if j >= 0:
                end = min(end, j)
        col_html = section_html[start:end]
        inner_m = re.search(r'<div class="mt-4 space-y-3">', col_html)
        if not inner_m:
            continue
        block = col_html[inner_m.end() :]
        for sm in row_re.finditer(block):
            name = html_lib.unescape(sm.group(1).strip())
            note = html_lib.unescape(sm.group(2).strip())
            if name and not any(r["name"] == name for r in result[key]):
                result[key].append({"name": name, "note": note})
    return result


def parse_key_relics_sidebar(html: str) -> list[dict[str, str]]:
    needle = '">Key Relics</h3><div class="flex flex-col gap-3">'
    start = html.find(needle)
    if start < 0:
        return []
    start += len(needle)
    end = html.find("</div></section>", start)
    if end < 0:
        return []
    block = html[start:end]
    out: list[dict[str, str]] = []
    for m in re.finditer(
        r'href="/relics/([^"]+)"[^>]*>.*?<div class="font-semibold[^"]*">([^<]+)</div>'
        r'<p class="mt-0\.5 text-xs text-muted-foreground[^"]*">([^<]*)</p>',
        block,
        re.DOTALL,
    ):
        slug = m.group(1).strip().lower()
        name = html_lib.unescape(m.group(2).strip())
        note = html_lib.unescape(m.group(3).strip())
        out.append({"name": name, "slug": slug, "note": note})
    return out


def parse_build_page(url: str, html: str) -> dict:
    title, description = parse_title_description(html)
    tier = parse_build_tier(html)

    core_start, core_end = slice_after_h2(html, "Core Cards")
    core_html = html[core_start:core_end] if core_start >= 0 else ""
    core_cards = parse_core_cards(core_html)

    acq_start, acq_end = slice_after_h2(html, "Card Acquisition Priority")
    acq_html = html[acq_start:acq_end] if acq_start >= 0 else ""
    acquisition = parse_acquisition(acq_html)

    flex_start, flex_end = slice_after_h2(html, "Flex / Alternative Cards")
    flex_html = html[flex_start:flex_end] if flex_start >= 0 else ""
    flex_cards = parse_flex_cards(flex_html)

    rel_start, rel_end = slice_after_h2(html, "Relic Priority")
    rel_html = html[rel_start:rel_end] if rel_start >= 0 else ""
    relics = parse_relic_columns(rel_html)
    key_relics = parse_key_relics_sidebar(html)

    slug = url.rstrip("/").split("/")[-1]
    return {
        "id": slug,
        "url": url,
        "title": title,
        "build_tier": tier,
        "description": description,
        "core_cards": core_cards,
        "card_acquisition_priority": acquisition,
        "flex_cards": flex_cards,
        "relic_priority": relics,
        "key_relics_sidebar": key_relics,
    }


def md_escape_cell(s: str) -> str:
    return s.replace("|", "\\|").replace("\n", " ")


def write_markdown(payload: dict) -> str:
    lines: list[str] = []
    char = payload.get("character", "Character")
    lines.append(f"# {char} builds — slaythespire-2.com")
    lines.append("")
    lines.append(f"> Source: {payload['source']}")
    lines.append(f"> Generated: {payload['generated_at']}")
    lines.append("")
    lines.append(
        "Structured copy for the reward advisor; refresh with `python scripts/scrape_sts2_wiki_builds.py`."
    )
    lines.append("")
    for b in payload["builds"]:
        lines.append(f"## {b['title']} ({b['build_tier']}-Tier)")
        lines.append("")
        lines.append(f"**URL:** {b['url']}")
        lines.append("")
        if b.get("description"):
            lines.append(b["description"])
            lines.append("")
        lines.append("### Core cards")
        lines.append("")
        for c in b["core_cards"]:
            slug = f" (`{c['slug']}`)" if c.get("slug") else ""
            lines.append(f"- {c['name']}{slug}")
        lines.append("")
        lines.append("### Card acquisition priority")
        lines.append("")
        lines.append("| Priority | Card | Note |")
        lines.append("| --- | --- | --- |")
        for row in b["card_acquisition_priority"]:
            lines.append(
                f"| {row['priority']} | {md_escape_cell(row['name'])} | {md_escape_cell(row.get('note', ''))} |"
            )
        lines.append("")
        lines.append("### Flex / alternative cards")
        lines.append("")
        for c in b["flex_cards"]:
            lines.append(f"- {c['name']} (`{c['slug']}`)")
        lines.append("")
        lines.append("### Relic priority")
        lines.append("")
        for label, key in (("Best", "best"), ("Good", "good"), ("Avoid", "avoid")):
            items = b["relic_priority"].get(key, [])
            if not items:
                continue
            lines.append(f"**{label}**")
            lines.append("")
            for r in items:
                lines.append(f"- **{r['name']}** — {r.get('note', '')}")
            lines.append("")
        if b.get("key_relics_sidebar"):
            lines.append("### Key relics (sidebar)")
            lines.append("")
            for r in b["key_relics_sidebar"]:
                lines.append(f"- **{r['name']}** — {r.get('note', '')}")
            lines.append("")
        lines.append("---")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    GUIDES_DIR.mkdir(parents=True, exist_ok=True)

    for character, subdir, slugs in SCRAPE_TARGETS:
        builds: list[dict] = []
        for slug in slugs:
            url = f"{BASE}/builds/{slug}"
            try:
                html = fetch_html(url)
            except Exception as e:
                print(f"Fetch failed {url}: {e}", file=sys.stderr)
                return 1
            if "/cards/" not in html and "Core Cards" not in html:
                print(f"Unexpected HTML for {url}", file=sys.stderr)
                return 1
            builds.append(parse_build_page(url, html))

        payload = {
            "source": BASE + "/builds",
            "character": character,
            "generated_at": now,
            "note": "Generated by scripts/scrape_sts2_wiki_builds.py; re-run to refresh.",
            "builds": builds,
        }
        out_dir = GUIDES_DIR / subdir
        out_dir.mkdir(parents=True, exist_ok=True)
        out_json = out_dir / "wiki_builds.json"
        out_md = out_dir / "wiki_builds.md"
        out_json.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        out_md.write_text(write_markdown(payload), encoding="utf-8")
        print(f"Wrote {out_json} ({len(builds)} builds)")
        print(f"Wrote {out_md}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
