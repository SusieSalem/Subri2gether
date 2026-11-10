import { PageHeader } from '../components/PageHeader'

export function About() {
  return (
    <>
      <PageHeader eyebrow="About" title="Why Subri2gether">
        Some of the best games are made for one player. Subri2gether is where I collect the mods I build so they can be played
        together, on one screen, with friends on the couch.
      </PageHeader>
      <div className="grid gap-6 lg:grid-cols-3">
        {[
          ['Open source', 'All the code is open source under the MIT license, so anyone can learn from it or build on it.'],
          ['No game files', 'Mods only patch the game at runtime. No game files, code or assets are included or redistributed.'],
          ['Local first', 'Everything runs on one PC: extra players just plug in a controller. No servers, no accounts.'],
        ].map(([t, d]) => (
          <div key={t} className="rounded-2xl border border-white/5 bg-zinc-900/40 p-6">
            <h2 className="font-semibold">{t}</h2>
            <p className="mt-2 text-sm leading-relaxed text-zinc-400">{d}</p>
          </div>
        ))}
      </div>
    </>
  )
}
