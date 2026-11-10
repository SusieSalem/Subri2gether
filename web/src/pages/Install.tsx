import { PageHeader } from '../components/PageHeader'
import { Steps } from '../components/Steps'

export function Install() {
  return (
    <>
      <PageHeader eyebrow="Guide" title="Install">
        All mods on Subri2gether run on BepInEx. Set it up once per game and you're ready to play.
      </PageHeader>
      <Steps />
      <section className="mt-10 grid gap-6 lg:grid-cols-2">
        <div className="glass p-6">
          <h2 className="font-display text-lg font-medium">What you need</h2>
          <ul className="mt-4 space-y-2 text-sm leading-relaxed text-mute">
            <li>— A legal copy of the game, on Windows.</li>
            <li>
              —{' '}
              <a href="https://github.com/BepInEx/BepInEx/releases" target="_blank" rel="noreferrer" className="text-teal-deep underline decoration-teal/40 underline-offset-4 hover:decoration-teal-deep">
                BepInEx 5.4.x
              </a>{' '}
              (the x64 build for 64-bit games).
            </li>
            <li>— One controller for each extra player.</li>
          </ul>
        </div>
        <div className="glass p-6">
          <h2 className="font-display text-lg font-medium">Settings</h2>
          <p className="mt-4 text-sm leading-relaxed text-mute">
            After the first launch with the mod, BepInEx creates a config file in <code className="text-teal-deep">BepInEx/config/</code>.
            Each mod page lists every option and its default value.
          </p>
        </div>
      </section>
    </>
  )
}
