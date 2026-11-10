import { installSteps } from '../data/mods'

export function Steps() {
  return (
    <ol className="grid gap-4 md:grid-cols-3">
      {installSteps.map((s, i) => (
        <li key={s.title} className="glass hud p-6">
          <span className="font-display text-4xl font-light text-ink/70">0{i + 1}</span>
          <h3 className="mt-4 font-display text-lg font-medium text-ink">{s.title}</h3>
          <p className="mt-2 text-sm leading-relaxed text-mute">{s.text}</p>
        </li>
      ))}
    </ol>
  )
}
