# BoberInSpire — Card Tier Badges for Slay the Spire 2

![Card tier badges in action](example.png)

A lightweight mod that shows **card tier ratings** directly on cards in Slay the Spire 2. Instantly see which cards are S-tier picks and which are trash — no external overlay needed.

## Features

- **Tier badges on every card** — colored circle (S/A/B/C/D) with a blended score, shown on:
  - Card reward screen (post-combat & events)
  - Deck view
  - Merchant shop
- **Two tier sources blended** — combines [Mobalytics](https://mobalytics.gg/slay-the-spire-2/tier-lists/cards) and [slaythespire-2.com](https://slaythespire-2.com/card-tier) tier lists into a single rating
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
   dotnet build STS2Mods\sts2_example_mod\ExampleMod.csproj
   ```

   This copies `BoberInSpire.dll`, `BoberInSpire.pck`, and `BoberInSpire.json` into your game's `mods\` folder.

2. **Enable the mod in-game:** Settings > Modding > Enable **BoberInSpire** > Restart.

> If the build fails with "file is being used by another process", close the game first.

## Configuration

If your game is not in the default Steam location, create `STS2Mods/sts2_example_mod/local.props`:

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

Tier list JSONs are in `data/tier_lists/`. Replace them to update ratings.

## License

MIT
