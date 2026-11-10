import { Outlet, ScrollRestoration } from 'react-router-dom'
import { Footer } from './Footer'
import { Navbar } from './Navbar'

export function Layout() {
  return (
    <div className="grain flex min-h-screen flex-col">
      <Navbar />
      <main className="mx-auto w-full max-w-6xl flex-1 px-6">
        <Outlet />
      </main>
      <Footer />
      <ScrollRestoration />
    </div>
  )
}
