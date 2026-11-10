import { useState } from 'react'
import type { ModImage } from '../data/mods'

interface Props {
  image: ModImage
  className?: string
  label?: string
}

/** Shows the image if it exists; otherwise a placeholder with the alt text. */
export function ImagePlaceholder({ image, className = '', label }: Props) {
  const [failed, setFailed] = useState(false)

  if (image.src && !failed) {
    // Paths start at /images/...; prefix them with the site base (/subri2gether/).
    const src = image.src.startsWith('/') ? import.meta.env.BASE_URL + image.src.slice(1) : image.src
    return (
      <img
        src={src}
        alt={image.alt}
        className={`object-cover ${className}`}
        loading="lazy"
        onError={() => setFailed(true)}
      />
    )
  }

  return (
    <div
      role="img"
      aria-label={image.alt}
      className={`relative flex items-center justify-center overflow-hidden bg-zinc-900 ${className}`}
    >
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_20%,rgba(248,113,113,0.18),transparent_55%),radial-gradient(circle_at_80%_90%,rgba(56,189,248,0.10),transparent_50%)]" />
      <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,0.03)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.03)_1px,transparent_1px)] bg-[size:24px_24px]" />
      <div className="relative flex flex-col items-center gap-2 px-4 text-center text-zinc-500">
        <svg viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth={1.5}>
          <rect x="3" y="4" width="18" height="16" rx="2" />
          <circle cx="9" cy="10" r="1.75" />
          <path d="m21 16-5-5-8 8" />
        </svg>
        <span className="text-xs font-medium">{label ?? image.alt}</span>
      </div>
    </div>
  )
}
