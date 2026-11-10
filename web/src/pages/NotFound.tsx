import { Link } from 'react-router-dom'

export function NotFound() {
  return (
    <div className="py-32 text-center">
      <p className="font-mono text-sm text-red-400">404</p>
      <h1 className="mt-3 text-4xl font-bold tracking-tight">Page not found</h1>
      <Link to="/" className="mt-8 inline-block rounded-full bg-red-500 px-6 py-2.5 text-sm font-semibold text-white hover:bg-red-400">
        Back home
      </Link>
    </div>
  )
}
