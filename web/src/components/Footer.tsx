import { Wordmark } from './Logo'

const year = new Date().getFullYear()

export function Footer() {
  return (
    <footer className="mt-32">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 border-t border-ink/10 px-6 py-8 text-[11px] text-mute">
        <Wordmark />
        <p>© {year} · MIT</p>
      </div>
    </footer>
  )
}
