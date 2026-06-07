# Lunacid QoL Mod

A [BepInEx 5](https://github.com/BepInEx/BepInEx) plugin for [Lunacid](https://store.steampowered.com/app/1745510/LUNACID/) that adds quality-of-life improvements via Harmony patches — no game file modifications required.

## Features

| Feature | Config key | Default |
|---|---|---|
| Respawn on death instead of game-over screen | `Gameplay.RespawnOnDeath` | `true` |
| Jump during weapon attacks | `Gameplay.JumpDuringAttack` | `true` |
| HUD transparency | `HUD.HudAlpha` | `0.8` |
| Ultrawide (21:9+) overlay and canvas fixes | `Display.UltrawideFix` | `true` |
| Custom input bindings via InputActionAsset JSON | `Input.InputOverridePath` | *(empty)* |

---

## Installation

### Windows

1. Download **BepInEx 5.4.x Windows x64** from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases).
2. Extract the zip into your Lunacid game folder so that `BepInEx/`, `winhttp.dll`, and `doorstop_config.ini` sit alongside `LUNACID.exe`.
3. Drop `LunacidQoLMod.dll` into `BepInEx/plugins/`.
4. Launch the game normally — BepInEx loads automatically.

### Linux (Steam / Proton)

Lunacid ships as a Windows build and runs through Proton.

1. Download **BepInEx 5.4.x Windows x64** from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases).
2. Extract into the Lunacid game folder (same as above).
3. Drop `LunacidQoLMod.dll` into `BepInEx/plugins/`.
4. Open Steam → Lunacid → **Properties → General → Launch Options** and add the appropriate line for your Proton version:

   | Proton version | Launch option |
   |---|---|
   | Proton 8 and earlier | `WINEDLLOVERRIDES="winhttp=n,b" %command%` |
   | Proton 9 / Proton Experimental | `WINEDLLOVERRIDES="winhttp=n,b" %command%` |
   | GE-Proton | `WINEDLLOVERRIDES="winhttp=n,b" %command%` |
   | CachyOS Proton (proton-cachyos-slr) | `WINEDLLOVERRIDES="winhttp=n,b" %command%` |

   If you are unsure which version you are on, the line `WINEDLLOVERRIDES="winhttp=n,b" %command%` works across all current Proton releases. `n,b` means *native first, fall back to builtin* — it tells Wine to load the `winhttp.dll` shipped with BepInEx rather than its own built-in implementation.

Default game path on Linux:
```
~/.local/share/Steam/steamapps/common/Lunacid/
```

### Verifying the install

On first launch BepInEx creates two files:

- `BepInEx/LogOutput.log` — check for `Loading [LunacidQoLMod 1.0.0]` to confirm the plugin loaded
- `BepInEx/config/crycode4650.lunacid.qolmod.cfg` — edit this to configure each feature

---

## Configuration

All settings are in `BepInEx/config/crycode4650.lunacid.qolmod.cfg`. Changes take effect on the next game launch.

### `[HUD]`

| Key | Type | Default | Description |
|---|---|---|---|
| `HudAlpha` | float (0–1) | `0.8` | In-game HUD opacity. `1.0` = fully opaque, `0.0` = invisible. |

### `[Display]`

| Key | Type | Default | Description |
|---|---|---|---|
| `UltrawideFix` | bool | `true` | Disables `AspectRatioFitter` on fullscreen overlays and switches canvas scalers to height-match mode for 21:9+ displays. |

### `[Gameplay]`

| Key | Type | Default | Description |
|---|---|---|---|
| `RespawnOnDeath` | bool | `true` | Skip the death screen and reload the current area on death. |
| `JumpDuringAttack` | bool | `true` | Allow jumping during a weapon attack animation. |

### `[Input]`

| Key | Type | Default | Description |
|---|---|---|---|
| `InputOverridePath` | string | *(empty)* | Absolute path to an `InputActionAsset` JSON file. Replaces the game's default bindings on startup. Leave empty to use game defaults. |

A template with the vanilla bindings is included: [`LunacidQoLMod-InputOverrides.json`](LunacidQoLMod-InputOverrides.json).

---

## Building from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Lunacid installed via Steam (app ID `1745510`)
- BepInEx 5.4.x installed in the Lunacid folder

### Steps

```sh
git clone https://github.com/starfox8439/lunacid-qol-mod.git
cd lunacid-qol-mod

# Copy the game assembly reference (not committed — copyright)
mkdir -p LUNACID_Data/Managed
cp /path/to/Lunacid/LUNACID_Data/Managed/Assembly-CSharp.dll LUNACID_Data/Managed/

dotnet build -c Release
# → bin/Release/netstandard2.1/LunacidQoLMod.dll
```

Unity engine DLLs and BepInEx core DLLs are resolved from `GamePath`, which defaults to the standard Steam install path on Linux. Override with:
```sh
dotnet build -c Release /p:GamePath="C:\Program Files (x86)\Steam\steamapps\common\Lunacid"
```

### CI / GitHub Actions

The `build` workflow runs a NuGet restore on every push (no credentials required). The full build job downloads game DLLs via [DepotDownloader](https://github.com/SteamRE/DepotDownloader) and requires `STEAM_USERNAME` and `STEAM_PASSWORD` secrets configured in your repository settings (Settings → Secrets and variables → Actions). The account must own Lunacid.

---

## Maintenance

This project is **not actively maintained**. No issues will be monitored or pull requests reviewed. If you want to extend or fix it, please fork the repository and carry it forward.

## Credits

The original Lunacid QoL mod was created by **MoArtis** — see the [original Steam discussion](https://steamcommunity.com/app/1745510/discussions/0/6333797474860240085/). This repository ports that mod to BepInEx 5 + Harmony so it works without modifying the game's DLLs.

## License

[MIT](LICENSE) — © 2026 crycode4650
