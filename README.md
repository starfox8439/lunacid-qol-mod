# Lunacid QoL Mod

A BepInEx 5 + Harmony plugin for [Lunacid](https://store.steampowered.com/app/1946440/LUNACID/) that adds quality-of-life improvements without modifying the game's DLLs.

## Features

| Feature | Config key | Default |
|---|---|---|
| Respawn on death instead of game-over screen | `Gameplay.RespawnOnDeath` | `true` |
| Jump during weapon attacks | `Gameplay.JumpDuringAttack` | `true` |
| HUD transparency | `HUD.HudAlpha` | `0.8` |
| Ultrawide (21:9+) fixes for overlays, popups, and canvas scaling | `Display.UltrawideFix` | `true` |
| Custom input bindings via InputActionAsset JSON | `Input.InputOverridePath` | *(empty)* |

## Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) into your Lunacid folder.
2. Drop `LunacidQoLMod.dll` into `BepInEx/plugins/`.
3. Launch the game once to generate `BepInEx/config/crycode4650.lunacid.qolmod.cfg`.
4. Edit the config file to enable or tune each feature.

## Configuration

Config file: `BepInEx/config/crycode4650.lunacid.qolmod.cfg`

### `[HUD]`

- **HudAlpha** (float, 0–1, default `0.8`) — overall HUD opacity. `1.0` = fully opaque, `0.0` = invisible.

### `[Display]`

- **UltrawideFix** (bool, default `true`) — disables `AspectRatioFitter` components on fullscreen overlays and switches canvas scalers to `Expand` mode so the UI fills the whole screen on 21:9 or wider monitors.

### `[Gameplay]`

- **RespawnOnDeath** (bool, default `true`) — skip the death screen and reload the current area immediately on death.
- **JumpDuringAttack** (bool, default `true`) — allow pressing jump while a weapon attack animation is playing.

### `[Input]`

- **InputOverridePath** (string, default empty) — absolute path to an `InputActionAsset` JSON file. When set, the bindings in that file are loaded over the game's defaults on startup. Leave empty to use the game's built-in bindings.

  Example JSON layout is the same format as Unity's `InputActionAsset` (exported via the Input System package). A template is provided in the repository: [`LunacidQoLMod-InputOverrides.json`](LunacidQoLMod-InputOverrides.json).

## Building from Source

### Prerequisites

- .NET 8 SDK
- Lunacid installed via Steam
- BepInEx 5.4.x installed in the Lunacid folder

### Steps

```sh
git clone https://github.com/crycode4650/lunacid-qol-mod.git
cd lunacid-qol-mod

# Populate the local reference copy of Assembly-CSharp.dll
mkdir -p LUNACID_Data/Managed
cp "$STEAM_ROOT/steamapps/common/Lunacid/LUNACID_Data/Managed/Assembly-CSharp.dll" \
   LUNACID_Data/Managed/

dotnet build -c Release
# Output: bin/Release/netstandard2.1/LunacidQoLMod.dll
```

The Unity engine DLLs are resolved automatically from `GamePath`
(defaults to `~/.local/share/Steam/steamapps/common/Lunacid`).
Override with `/p:GamePath=/path/to/Lunacid` if your install is elsewhere.

## License

MIT — see [LICENSE](LICENSE).
