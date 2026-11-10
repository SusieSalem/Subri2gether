import { Link } from 'react-router-dom'
import type { Mod } from '../data/mods'
import { ImagePlaceholder } from './ImagePlaceholder'

export function ModCard({ mod }: { mod: Mod }) {
  return (
    <Link to={`/mods/${mod.id}`} className="glass sheen group block transition-transform hover:-translate-y-1">
      <ImagePlaceholder image={mod.cover} label="Cover" className="aspect-video w-full" />
      <div className="p-6">
        <p className="text-[11px] uppercase tracking-[0.22em] text-mute">{mod.game}</p>
        <h3 className="mt-2 font-display text-2xl font-medium text-ink group-hover:text-teal-deep">{mod.title}</h3>
      </div>
    </Link>
  )
}
