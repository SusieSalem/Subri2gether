import { PageHeader } from '../components/PageHeader'
import { Steps } from '../components/Steps'

export function Install() {
  return (
    <>
      <PageHeader eyebrow="Guide" title="How to install">
        All mods on Subri2gether run on BepInEx. Set it up once per game and you're ready to play.
      </PageHeader>
      <Steps />
      <section className="mt-12 grid gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-white/5 bg-zinc-900/40 p-6">
          <h2 className="font-semibold">What you need</h2>
          <ul className="mt-4 space-y-2 text-sm leading-relaxed text-zinc-400">
            <li>A legal copy of the game, on Windows.</li>
            <li>
              <a href="https://github.com/BepInEx/BepInEx/releases" target="_blank" rel="noreferrer" className="text-zinc-200 underline decoration-white/20 underline-offset-4 hover:decoration-white/60">
                BepInEx 5.4.x
              </a>{' '}
              (the x64 build for 64-bit games).
            </li>
            <li>One controller for each extra player.</li>
          </ul>
        </div>
        <div className="rounded-2xl border border-white/5 bg-zinc-900/40 p-6">
          <h2 className="font-semibold">Settings</h2>
          <p className="mt-4 text-sm leading-relaxed text-zinc-400">
            After the first launch with the mod, BepInEx creates a config file in <code className="text-zinc-200">BepInEx/config/</code>.
            Each mod page lists every option and its default value.
          </p>
        </div>
      </section>
    </>
  )
}
