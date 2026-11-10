import type { ReactNode } from 'react'

export function PageHeader({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <header className="pb-12 pt-16 lg:pt-24">
      <h1 className="font-display text-5xl font-light tracking-wide text-ink sm:text-6xl">{title}</h1>
      {children && <div className="mt-5 max-w-xl text-sm leading-relaxed text-mute">{children}</div>}
    </header>
  )
}
