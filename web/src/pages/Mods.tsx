import { Emblem } from '../components/Art'
import { ModCard } from '../components/ModCard'
import { PageHeader } from '../components/PageHeader'
import { mods } from '../data/mods'

export function Mods() {
  return (
    <>
      <PageHeader eyebrow="Catalog" title="Mods">
        Every mod comes with its source code, controls and settings. More games are on the way.
      </PageHeader>
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {mods.map((mod, i) => (
          <ModCard key={mod.id} mod={mod} index={i + 1} />
        ))}
        <div className="hud grid min-h-72 place-items-center p-6 text-center">
          <div>
            <Emblem className="mx-auto size-24 text-ink/50" />
            <p className="caret mt-4 text-[11px] uppercase tracking-[0.22em] text-mute">Next unit in production</p>
          </div>
        </div>
      </div>
    </>
  )
}
