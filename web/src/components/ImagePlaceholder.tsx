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
    <div role="img" aria-label={image.alt} className={`glass hud relative flex items-center justify-center overflow-hidden ${className}`}>
      <div className="absolute inset-0 bg-[linear-gradient(rgba(28,39,41,0.05)_1px,transparent_1px),linear-gradient(90deg,rgba(28,39,41,0.05)_1px,transparent_1px)] bg-[size:20px_20px]" />
      <p className="relative text-[11px] uppercase tracking-[0.22em] text-ink/35">{label ?? image.alt}</p>
    </div>
  )
}
