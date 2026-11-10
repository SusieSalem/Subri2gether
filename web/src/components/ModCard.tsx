import { Link } from 'react-router-dom'
import type { Mod } from '../data/mods'
import { playersLabel } from '../data/mods'
import { ImagePlaceholder } from './ImagePlaceholder'

export function ModCard({ mod, index = 1 }: { mod: Mod; index?: number }) {
  return (
    <Link to={`/mods/${mod.id}`} className="glass sheen group block transition-transform hover:-translate-y-1">
      <ImagePlaceholder image={mod.cover} label="Cover 16:9" className="aspect-video w-full" />
      <div className="relative p-6">
        <span className="absolute right-6 top-5 font-display text-4xl font-light text-ink/15">{String(index).padStart(2, '0')}</span>
        <p className="text-[11px] uppercase tracking-[0.22em] text-mute">{mod.game}</p>
        <h3 className="mt-2 font-display text-2xl font-medium text-ink">{mod.title}</h3>
        <p className="mt-1 text-sm text-teal-deep">{mod.tagline}</p>
        <div className="mt-6 flex items-center justify-between border-t border-ink/10 pt-4 text-xs">
          <span className="text-mute">{playersLabel(mod)}</span>
          <span className="font-display uppercase tracking-[0.14em] text-ink group-hover:text-rose">Open →</span>
        </div>
      </div>
    </Link>
  )
}
