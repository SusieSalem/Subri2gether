import { Emblem } from './Art'

export function Logo({ className = 'size-8' }: { className?: string }) {
  return <Emblem strong className={`${className} text-teal-deep`} />
}

export function Wordmark() {
  return (
    <span className="font-display text-[15px] font-medium tracking-[0.08em]">
      SUBRI<span className="text-teal-deep">2</span>GETHER
    </span>
  )
}
