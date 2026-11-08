# Subri2gether — Co-Op Mod Hub

**Co-Op Mod Hub** is a website that collects my mods for turning single-player games into **local co-op** experiences for 2 to 4 players, so friends can play together on the same screen.

The site will be hosted on **Firebase Hosting**. For each mod it will show:

- What the mod adds and how the co-op works
- Screenshots and a short gallery
- Controls for each player (keyboard/mouse and gamepad)
- Configuration options
- Installation guide and download link
- Link to the source code in this repository

## Mods

| Mod | Game | Players | Status |
| --- | --- | --- | --- |
| [ZNT 2 Player](mods/znt-2p) | Zombie Night Terror | 2 | v0.1.0, working |

### ZNT 2 Player

Adds local co-op for 2 players to **Zombie Night Terror**. Player 1 uses mouse and keyboard as usual; Player 2 uses a gamepad with their own cursor. Both share the DNA pool and play the same level together.

Co-op features:

- **Second player on gamepad** (via SDL2), with aim assist, rumble and right-stick camera
- **Auto camera zoom** when the two cursors move apart
- **Feeding Frenzy**: both players mutating at almost the same time earns bonus DNA
- **Fusion**: both players mutating the same zombie fuses it, stacking up to 10 mutations (about 60 named fusions)
- **Injection**: turn a zombie into a random fusion for 5 DNA
- **Fusion almanac** that tracks the fusions you discover

Built with C#, BepInEx 5, HarmonyX and SDL2.

## Repository structure

```
web/            The website (React + Vite + TypeScript + Tailwind CSS)
mods/znt-2p/    ZNT 2 Player mod source (BepInEx plugin)
```

## License

My code is released under the [MIT license](LICENSE). This repository does not include any game files, code or assets; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). All games belong to their respective owners.
