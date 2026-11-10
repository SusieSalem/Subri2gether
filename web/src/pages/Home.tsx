import { Link } from 'react-router-dom'
import { PixelSlab } from '../components/Art'
import { ModCard } from '../components/ModCard'
import { btnGhost, btnPrimary, sectionTitle } from '../components/ui'
import { mods } from '../data/mods'

export function Home() {
  return (
    <>
      <section className="grid items-center gap-14 py-20 lg:grid-cols-[1.05fr_1fr] lg:py-28">
        <div>
          <h1 className="font-display text-5xl font-light leading-[1.05] tracking-wide text-ink sm:text-6xl">
            Single-player games,
            <br />
            made to play <span className="font-medium text-teal-deep">together.</span>
          </h1>
          <p className="mt-6 max-w-sm text-sm leading-relaxed text-mute">Mods that add local co-op to games built for one.</p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link to="/mods" className={btnPrimary}>
              Browse mods
            </Link>
            <Link to="/install" className={btnGhost}>
              How to install
            </Link>
          </div>
        </div>

        <div className="relative mx-auto w-full max-w-md">
          <div className="glass hud float relative z-10 ml-auto w-[78%] p-8">
            <div className="flex items-start justify-between">
              <span className="font-display text-8xl font-light leading-none text-ink">01</span>
              <span className="mt-2 size-2 rounded-full bg-rose" />
            </div>
            <p className="mt-8 text-[12px] text-mute">
              ZNT-2P · <span className="text-teal-deep">working</span>
            </p>
          </div>
          <div className="absolute -bottom-12 left-0 h-[130%] w-[28%]">
            <PixelSlab className="size-full" cols={9} rows={34} />
          </div>
        </div>
      </section>

      <section className="py-16">
        <h2 className={`mb-8 ${sectionTitle}`}>Mods</h2>
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {mods.map((mod) => (
            <ModCard key={mod.id} mod={mod} />
          ))}
        </div>
      </section>
    </>
  )
}
