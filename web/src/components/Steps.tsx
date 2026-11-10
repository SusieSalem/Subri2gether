import { installSteps } from '../data/mods'

export function Steps() {
  return (
    <ol className="grid gap-4 md:grid-cols-3">
      {installSteps.map((s, i) => (
        <li key={s.title} className="rounded-2xl border border-white/5 bg-zinc-900/40 p-6">
          <span className="font-mono text-sm text-red-400">0{i + 1}</span>
          <h3 className="mt-3 font-semibold">{s.title}</h3>
          <p className="mt-2 text-sm leading-relaxed text-zinc-400">{s.text}</p>
        </li>
      ))}
    </ol>
  )
}
