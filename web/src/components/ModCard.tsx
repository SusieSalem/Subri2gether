import { Link } from 'react-router-dom'
import type { Mod } from '../data/mods'
import { playersLabel } from '../data/mods'
import { ImagePlaceholder } from './ImagePlaceholder'

export function ModCard({ mod }: { mod: Mod }) {
  return (
    <Link
      to={`/mods/${mod.id}`}
      className="group block overflow-hidden rounded-3xl border border-white/5 bg-zinc-900/40 transition-colors hover:border-white/15"
    >
      <div className="overflow-hidden">
        <ImagePlaceholder
          image={mod.cover}
          label="Cover · 16:9"
          className="aspect-video w-full transition-transform duration-500 group-hover:scale-[1.03]"
        />
      </div>
      <div className="p-6">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-zinc-500">{mod.game}</p>
        <h3 className="mt-2 text-xl font-bold tracking-tight">{mod.title}</h3>
        <p className="mt-1 text-red-300/90">{mod.tagline}</p>
        <div className="mt-5 flex items-center justify-between text-sm">
          <span className="text-zinc-500">{playersLabel(mod)}</span>
          <span className="font-medium text-zinc-300 transition-colors group-hover:text-white">
            View mod <span className="inline-block transition-transform group-hover:translate-x-0.5">→</span>
          </span>
        </div>
      </div>
    </Link>
  )
}
