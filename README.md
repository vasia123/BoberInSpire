# SmartPick — Card & Relic Tier Badges for Slay the Spire 2

![Card tier badges in action](example.png)

![Merchant screen with card and relic badges](example-merchant.png)

A lightweight mod that shows **tier ratings** directly on cards and relics in Slay the Spire 2. Instantly see which picks are S-tier and which are trash — no external overlay needed.

## Features

- **Tier badges on every card** — colored circle (S/A/B/C/D) with a blended score, shown on:
  - Card reward screen (post-combat & events)
  - Deck view
  - Merchant shop
  - Card forge / transform / enchant / remove screens
  - Bundle selection (start of run)
  - Card compendium (library)
- **Tier badges on relics** — shown on:
  - Relic reward screen (post-combat)
  - Merchant shop
  - Treasure room
  - Relic compendium
- **Two card tier sources blended** — combines [Mobalytics](https://mobalytics.gg/slay-the-spire-2/tier-lists/cards) and [slaythespire-2.com](https://slaythespire-2.com/card-tier) tier lists into a single rating
- **Relic tiers** from [Mobalytics](https://mobalytics.gg/slay-the-spire-2/tier-lists/relics) (global, not per-character)
- **All 5 characters supported** — Ironclad, Silent, Defect, Regent, Necrobinder
- **WoW-style color coding:**
  - **S** — Orange (legendary)
  - **A** — Purple (epic)
  - **B** — Blue (rare)
  - **C** — Green (uncommon)
  - **D** — Grey (poor)

## Installation

### From Steam Workshop (coming soon)

Subscribe to the mod on Steam Workshop — it installs automatically.

### Manual installation

1. **Build the mod** (requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) and [Godot 4.5 .NET](https://godotengine.org/download)):

   ```
   dotnet build SmartPick.csproj
   ```

   This copies `SmartPick.dll`, `SmartPick.pck`, and `SmartPick.json` into your game's `mods\` folder.

2. **Enable the mod in-game:** Settings > Modding > Enable **SmartPick** > Restart.

> If the build fails with "file is being used by another process", close the game first.

## Configuration

If your game is not in the default Steam location, create `local.props`:

```xml
<Project>
  <PropertyGroup>
    <STS2GamePath>D:\YourPath\Slay the Spire 2</STS2GamePath>
    <GodotExePath>C:\Path\To\Godot_v4.5-stable_mono_win64.exe</GodotExePath>
  </PropertyGroup>
</Project>
```

## How it works

The mod uses [HarmonyLib](https://github.com/pardeike/Harmony) to hook into game screens and attaches Godot UI elements (tier badges) to card nodes. Card identity is read directly from the game's `CardModel` — no heuristics or name parsing needed.

Tier data from two community sources is embedded in the DLL as JSON resources and blended into a single S/A/B/C/D rating per card per character.

## Data sources

- **[Mobalytics](https://mobalytics.gg/slay-the-spire-2/tier-lists/cards)** — card tier list
- **[slaythespire-2.com](https://slaythespire-2.com/card-tier)** — community wiki card tiers
- **[Mobalytics](https://mobalytics.gg/slay-the-spire-2/tier-lists/relics)** — relic tier list

Tier list JSONs are in `data/tier_lists/`. Replace them to update ratings.

## License

MIT
