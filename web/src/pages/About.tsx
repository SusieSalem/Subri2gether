import { PageHeader } from '../components/PageHeader'

export function About() {
  return (
    <PageHeader title="About">
      <p>Some of the best games are made for one player. I build mods so they can be played together, on one screen.</p>
      <p className="mt-4">Open source under MIT. No game files are included.</p>
    </PageHeader>
  )
}
