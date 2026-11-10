import { Link, useParams } from 'react-router-dom'
import { ImagePlaceholder } from '../components/ImagePlaceholder'
import { btnGhost, btnPrimary } from '../components/ui'
import { mods, playersLabel } from '../data/mods'
import { NotFound } from './NotFound'

function Kbd({ children }: { children: string }) {
  if (children === '—') return <span className="text-mute">—</span>
  // Long labels ("Same as the game") render as plain text, not as a key.
  if (children.length > 12) return <span className="text-mute">{children}</span>
  return (
    <kbd className="whitespace-nowrap border border-ink/20 bg-white/50 px-2 py-0.5 font-mono text-xs text-ink">
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
      <Link to="/mods" className="font-display text-[13px] uppercase tracking-[0.14em] text-mute hover:text-teal-deep">
        ← All mods
      </Link>

      <header className="mt-6 grid items-center gap-10 lg:grid-cols-[1fr_1.15fr]">
        <div>
          <p className="text-[11px] uppercase tracking-[0.22em] text-mute">{mod.game}</p>
          <h1 className="mt-3 font-display text-5xl font-light tracking-wide text-ink sm:text-6xl">
{mod.title}</h1>
          <p className="mt-3 font-display text-xl text-teal-deep">{mod.tagline}</p>
          <p className="mt-5 text-sm leading-relaxed text-mute">{mod.summary}</p>
          <div className="mt-8 flex flex-wrap gap-3">
            <a
              href={`https://github.com/SusieSalem/Subri2gether/tree/main/${mod.sourcePath}`}
              target="_blank"
              rel="noreferrer"
              className={btnPrimary}
            >
              View source
            </a>
            <Link to="/install" className={btnGhost}>
              How to install
            </Link>
          </div>
        </div>
        <ImagePlaceholder image={mod.cover} label="Cover · 16:9" className="aspect-video w-full" />
      </header>

      <dl className="glass hud mt-12 grid grid-cols-2 divide-ink/10 sm:grid-cols-4 sm:divide-x">
        {specs.map(([k, v]) => (
          <div key={k} className="px-5 py-4">
            <dt className="text-[11px] uppercase tracking-[0.22em] text-mute">{k}</dt>
            <dd className="mt-1 font-display text-lg text-ink">{v}</dd>
          </div>
        ))}
      </dl>

      <section className="mt-16">
        <h2 className="font-display text-3xl font-light tracking-wide text-ink">Screenshots</h2>
        <div className="mt-6 grid gap-3 sm:grid-cols-3">
          {mod.gallery.map((img) => (
            <ImagePlaceholder key={img.alt} image={img} className="aspect-video w-full" />
          ))}
        </div>
      </section>

      <section className="mt-16">
        <h2 className="font-display text-3xl font-light tracking-wide text-ink">Features</h2>
        <ul className="mt-6 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {mod.features.map((f) => (
            <li key={f.name} className="glass sheen p-6">
              <p className="font-display text-lg font-medium text-ink">{f.name}</p>
              <p className="mt-2 text-sm leading-relaxed text-mute">{f.description}</p>
            </li>
          ))}
        </ul>
      </section>

      <div className="mt-16 grid gap-10 lg:grid-cols-2">
        <section>
          <h2 className="font-display text-3xl font-light tracking-wide text-ink">Controls</h2>
          <div className="glass mt-6 overflow-hidden">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-ink/10 text-[11px] uppercase tracking-[0.18em] text-mute">
                <tr>
                  <th className="px-4 py-3 font-medium">Action</th>
                  <th className="px-4 py-3 font-medium">P1 · Keyboard</th>
                  <th className="px-4 py-3 font-medium">P2 · Controller</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-ink/10">
                {mod.controls.map((c) => (
                  <tr key={c.action}>
                    <td className="px-4 py-3 text-ink">{c.action}</td>
                    <td className="px-4 py-3"><Kbd>{c.p1}</Kbd></td>
                    <td className="px-4 py-3"><Kbd>{c.p2}</Kbd></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section>
          <h2 className="font-display text-3xl font-light tracking-wide text-ink">Settings</h2>
          <p className="mt-2 text-sm text-mute">
            Found in <code className="text-teal-deep">BepInEx/config/</code> after the first launch.
          </p>
          <dl className="glass mt-4 divide-y divide-ink/10">
            {mod.config.map((o) => (
              <div key={`${o.section}.${o.key}`} className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-0.5 px-4 py-3 text-sm">
                <dt className="font-mono text-xs text-ink">
                  <span className="text-mute">[{o.section}]</span> {o.key}
                </dt>
                <dd className="font-mono text-xs text-rose">{o.default}</dd>
                <dd className="col-span-2 text-xs text-mute">{o.description}</dd>
              </div>
            ))}
          </dl>
        </section>
      </div>
    </article>
  )
}
