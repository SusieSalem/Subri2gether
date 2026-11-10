export function Logo({ className = 'size-8' }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={className} aria-hidden>
      <defs>
        <linearGradient id="logo-g" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#f87171" />
          <stop offset="1" stopColor="#fdba74" />
        </linearGradient>
      </defs>
      <rect width="32" height="32" rx="8" fill="rgba(239,68,68,0.15)" />
      <circle cx="12" cy="16" r="6" fill="none" stroke="url(#logo-g)" strokeWidth="2.5" />
      <circle cx="20" cy="16" r="6" fill="none" stroke="url(#logo-g)" strokeWidth="2.5" />
    </svg>
  )
}

export function Wordmark() {
  return (
    <span className="font-semibold tracking-tight">
      Subri<span className="bg-gradient-to-r from-red-400 to-orange-300 bg-clip-text text-transparent">2</span>gether
    </span>
  )
}
