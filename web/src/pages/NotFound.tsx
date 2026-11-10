import { Link } from 'react-router-dom'
import { btnPrimary } from '../components/ui'

export function NotFound() {
  return (
    <div className="py-32 text-center">
      <p className="font-display text-8xl font-light text-ink/80">404</p>
      <p className="caret mt-4 text-[11px] uppercase tracking-[0.22em] text-mute">Unit not found</p>
      <Link to="/" className={`${btnPrimary} mt-10`}>
        Back home
      </Link>
    </div>
  )
}
