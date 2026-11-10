import type { ReactNode } from 'react'

export function PageHeader({ eyebrow, title, children }: { eyebrow?: string; title: string; children?: ReactNode }) {
  return (
    <header className="pb-10 pt-16 lg:pt-20">
      {eyebrow && <p className="text-xs font-semibold uppercase tracking-[0.2em] text-red-300/80">{eyebrow}</p>}
      <h1 className="mt-3 text-4xl font-bold tracking-tight sm:text-5xl">{title}</h1>
      {children && <div className="mt-4 max-w-2xl text-lg leading-relaxed text-zinc-400">{children}</div>}
    </header>
  )
}
