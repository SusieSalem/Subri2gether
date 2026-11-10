import { Link } from 'react-router-dom'
import { Wordmark } from './Logo'

const year = new Date().getFullYear()

export function Footer() {
  return (
    <footer className="mt-24">
      <div className="mx-auto max-w-6xl px-6 py-10">
        <div className="flex flex-wrap items-end justify-between gap-6 border-t border-ink/15 pt-8">
          <div>
            <Wordmark />
            <p className="mt-2 text-[11px] uppercase tracking-[0.22em] text-mute">A plan to let two people share one game.</p>
          </div>
          <nav className="flex gap-6 font-display text-[13px] uppercase tracking-[0.14em] text-mute">
            <Link to="/mods" className="hover:text-teal-deep">Mods</Link>
            <Link to="/install" className="hover:text-teal-deep">Install</Link>
            <Link to="/about" className="hover:text-teal-deep">About</Link>
          </nav>
        </div>
        <div className="mt-6 flex flex-wrap justify-between gap-4 text-[11px] text-mute">
          <p>© {year} Subri2gether · MIT</p>
          <p>All games belong to their respective owners.</p>
        </div>
      </div>
    </footer>
  )
}
