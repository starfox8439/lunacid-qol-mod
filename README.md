# Lunacid QoL Mod

A BepInEx 5 + Harmony plugin for [Lunacid](https://store.steampowered.com/app/1745510/LUNACID/) that adds quality-of-life improvements without modifying the game's DLLs.

## Features

| Feature | Config key | Default |
|---|---|---|
| Respawn on death instead of game-over screen | `Gameplay.RespawnOnDeath` | `true` |
| Jump during weapon attacks | `Gameplay.JumpDuringAttack` | `true` |
| HUD transparency | `HUD.HudAlpha` | `0.8` |
| Ultrawide (21:9+) fixes for overlays, popups, and canvas scaling | `Display.UltrawideFix` | `true` |
| Custom input bindings via InputActionAsset JSON | `Input.InputOverridePath` | *(empty)* |

## Installation

### 1. Install BepInEx

Download **BepInEx 5.4.x Windows x64** from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases) and extract it into your Lunacid game folder so that `BepInEx/`, `winhttp.dll`, and `doorstop_config.ini` sit alongside `LUNACID.exe`.

Default game path on Steam (Linux):
```
~/.local/share/Steam/steamapps/common/Lunacid/
```

### 2. Drop in the plugin

Copy `LunacidQoLMod.dll` into `BepInEx/plugins/`.

### 3. Configure Steam launch options (non-CachyOS Proton)

Lunacid is a Windows build that runs through Proton. BepInEx uses `winhttp.dll` as its injection point. Most Proton versions require an explicit hint to prefer the local DLL over Wine's built-in one.

**CachyOS Proton** — no extra setup needed; it loads the local `winhttp.dll` automatically.

**All other Proton versions** (Proton 9, Proton Experimental, GE-Proton, etc.) — set the following in Steam → Lunacid → Properties → Launch Options:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

### 4. First launch

Start the game through Steam. BepInEx will create:
- `BepInEx/LogOutput.log` — confirm the plugin loaded here
- `BepInEx/config/crycode4650.lunacid.qolmod.cfg` — edit this to tune each feature

## Configuration

Config file: `BepInEx/config/crycode4650.lunacid.qolmod.cfg`

### `[HUD]`

- **HudAlpha** (float, 0–1, default `0.8`) — in-game HUD opacity. `1.0` = fully opaque, `0.0` = invisible.

### `[Display]`

- **UltrawideFix** (bool, default `true`) — disables `AspectRatioFitter` components on fullscreen overlays and switches canvas scalers to height-match mode so the UI fills the whole screen on 21:9 or wider monitors.

### `[Gameplay]`

- **RespawnOnDeath** (bool, default `true`) — skip the death screen and reload the current area on death.
- **JumpDuringAttack** (bool, default `true`) — allow pressing jump during a weapon attack animation.

### `[Input]`

- **InputOverridePath** (string, default empty) — absolute path to an `InputActionAsset` JSON file. When set, the file replaces the game's default input bindings on startup. Leave empty to use the game's defaults.

  A template with the vanilla bindings is included: [`LunacidQoLMod-InputOverrides.json`](LunacidQoLMod-InputOverrides.json).

## Building from Source

### Prerequisites

- .NET 8 SDK
- Lunacid installed via Steam (app ID 1745510)
- BepInEx 5.4.x installed in the Lunacid folder

### Steps

```sh
git clone https://github.com/starfox8439/lunacid-qol-mod.git
cd lunacid-qol-mod

# Populate the local reference copy of Assembly-CSharp.dll
mkdir -p LUNACID_Data/Managed
cp "$HOME/.local/share/Steam/steamapps/common/Lunacid/LUNACID_Data/Managed/Assembly-CSharp.dll" \
   LUNACID_Data/Managed/

dotnet build -c Release
# Output: bin/Release/netstandard2.1/LunacidQoLMod.dll
```

The Unity engine DLLs and BepInEx core DLLs are resolved automatically from `GamePath` (defaults to the standard Steam install path). Override with `/p:GamePath=/path/to/Lunacid` if your install is elsewhere.

### CI

The GitHub Actions workflow (`build.yml`) runs a NuGet restore on every push (no credentials needed). The full build job downloads game DLLs via DepotDownloader and requires `STEAM_USERNAME` and `STEAM_PASSWORD` repository secrets set to an account that owns Lunacid.

## License

MIT — see [LICENSE](LICENSE).
