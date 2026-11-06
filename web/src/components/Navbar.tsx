const links = [
  { href: '#mods', label: 'Mods' },
  { href: '#install', label: 'Install' },
  { href: '#about', label: 'About' },
]

export function Navbar() {
  return (
    <header className="sticky top-0 z-50 px-4 pt-4">
      <nav className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-6 rounded-2xl border border-white/10 bg-white/[0.06] px-3 pl-4 shadow-[0_8px_32px_rgba(0,0,0,0.35)] backdrop-blur-xl backdrop-saturate-150">
        <a href="#" className="flex shrink-0 items-center gap-2.5 font-semibold tracking-tight">
          <span className="grid size-8 place-items-center rounded-lg bg-red-500/20 text-xs font-bold text-red-300 ring-1 ring-red-400/30">
            2P
          </span>
          <span>Co-Op Mod Hub</span>
        </a>

        <ul className="hidden items-center gap-1 text-sm text-zinc-300 sm:flex">
          {links.map((l) => (
            <li key={l.href}>
              <a href={l.href} className="rounded-lg px-3 py-1.5 transition-colors hover:bg-white/10 hover:text-white">
                {l.label}
              </a>
            </li>
          ))}
        </ul>

        <a
          href="https://github.com/SusieSalem/Subri2gether"
          target="_blank"
          rel="noreferrer"
          className="flex shrink-0 items-center gap-2 rounded-xl border border-white/10 bg-white/10 px-3.5 py-1.5 text-sm font-medium text-zinc-100 transition-colors hover:bg-white/15"
        >
          <svg viewBox="0 0 16 16" width="16" height="16" className="size-4 shrink-0" fill="currentColor" aria-hidden>
            <path d="M8 0C3.58 0 0 3.58 0 8a8 8 0 0 0 5.47 7.59c.4.07.55-.17.55-.38v-1.33c-2.23.48-2.7-1.07-2.7-1.07-.36-.92-.89-1.17-.89-1.17-.73-.5.05-.49.05-.49.8.06 1.23.83 1.23.83.72 1.23 1.88.87 2.34.67.07-.52.28-.87.5-1.07-1.78-.2-3.65-.89-3.65-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82a7.6 7.6 0 0 1 4 0c1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48v2.2c0 .21.15.46.55.38A8 8 0 0 0 16 8c0-4.42-3.58-8-8-8Z" />
          </svg>
          GitHub
        </a>
      </nav>
    </header>
  )
}
