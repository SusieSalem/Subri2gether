import type { ReactNode } from 'react'
import type { Mod } from '../data/mods'
import { ImagePlaceholder } from './ImagePlaceholder'

function Kbd({ children }: { children: string }) {
  if (children === '—') return <span className="text-zinc-600">—</span>
  // Long labels ("Same as the game") render as plain text, not as a key.
  if (children.length > 12) return <span className="text-zinc-400">{children}</span>
  return (
    <kbd className="whitespace-nowrap rounded-md border border-white/10 bg-white/5 px-2 py-0.5 font-sans text-xs text-zinc-200 shadow-[inset_0_-1px_0_rgba(255,255,255,0.08)]">
      {children}
    </kbd>
  )
}

function Pill({ children, accent = false }: { children: ReactNode; accent?: boolean }) {
  return (
    <span
      className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${
        accent ? 'bg-emerald-500/10 text-emerald-300 ring-emerald-500/25' : 'bg-white/5 text-zinc-300 ring-white/10'
      }`}
    >
      {children}
    </span>
  )
}

export function ModSection({ mod }: { mod: Mod }) {
  const players = mod.players.min === mod.players.max ? `${mod.players.min} players` : `${mod.players.min}–${mod.players.max} players`

  return (
    <article id={mod.id} className="scroll-mt-24 overflow-hidden rounded-3xl border border-white/5 bg-zinc-900/40">
      <div className="grid gap-0 lg:grid-cols-[1.15fr_1fr]">
        <ImagePlaceholder image={mod.cover} label="Cover · 16:9" className="aspect-video h-full w-full lg:aspect-auto lg:min-h-80" />

        <div className="flex flex-col justify-center gap-5 p-8 lg:p-10">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-zinc-500">{mod.game}</p>
          <div>
            <h3 className="text-3xl font-bold tracking-tight">{mod.title}</h3>
            <p className="mt-1 text-lg text-red-300/90">{mod.tagline}</p>
          </div>
          <p className="leading-relaxed text-zinc-400">{mod.summary}</p>
          <div className="flex flex-wrap gap-2">
            <Pill accent>v{mod.version} · {mod.status}</Pill>
            <Pill>{players}</Pill>
            <Pill>{mod.loader}</Pill>
          </div>
          <div className="flex flex-wrap gap-3 pt-2">
            <a
              href={`https://github.com/SusieSalem/Subri2gether/tree/main/${mod.sourcePath}`}
              target="_blank"
              rel="noreferrer"
              className="rounded-full bg-zinc-100 px-5 py-2 text-sm font-semibold text-zinc-900 transition-colors hover:bg-white"
            >
              View source
            </a>
            <a href="#install" className="rounded-full px-5 py-2 text-sm font-semibold text-zinc-300 ring-1 ring-white/10 transition-colors hover:bg-white/5">
              How to install
            </a>
          </div>
        </div>
      </div>

      <div className="grid gap-3 border-t border-white/5 p-6 sm:grid-cols-3 lg:p-8">
        {mod.gallery.map((img) => (
          <ImagePlaceholder key={img.alt} image={img} className="aspect-video w-full rounded-xl ring-1 ring-white/5" />
        ))}
      </div>

      <div className="grid gap-10 border-t border-white/5 p-6 lg:grid-cols-2 lg:p-10">
        <section>
          <h4 className="text-sm font-semibold uppercase tracking-[0.15em] text-zinc-500">Features</h4>
          <ul className="mt-5 space-y-5">
            {mod.features.map((f, i) => (
              <li key={f.name} className="flex gap-4">
                <span className="mt-0.5 grid size-7 shrink-0 place-items-center rounded-full bg-red-500/10 text-xs font-semibold text-red-300 ring-1 ring-red-500/20">
                  {i + 1}
                </span>
                <div>
                  <p className="font-medium text-zinc-100">{f.name}</p>
                  <p className="mt-0.5 text-sm leading-relaxed text-zinc-400">{f.description}</p>
                </div>
              </li>
            ))}
          </ul>
        </section>

        <section>
          <h4 className="text-sm font-semibold uppercase tracking-[0.15em] text-zinc-500">Controls</h4>
          <div className="mt-5 overflow-hidden rounded-2xl ring-1 ring-white/5">
            <table className="w-full text-left text-sm">
              <thead className="bg-white/[0.03] text-xs text-zinc-500">
                <tr>
                  <th className="px-4 py-3 font-medium">Action</th>
                  <th className="px-4 py-3 font-medium">P1 · Keyboard</th>
                  <th className="px-4 py-3 font-medium">P2 · Controller</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5">
                {mod.controls.map((c) => (
                  <tr key={c.action}>
                    <td className="px-4 py-3 text-zinc-300">{c.action}</td>
                    <td className="px-4 py-3"><Kbd>{c.p1}</Kbd></td>
                    <td className="px-4 py-3"><Kbd>{c.p2}</Kbd></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <details className="group mt-6 rounded-2xl ring-1 ring-white/5">
            <summary className="flex cursor-pointer list-none items-center justify-between px-4 py-3 text-sm font-medium text-zinc-300">
              BepInEx settings
              <span className="text-zinc-500 transition-transform group-open:rotate-45">+</span>
            </summary>
            <dl className="divide-y divide-white/5 border-t border-white/5">
              {mod.config.map((o) => (
                <div key={`${o.section}.${o.key}`} className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-0.5 px-4 py-3 text-sm">
                  <dt className="font-mono text-xs text-zinc-300">
                    <span className="text-zinc-600">[{o.section}]</span> {o.key}
                  </dt>
                  <dd className="font-mono text-xs text-red-300/80">{o.default}</dd>
                  <dd className="col-span-2 text-xs text-zinc-500">{o.description}</dd>
                </div>
              ))}
            </dl>
          </details>
        </section>
      </div>
    </article>
  )
}
