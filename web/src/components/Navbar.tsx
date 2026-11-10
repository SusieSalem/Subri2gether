import { useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { Logo, Wordmark } from './Logo'

const links = [
  { to: '/mods', label: 'Mods' },
  { to: '/install', label: 'Install' },
  { to: '/about', label: 'About' },
]

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `px-3 py-1.5 font-display text-[13px] uppercase tracking-[0.14em] transition-colors ${isActive ? 'text-teal-deep' : 'text-mute hover:text-ink'}`

function GitHubIcon() {
  return (
    <svg viewBox="0 0 16 16" width="16" height="16" className="size-4 shrink-0" fill="currentColor" aria-hidden>
      <path d="M8 0C3.58 0 0 3.58 0 8a8 8 0 0 0 5.47 7.59c.4.07.55-.17.55-.38v-1.33c-2.23.48-2.7-1.07-2.7-1.07-.36-.92-.89-1.17-.89-1.17-.73-.5.05-.49.05-.49.8.06 1.23.83 1.23.83.72 1.23 1.88.87 2.34.67.07-.52.28-.87.5-1.07-1.78-.2-3.65-.89-3.65-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82a7.6 7.6 0 0 1 4 0c1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48v2.2c0 .21.15.46.55.38A8 8 0 0 0 16 8c0-4.42-3.58-8-8-8Z" />
    </svg>
  )
}

export function Navbar() {
  const [open, setOpen] = useState(false)

  return (
    <header className="sticky top-0 z-50 px-4 pt-4">
      <nav className="mx-auto max-w-6xl glass-nav px-3 pl-4">
        <div className="flex h-14 items-center justify-between gap-6">
          <Link to="/" className="flex shrink-0 items-center gap-2.5" onClick={() => setOpen(false)}>
            <Logo />
            <Wordmark />
          </Link>

          <ul className="hidden items-center gap-1 text-sm sm:flex">
            {links.map((l) => (
              <li key={l.to}>
                <NavLink to={l.to} className={linkClass}>
                  {l.label}
                </NavLink>
              </li>
            ))}
          </ul>

          <div className="flex items-center gap-2">
            <a
              href="https://github.com/SusieSalem/Subri2gether"
              target="_blank"
              rel="noreferrer"
              className="flex shrink-0 items-center gap-2 bg-ink px-3.5 py-1.5 font-display text-[13px] uppercase tracking-[0.14em] text-white transition-colors hover:bg-teal-deep"
            >
              <GitHubIcon />
              <span className="hidden sm:inline">GitHub</span>
            </a>
            <button
              type="button"
              aria-label="Menu"
              aria-expanded={open}
              onClick={() => setOpen((o) => !o)}
              className="grid size-9 place-items-center text-ink hover:text-teal-deep sm:hidden"
            >
              <svg viewBox="0 0 20 20" className="size-5" fill="none" stroke="currentColor" strokeWidth={1.75}>
                {open ? <path d="m5 5 10 10M15 5 5 15" /> : <path d="M3 6h14M3 10h14M3 14h14" />}
              </svg>
            </button>
          </div>
        </div>

        {open && (
          <ul className="flex flex-col gap-1 border-t border-ink/10 py-3 text-sm sm:hidden">
            {links.map((l) => (
              <li key={l.to}>
                <NavLink to={l.to} className={(s) => `block ${linkClass(s)}`} onClick={() => setOpen(false)}>
                  {l.label}
                </NavLink>
              </li>
            ))}
          </ul>
        )}
      </nav>
    </header>
  )
}
