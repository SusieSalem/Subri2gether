/** Decorative HUD pieces: tick bars, emblem, pixel-window slab. */

export function Bars({ className = '' }: { className?: string }) {
  return (
    <div className={`flex items-center gap-1.5 ${className}`} aria-hidden>
      {[18, 10, 10, 10, 26].map((w, i) => (
        <span key={i} className="h-1 bg-ink/25" style={{ width: w }} />
      ))}
    </div>
  )
}

export function Emblem({ className = 'size-24', strong = false }: { className?: string; strong?: boolean }) {
  const o = strong ? 0.8 : 0.35
  const ticks = Array.from({ length: 36 }, (_, i) => i * 10)
  return (
    <svg viewBox="-50 -50 100 100" className={className} fill="none" aria-hidden>
      <circle r="44" stroke="currentColor" strokeOpacity={o} strokeWidth={strong ? 2.5 : 0.6} />
      <circle r="36" stroke="currentColor" strokeOpacity={0.25} strokeWidth={0.4} strokeDasharray="2 3" />
      {ticks.map((a) => (
        <line key={a} x1="0" y1="-44" x2="0" y2={a % 90 === 0 ? -39 : -42} stroke="currentColor" strokeOpacity={o} strokeWidth={strong ? 1.5 : 0.5} transform={`rotate(${a})`} />
      ))}
      <text x="0" y={strong ? 12 : 8} textAnchor="middle" fontFamily="Chakra Petch, sans-serif" fontWeight={strong ? 600 : 300} fontSize={strong ? 34 : 24} fill="currentColor" fillOpacity={strong ? 1 : 0.7}>
        2P
      </text>
    </svg>
  )
}

/** Deterministic pseudo-random so the windows look the same on every render. */
function rand(seed: number) {
  const x = Math.sin(seed * 9301 + 49297) * 233280
  return x - Math.floor(x)
}

export function PixelSlab({ className = '', cols = 6, rows = 18 }: { className?: string; cols?: number; rows?: number }) {
  const cells: { x: number; y: number; rose: boolean; flick: boolean }[] = []
  for (let y = 0; y < rows; y++) {
    for (let x = 0; x < cols; x++) {
      const r = rand(y * cols + x + 1)
      // denser toward the bottom, like lit windows in the reference
      if (r < 0.12 + (y / rows) * 0.38) cells.push({ x, y, rose: r < 0.025, flick: r > 0.4 && r < 0.43 })
    }
  }
  return (
    <div className={`relative overflow-hidden bg-[linear-gradient(180deg,rgba(42,46,51,0.15),#2a2e33_45%,#1e2124)] ${className}`} aria-hidden>
      <svg viewBox={`0 0 ${cols * 4} ${rows * 4}`} className="absolute inset-0 size-full" preserveAspectRatio="xMidYMax slice" shapeRendering="crispEdges">
        {cells.map((c) => (
          <rect
            key={`${c.x}-${c.y}`}
            x={c.x * 4 + 1.25}
            y={c.y * 4 + 1.25}
            width="1.5"
            height="1.5"
            fill={c.rose ? '#ec5b85' : '#eef3f3'}
            fillOpacity={c.rose ? 1 : 0.55 + rand(c.x + c.y * 7) * 0.45}
            className={c.flick ? 'flicker' : undefined}
          />
        ))}
      </svg>
      <div className="absolute inset-0 bg-[radial-gradient(rgba(255,255,255,0.08)_1px,transparent_1px)] bg-[size:3px_3px]" />
    </div>
  )
}
