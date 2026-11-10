import { ModCard } from '../components/ModCard'
import { PageHeader } from '../components/PageHeader'
import { mods } from '../data/mods'

export function Mods() {
  return (
    <>
      <PageHeader title="Mods" />
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {mods.map((mod) => (
          <ModCard key={mod.id} mod={mod} />
        ))}
      </div>
    </>
  )
}
