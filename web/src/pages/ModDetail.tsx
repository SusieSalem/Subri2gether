import { Link, useParams } from 'react-router-dom'
import { ImagePlaceholder } from '../components/ImagePlaceholder'
import { mods, playersLabel } from '../data/mods'
import { NotFound } from './NotFound'

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

export function ModDetail() {
  const { id } = useParams()
  const mod = mods.find((m) => m.id === id)
  if (!mod) return <NotFound />

  const specs = [
    ['Version', mod.version],
    ['Players', playersLabel(mod)],
    ['Status', mod.status[0].toUpperCase() + mod.status.slice(1)],
    ['Requires', mod.loader],
  ]

  return (
    <article className="pt-10">
      <Link to="/mods" className="text-sm text-zinc-500 hover:text-zinc-300">
        ← All mods
      </Link>

      <header className="mt-6 grid items-center gap-10 lg:grid-cols-[1fr_1.15fr]">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-zinc-500">{mod.game}</p>
          <h1 className="mt-3 text-4xl font-bold tracking-tight sm:text-5xl">{mod.title}</h1>
          <p className="mt-2 text-xl text-red-300/90">{mod.tagline}</p>
          <p className="mt-5 leading-relaxed text-zinc-400">{mod.summary}</p>
          <div className="mt-8 flex flex-wrap gap-3">
            <a
              href={`https://github.com/SusieSalem/Subri2gether/tree/main/${mod.sourcePath}`}
              target="_blank"
              rel="noreferrer"
              className="rounded-full bg-zinc-100 px-5 py-2 text-sm font-semibold text-zinc-900 transition-colors hover:bg-white"
            >
              View source
            </a>
            <Link to="/install" className="rounded-full px-5 py-2 text-sm font-semibold text-zinc-300 ring-1 ring-white/10 transition-colors hover:bg-white/5">
              How to install
            </Link>
          </div>
        </div>
        <ImagePlaceholder image={mod.cover} label="Cover · 16:9" className="aspect-video w-full rounded-3xl ring-1 ring-white/10" />
      </header>

      <dl className="mt-12 grid grid-cols-2 divide-white/5 rounded-2xl border border-white/5 sm:grid-cols-4 sm:divide-x">
        {specs.map(([k, v]) => (
          <div key={k} className="px-5 py-4">
            <dt className="text-xs text-zinc-500">{k}</dt>
            <dd className="mt-1 text-sm font-medium text-zinc-200">{v}</dd>
          </div>
        ))}
      </dl>

      <section className="mt-16">
        <h2 className="text-2xl font-bold tracking-tight">Screenshots</h2>
        <div className="mt-6 grid gap-3 sm:grid-cols-3">
          {mod.gallery.map((img) => (
            <ImagePlaceholder key={img.alt} image={img} className="aspect-video w-full rounded-xl ring-1 ring-white/5" />
          ))}
        </div>
      </section>

      <section className="mt-16">
        <h2 className="text-2xl font-bold tracking-tight">Features</h2>
        <ul className="mt-6 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {mod.features.map((f) => (
            <li key={f.name} className="rounded-2xl border border-white/5 bg-zinc-900/40 p-6">
              <p className="font-semibold text-zinc-100">{f.name}</p>
              <p className="mt-2 text-sm leading-relaxed text-zinc-400">{f.description}</p>
            </li>
          ))}
        </ul>
      </section>

      <div className="mt-16 grid gap-10 lg:grid-cols-2">
        <section>
          <h2 className="text-2xl font-bold tracking-tight">Controls</h2>
          <div className="mt-6 overflow-hidden rounded-2xl ring-1 ring-white/5">
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
        </section>

        <section>
          <h2 className="text-2xl font-bold tracking-tight">Settings</h2>
          <p className="mt-2 text-sm text-zinc-500">
            Found in <code className="text-zinc-300">BepInEx/config/</code> after the first launch.
          </p>
          <dl className="mt-4 divide-y divide-white/5 rounded-2xl ring-1 ring-white/5">
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
        </section>
      </div>
    </article>
  )
}
