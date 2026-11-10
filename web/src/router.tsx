import { createBrowserRouter } from 'react-router-dom'
import { Layout } from './components/Layout'
import { About } from './pages/About'
import { Home } from './pages/Home'
import { Install } from './pages/Install'
import { ModDetail } from './pages/ModDetail'
import { Mods } from './pages/Mods'
import { NotFound } from './pages/NotFound'

export const router = createBrowserRouter(
  [
    {
      element: <Layout />,
      children: [
        { index: true, element: <Home /> },
        { path: 'mods', element: <Mods /> },
        { path: 'mods/:id', element: <ModDetail /> },
        { path: 'install', element: <Install /> },
        { path: 'about', element: <About /> },
        { path: '*', element: <NotFound /> },
      ],
    },
  ],
  { basename: import.meta.env.BASE_URL.replace(/\/$/, '') },
)
