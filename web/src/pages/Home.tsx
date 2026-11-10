import { Link } from 'react-router-dom'
import { Bars, Emblem, PixelSlab } from '../components/Art'
import { ModCard } from '../components/ModCard'
import { Steps } from '../components/Steps'
import { btnGhost, btnPrimary, linkMore, sectionTitle } from '../components/ui'
import { mods } from '../data/mods'

export function Home() {
  const maxPlayers = Math.max(...mods.map((m) => m.players.max))

  return (
    <>
      <section className="grid items-center gap-14 py-16 lg:grid-cols-[1.05fr_1fr] lg:py-24">
        <div>
          <Bars />
          <p className="mt-6 text-[11px] uppercase tracking-[0.22em] text-mute">Local co-op mods · Unit 01</p>
          <h1 className="mt-4 font-display text-5xl font-light leading-[1.05] tracking-wide text-ink sm:text-6xl">
            Single-player games,
            <br />
            made to play <span className="font-medium text-teal-deep">together.</span>
          </h1>
          <p className="mt-6 max-w-md text-sm leading-relaxed text-mute">
            Subri2gether is a collection of mods that turn single-player games into local co-op for 2 to 4 people, on the same couch.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link to="/mods" className={btnPrimary}>
              Browse mods
            </Link>
            <Link to="/install" className={btnGhost}>
              How to install
            </Link>
          </div>
          <dl className="mt-12 flex gap-10">
            {[
              [String(mods.length).padStart(2, '0'), mods.length === 1 ? 'Mod' : 'Mods'],
              [`0${maxPlayers}`, 'Players max'],
              ['100', 'Percent local'],
            ].map(([v, l]) => (
              <div key={l}>
                <dt className="font-display text-4xl font-light text-ink">{v}</dt>
                <dd className="mt-1 text-[11px] uppercase tracking-[0.22em] text-mute">{l}</dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="relative mx-auto w-full max-w-md">
          <div className="glass hud float relative z-10 ml-auto w-[78%] p-8">
            <div className="flex items-start justify-between">
              <span className="font-display text-8xl font-light leading-none text-ink">01</span>
              <span className="mt-2 size-2 rounded-full bg-rose" />
            </div>
            <dl className="mt-8 space-y-1.5 text-[12px] text-ink/80">
              <div className="flex gap-2"><dt className="text-mute">Unit:</dt><dd>ZNT-2P</dd></div>
              <div className="flex gap-2"><dt className="text-mute">Date of manufacture:</dt><dd>1010</dd></div>
              <div className="flex gap-2"><dt className="text-mute">Players:</dt><dd>02 · linked</dd></div>
              <div className="flex gap-2"><dt className="text-mute">Status:</dt><dd className="text-teal-deep">working</dd></div>
            </dl>
          </div>
          <div className="absolute -bottom-12 left-0 h-[120%] w-[28%]">
            <PixelSlab className="size-full" cols={9} rows={34} />
          </div>
          <div className="absolute -top-24 left-[30%] hidden items-center gap-3 sm:flex">
            <Emblem className="size-20 text-ink" />
            <p className="max-w-[9rem] text-[10px] leading-snug text-mute">A plan to let two minds share one game.</p>
          </div>
        </div>
      </section>

      <section className="py-12">
        <div className="mb-8 flex items-end justify-between gap-4">
          <div>
            <p className="text-[11px] uppercase tracking-[0.22em] text-mute">Catalog</p>
            <h2 className={`mt-2 ${sectionTitle}`}>Latest mods</h2>
          </div>
          <Link to="/mods" className={linkMore}>
            All mods →
          </Link>
        </div>
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {mods.slice(0, 3).map((mod, i) => (
            <ModCard key={mod.id} mod={mod} index={i + 1} />
          ))}
        </div>
      </section>

      <section className="py-12">
        <div className="mb-8 flex items-end justify-between gap-4">
          <div>
            <p className="text-[11px] uppercase tracking-[0.22em] text-mute">Setup</p>
            <h2 className={`mt-2 ${sectionTitle}`}>Up and running</h2>
          </div>
          <Link to="/install" className={linkMore}>
            Full guide →
          </Link>
        </div>
        <Steps />
      </section>
    </>
  )
}
