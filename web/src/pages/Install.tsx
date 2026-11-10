import { PageHeader } from '../components/PageHeader'
import { Steps } from '../components/Steps'

export function Install() {
  return (
    <>
      <PageHeader title="Install">
        You need the game on Windows,{' '}
        <a href="https://github.com/BepInEx/BepInEx/releases" target="_blank" rel="noreferrer" className="text-teal-deep underline decoration-teal/40 underline-offset-4 hover:decoration-teal-deep">
          BepInEx 5.4.x
        </a>{' '}
        and one controller per extra player.
      </PageHeader>
      <Steps />
    </>
  )
}
