import { Link } from 'react-router-dom'
import { ImagePlaceholder } from '../components/ImagePlaceholder'
import { ModCard } from '../components/ModCard'
import { Steps } from '../components/Steps'
import { mods } from '../data/mods'

export function Home() {
  const maxPlayers = Math.max(...mods.map((m) => m.players.max))

  return (
    <>
      <section className="grid items-center gap-12 py-20 lg:grid-cols-2 lg:py-24">
        <div>
          <h1 className="text-5xl font-bold leading-[1.05] tracking-tight sm:text-6xl">
            Single-player games,
            <br />
            <span className="bg-gradient-to-r from-red-400 to-orange-300 bg-clip-text text-transparent">made to play together.</span>
          </h1>
          <p className="mt-6 max-w-md text-lg leading-relaxed text-zinc-400">
            Subri2gether is a collection of mods that turn single-player games into local co-op for 2 to 4 people, on the same couch.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link to="/mods" className="rounded-full bg-red-500 px-6 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-red-400">
              Browse mods
            </Link>
            <Link to="/install" className="rounded-full px-6 py-2.5 text-sm font-semibold text-zinc-300 ring-1 ring-white/10 transition-colors hover:bg-white/5">
              How to install
            </Link>
          </div>
          <dl className="mt-12 flex gap-10">
            {[
              [String(mods.length), mods.length === 1 ? 'mod' : 'mods'],
              [`up to ${maxPlayers}`, 'players'],
              ['100%', 'local'],
            ].map(([v, l]) => (
              <div key={l}>
                <dt className="text-2xl font-semibold text-zinc-100">{v}</dt>
                <dd className="text-sm text-zinc-500">{l}</dd>
              </div>
            ))}
          </dl>
        </div>

        <ImagePlaceholder
          image={{ src: '/images/site/hero.png', alt: 'Hero image' }}
          label="Hero image · 4:3"
          className="aspect-[4/3] w-full rounded-3xl ring-1 ring-white/10"
        />
      </section>

      <section className="py-12">
        <div className="mb-8 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-3xl font-bold tracking-tight">Latest mods</h2>
            <p className="mt-2 text-zinc-400">Pick a game and grab a second controller.</p>
          </div>
          <Link to="/mods" className="shrink-0 text-sm font-medium text-zinc-300 hover:text-white">
            All mods →
          </Link>
        </div>
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {mods.slice(0, 3).map((mod) => (
            <ModCard key={mod.id} mod={mod} />
          ))}
        </div>
      </section>

      <section className="py-12">
        <div className="mb-8 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-3xl font-bold tracking-tight">Up and running in minutes</h2>
            <p className="mt-2 text-zinc-400">Three steps and you're done.</p>
          </div>
          <Link to="/install" className="shrink-0 text-sm font-medium text-zinc-300 hover:text-white">
            Full guide →
          </Link>
        </div>
        <Steps />
      </section>
    </>
  )
}
