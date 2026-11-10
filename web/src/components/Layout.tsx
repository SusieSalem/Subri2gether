import { Outlet, ScrollRestoration } from 'react-router-dom'
import { Bars } from './Art'
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
      <Bars className="mx-auto mb-6 justify-center" />
      <ScrollRestoration />
    </div>
  )
}
