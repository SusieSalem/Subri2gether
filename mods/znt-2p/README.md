# ZNT 2 Player

A mod for **Zombie Night Terror** that adds 2-player local co-op. P1 plays with keyboard and mouse, as usual, and P2 with a controller.

- **Version:** 0.1.0 (working)
- **GUID:** `com.local.znt2p`
- **Requirements:** BepInEx 5.4.x and `SDL2.dll`

## What it does

- **Second player on a controller:** P2 gets their own red cursor (read through SDL2), with aim assist, rumble and camera on the right stick. Both players share the DNA pool and each one picks their own mutation.
- **Automatic camera:** zooms out on its own when the cursors move apart.
- **Feeding Frenzy:** if both players mutate less than 1.2 s apart, they earn +1 DNA.
- **Fusion:** if both players mutate the same zombie within 6 s, it fuses and stacks up to 10 mutations. There are around 60 named fusions with special traits.
- **Injection:** turns a zombie into a random fusion for 5 DNA.
- **Fusion almanac:** keeps track of the fusions you discover.

## Controls

| Action | P1 | P2 (controller) |
| --- | --- | --- |
| Toggle 2P | F9 | — |
| Inject zombie | I | Y |
| Almanac | F10 | Minus / Back |
| Move camera | Same as the game | Right stick |
| Direct the Overlord | Same as the game | D-pad ◀ ▶ (B cancels) |

## Installation

1. Install [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) in the game folder and launch the game once.
2. Copy `Znt2P.dll` and `SDL2.dll` into `BepInEx/plugins/Znt2P/`.
3. Plug in the controller and launch the game. The config file is created at `BepInEx/config/com.local.znt2p.cfg`.

## Building

You need the .NET SDK and the game installed with BepInEx, because the project references its DLLs. By default it looks for the game in the Steam path (`C:\Program Files (x86)\Steam\steamapps\common\Zombie Night Terror`). If yours is somewhere else, pass the path:

```bash
dotnet build -c Release
dotnet build -c Release -p:GameDir="D:\Games\Zombie Night Terror"
```

The game's code is not included in this repository.
