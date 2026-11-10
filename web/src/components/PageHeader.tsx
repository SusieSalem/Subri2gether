import type { ReactNode } from 'react'
import { Bars } from './Art'

export function PageHeader({ eyebrow, title, children }: { eyebrow?: string; title: string; children?: ReactNode }) {
  return (
    <header className="pb-12 pt-16 lg:pt-24">
      <Bars />
      {eyebrow && <p className="mt-6 text-[11px] uppercase tracking-[0.22em] text-mute">{eyebrow}</p>}
      <h1 className="mt-3 font-display text-5xl font-light tracking-wide text-ink sm:text-6xl">{title}</h1>
      {children && <div className="mt-5 max-w-2xl text-sm leading-relaxed text-mute">{children}</div>}
    </header>
  )
}
