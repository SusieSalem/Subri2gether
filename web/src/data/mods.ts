export type ModStatus = 'working' | 'in development' | 'experimental'

export interface Control {
  action: string
  p1: string
  p2: string
}

export interface ConfigOption {
  section: string
  key: string
  default: string
  description: string
}

export interface ModImage {
  /** Path inside web/public (for example /images/mods/znt-2p/cover.png). Shows a placeholder if the file is missing. */
  src?: string
  alt: string
}

export interface Mod {
  id: string
  cover: ModImage
  tagline: string
  gallery: ModImage[]
  title: string
  game: string
  players: { min: number; max: number }
  version: string
  status: ModStatus
  loader: string
  summary: string
  features: { name: string; description: string }[]
  controls: Control[]
  config: ConfigOption[]
  sourcePath: string
}

export const mods: Mod[] = [
  {
    id: 'znt-2p',
    tagline: 'Two minds, one horde.',
    cover: { src: '/images/mods/znt-2p/cover.png', alt: 'ZNT 2 Player cover' },
    gallery: [
      { src: '/images/mods/znt-2p/gallery-1.png', alt: 'Both cursors in a match' },
      { src: '/images/mods/znt-2p/gallery-2.png', alt: 'A fused zombie with its label' },
      { src: '/images/mods/znt-2p/gallery-3.png', alt: 'Fusion almanac' },
    ],
    title: 'ZNT 2 Player',
    game: 'Zombie Night Terror',
    players: { min: 2, max: 2 },
    version: '0.1.0',
    status: 'working',
    loader: 'BepInEx 5.4.x + HarmonyX',
    summary:
      'Turns Zombie Night Terror into 2-player local co-op: P1 on mouse and P2 on a controller, each with their own cursor, sharing the DNA pool.',
    features: [
      {
        name: 'Second player on a controller',
        description:
          'P2 uses a controller read through SDL2 and gets their own red cursor with aim assist, rumble and camera on the right stick.',
      },
      {
        name: 'Automatic camera',
        description: 'The camera zooms out on its own when the two cursors move apart.',
      },
      {
        name: 'Feeding Frenzy',
        description: 'If both players mutate almost at the same time (1.2 s), they earn extra DNA.',
      },
      {
        name: 'Fusion',
        description:
          'If both players mutate the same zombie, it fuses and stacks up to 10 mutations, with around 60 named fusions.',
      },
      {
        name: 'Injection',
        description: 'Turns a zombie into a random fusion for 5 DNA.',
      },
      {
        name: 'Fusion almanac',
        description: 'Keeps track of the fusions you discover.',
      },
    ],
    controls: [
      { action: 'Toggle 2P mode', p1: 'F9', p2: '—' },
      { action: 'Move cursor', p1: 'Mouse', p2: 'Left stick' },
      { action: 'Move camera', p1: 'Same as the game', p2: 'Right stick' },
      { action: 'Inject zombie', p1: 'I', p2: 'Y' },
      { action: 'Fusion almanac', p1: 'F10', p2: 'Minus / Back' },
      { action: 'Direct the Overlord', p1: 'Same as the game', p2: 'D-pad ← → · B cancels' },
    ],
    config: [
      { section: 'General', key: 'EnabledAtStart', default: 'true', description: 'Start with 2P mode on.' },
      { section: 'Player2', key: 'CursorSpeed', default: '900', description: 'P2 cursor speed (px/s).' },
      { section: 'Player2', key: 'Deadzone', default: '0.2', description: 'Stick deadzone.' },
      { section: 'Player2', key: 'CursorColor', default: '#FF2020', description: 'P2 cursor color.' },
      { section: 'Player2', key: 'AimAssistRadius', default: '45', description: 'Aim assist radius in pixels (0 = off).' },
      { section: 'Player2', key: 'RumbleStrength', default: '1', description: 'Rumble strength (0 = off).' },
      { section: 'Camera', key: 'AutoZoomMax', default: '1.35', description: 'Max zoom when the cursors move apart (1 = off).' },
      { section: 'Frenzy', key: 'WindowSeconds', default: '1.2', description: 'Max seconds between P1 and P2 mutations.' },
      { section: 'Frenzy', key: 'BonusDna', default: '1', description: 'Extra DNA per frenzy.' },
      { section: 'Fusion', key: 'WindowSeconds', default: '6', description: 'Max seconds between both players\' hits.' },
      { section: 'Fusion', key: 'MaxStack', default: '10', description: 'Max mutations on a fused zombie.' },
      { section: 'Fusion', key: 'InjectionCostDna', default: '5', description: 'DNA cost of one injection.' },
    ],
    sourcePath: 'mods/znt-2p',
  },
]
