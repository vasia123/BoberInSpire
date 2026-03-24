# Card tier lists

## Mobalytics (`mobalytics_cards.json`)

- **Source:** [Mobalytics STS2 card tiers](https://mobalytics.gg/slay-the-spire-2/tier-lists/cards)
- Tiers **S** / **A** / **B** / **C** / **D** per character.

## slaythespire-2.com (`slaythespire2_com_cards.json`)

- **Source:** [STS2 Wiki card tier page](https://slaythespire-2.com/card-tier)
- **Refresh (recommended):** from repo root, `python scripts/scrape_sts2_wiki_tiers.py` (network) — regenerates the JSON from the live page.
- **Manual:** you can still edit `slaythespire2_com_cards.json` directly (same `characters` → `S`/`A`/… shape as Mobalytics). Includes **F** where the site uses it.

## Advisor blending

`python_app/reward_advisor.py` averages numeric scores from **each tier source that recognizes the card** (Mobalytics and/or wiki), then blends that average **~55%** with archetype heuristics **~45%**. The overlay shows **M:** / **W:** tier letters next to the combined score when known.
