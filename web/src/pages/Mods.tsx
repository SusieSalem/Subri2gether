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
        {mods.map((mod) => (
          <ModCard key={mod.id} mod={mod} />
        ))}
        <div className="grid min-h-64 place-items-center rounded-3xl border border-dashed border-white/10 p-6 text-center text-sm text-zinc-500">
          More co-op mods coming soon.
        </div>
      </div>
    </>
  )
}
