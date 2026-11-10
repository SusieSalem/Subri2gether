import { Link } from 'react-router-dom'
import { Wordmark } from './Logo'

const year = new Date().getFullYear()

export function Footer() {
  return (
    <footer className="mt-24 border-t border-white/5">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-8 text-sm text-zinc-500">
        <p>
          © {year} <Wordmark /> · MIT
        </p>
        <nav className="flex gap-5">
          <Link to="/mods" className="hover:text-zinc-300">Mods</Link>
          <Link to="/install" className="hover:text-zinc-300">Install</Link>
          <Link to="/about" className="hover:text-zinc-300">About</Link>
        </nav>
        <p>All games belong to their respective owners.</p>
      </div>
    </footer>
  )
}
