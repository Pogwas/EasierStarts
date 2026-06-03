# Easier Starts

A run-start quality-of-life mod for [R.E.P.O.](https://store.steampowered.com/app/3241660/REPO/) (Semiwork, 2025). Takes the edge off the early game — for **any lobby size**, not just solo.

Every gameplay value is exposed as a config entry. Tune to taste.

## Features

- **Free Defibros at the truck** — Auto-grants the vanilla DEFIBRO revive bot at the truck each level, so a fresh run isn't gated behind its ~$44,000 shop price. The number granted scales with your lobby (a flat base plus a per-player amount). Includes an optional **Defibro shop-price override** if you'd rather just make it cheaper than free.
- **Free Items at run start** — Spawns a configurable list of items at the truck. By default you start with a **Tranq Gun per player on level 1** — a leg-up past the bad early-shop RNG (no gun in the first shops) without every-level power creep. The list takes any item by name, with optional per-player scaling, and can grant either only on level 1 or every level.

Both features are independent — enable either, both, or neither. The mod is general-purpose: it is **not** solo-gated and works for any number of players.

## Installation

1. Install [BepInEx 5.4](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) for R.E.P.O.
2. Drop `EasierStarts.dll` into `BepInEx/plugins/`.
3. Launch the game once to generate `BepInEx/config/com.pogwas.easierstarts.cfg`, then edit it to taste — or use [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) for an in-game UI.

> **Multiplayer note:** item/Defibro spawning is performed by the host. Install it on the host for it to take effect in a multiplayer lobby.

## Configuration sections

| Section | What it controls |
|---|---|
| `Defibro` | Free Defibro count at the truck (`DefibrosBase` + `DefibrosPerPlayer` × players) and the Defibro shop price (`StorePrice`, in thousands; 0 = vanilla) |
| `Free Items` | The free-item grant: master toggle, the item list, and whether it grants only on level 1 or every level (`FirstLevelOnly`) |

### `[Free Items]` list grammar

The `Items` setting is a comma-separated list. Each entry is:

- `name:count` — grant `count` of the item (a flat total), or
- `name:count/player` — grant `count` per player (scales with lobby size).

`name` matches an item's asset name or display name — e.g. `Item Gun Tranq` (or just `tranq`). Whitespace is ignored, and a bad entry is skipped with a warning rather than breaking the rest of the list. Leave `Items` empty to grant nothing.

Example: `Item Gun Tranq:1/player, Item Health Pack Small:2`

## Bug reports

Please open an [Issue](https://github.com/Pogwas/EasierStarts/issues) and include:

- R.E.P.O. game version
- Mod version
- Your `BepInEx/LogOutput.log` (or the relevant ~50 lines around the bug)
- Other plugins installed
- Steps to reproduce

## Changelog

### 0.2.0

- **Free Items** — new `[Free Items]` feature: a configurable list of items spawned free at the truck at run start. Default grants one Tranq Gun per player on level 1. Grammar `name:count` / `name:count/player`, with a `FirstLevelOnly` toggle (default on).

### 0.1.0

- Initial release. **Free Defibros** at the truck each level (count scales with lobby size: base + per-player) plus a configurable Defibro shop-price override.

## License

MIT — see [LICENSE](LICENSE).
