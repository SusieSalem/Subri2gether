import { ImagePlaceholder } from './components/ImagePlaceholder'
import { ModSection } from './components/ModSection'
import { Navbar } from './components/Navbar'
import { mods } from './data/mods'

const year = new Date().getFullYear()

const installSteps = [
  { title: 'Install the loader', text: 'Download BepInEx 5.4.x and extract it into the game folder. Launch the game once.' },
  { title: 'Copy the mod', text: 'Put the mod files in BepInEx/plugins/, inside their own folder.' },
  { title: 'Plug in the controllers', text: 'Connect the second player\'s controller before launching the game, and play.' },
]

function App() {
  const maxPlayers = Math.max(...mods.map((m) => m.players.max))

  return (
    <div className="min-h-screen">
      <div className="pointer-events-none fixed inset-x-0 top-0 -z-10 h-[600px] bg-[radial-gradient(ellipse_at_top,rgba(239,68,68,0.12),transparent_60%)]" />
      <Navbar />

      <main className="mx-auto max-w-6xl px-6">
        <section className="grid items-center gap-12 py-20 lg:grid-cols-2 lg:py-24">
          <div>
            <h1 className="text-5xl font-bold leading-[1.05] tracking-tight sm:text-6xl">
              Single-player games,
              <br />
              <span className="bg-gradient-to-r from-red-400 to-orange-300 bg-clip-text text-transparent">made to play together.</span>
            </h1>
            <p className="mt-6 max-w-md text-lg leading-relaxed text-zinc-400">
              Mods that turn single-player games into local co-op for 2 to 4 people, on the same couch.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <a href="#mods" className="rounded-full bg-red-500 px-6 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-red-400">
                Browse mods
              </a>
              <a href="#install" className="rounded-full px-6 py-2.5 text-sm font-semibold text-zinc-300 ring-1 ring-white/10 transition-colors hover:bg-white/5">
                How to install
              </a>
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
            image={{ alt: 'Hero image' }}
            label="Hero image · 4:3"
            className="aspect-[4/3] w-full rounded-3xl ring-1 ring-white/10"
          />
        </section>

        <section id="mods" className="scroll-mt-24 py-12">
          <div className="mb-10">
            <h2 className="text-3xl font-bold tracking-tight">Mods</h2>
            <p className="mt-2 text-zinc-400">Each mod comes with its source code, controls and settings.</p>
          </div>
          <div className="space-y-10">
            {mods.map((mod) => (
              <ModSection key={mod.id} mod={mod} />
            ))}
          </div>
        </section>

        <section id="install" className="scroll-mt-24 py-20">
          <h2 className="text-3xl font-bold tracking-tight">How to install</h2>
          <p className="mt-2 text-zinc-400">Three steps and you're done.</p>
          <ol className="mt-10 grid gap-4 md:grid-cols-3">
            {installSteps.map((s, i) => (
              <li key={s.title} className="rounded-2xl border border-white/5 bg-zinc-900/40 p-6">
                <span className="font-mono text-sm text-red-400">0{i + 1}</span>
                <h3 className="mt-3 font-semibold">{s.title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-zinc-400">{s.text}</p>
              </li>
            ))}
          </ol>
        </section>

        <section id="about" className="scroll-mt-24 border-t border-white/5 py-20">
          <div className="max-w-2xl">
            <h2 className="text-3xl font-bold tracking-tight">About</h2>
            <p className="mt-4 leading-relaxed text-zinc-400">
              Co-Op Mod Hub collects the mods I make to play single-player games in local co-op. All the code is open source under
              the MIT license. No game files are included or redistributed.
            </p>
          </div>
        </section>
      </main>

      <footer className="border-t border-white/5">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-8 text-sm text-zinc-500">
          <p>© {year} Co-Op Mod Hub · MIT</p>
          <p>All games belong to their respective owners.</p>
        </div>
      </footer>
    </div>
  )
}

export default App
