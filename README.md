# Subri2gether — Co-Op Mod Hub

A web catalog of mods that turn single-player games into local co-op experiences for 2 to 4 players. Every mod in the catalog is my own.

## Structure

```
web/            Catalog website (React + Vite + TypeScript + Tailwind CSS)
mods/znt-2p/    ZNT 2 Player: co-op mod for Zombie Night Terror (BepInEx)
```

## Website

```bash
cd web
npm install
npm run dev      # development server
npm run build    # production build in web/dist
```

Each mod's data lives in `web/src/data/mods.ts`.

## Mods

| Mod | Game | Players | Status |
| --- | --- | --- | --- |
| [ZNT 2 Player](mods/znt-2p) | Zombie Night Terror | 2 | v0.1.0, working |

## License

Code under the [MIT](LICENSE) license. This repository does not include any game files, code or assets; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
