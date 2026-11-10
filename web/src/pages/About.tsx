import { Emblem, PixelSlab } from '../components/Art'
import { PageHeader } from '../components/PageHeader'

export function About() {
  return (
    <>
      <div className="relative">
        <PageHeader eyebrow="About" title="Why Subri2gether">
          Some of the best games are made for one player. Subri2gether is where I collect the mods I build so they can be played
          together, on one screen, with friends on the couch.
        </PageHeader>
        <div className="absolute right-0 top-16 hidden items-end gap-4 lg:flex">
          <Emblem className="size-28 text-ink" />
          <PixelSlab className="h-48 w-16" cols={6} rows={18} />
        </div>
      </div>
      <div className="grid gap-6 lg:grid-cols-3">
        {[
          ['01', 'Open source', 'All the code is open source under the MIT license, so anyone can learn from it or build on it.'],
          ['02', 'No game files', 'Mods only patch the game at runtime. No game files, code or assets are included or redistributed.'],
          ['03', 'Local first', 'Everything runs on one PC: extra players just plug in a controller. No servers, no accounts.'],
        ].map(([n, t, d]) => (
          <div key={t} className="glass hud p-6">
            <span className="font-display text-4xl font-light text-ink/30">{n}</span>
            <h2 className="mt-3 font-display text-lg font-medium">{t}</h2>
            <p className="mt-2 text-sm leading-relaxed text-mute">{d}</p>
          </div>
        ))}
      </div>
    </>
  )
}
