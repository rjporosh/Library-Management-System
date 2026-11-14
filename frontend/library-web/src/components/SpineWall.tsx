import type { CSSProperties } from 'react'

// Deterministic "shelf" so server and client render identically.
const SPINES: { h: number; w: number; c: string }[] = [
  { h: 78, w: 34, c: '#0e5a4a' }, { h: 64, w: 26, c: '#b7832a' }, { h: 90, w: 40, c: '#1f8068' },
  { h: 70, w: 30, c: '#b4401f' }, { h: 84, w: 28, c: '#2f5176' }, { h: 60, w: 36, c: '#6e4e18' },
  { h: 92, w: 32, c: '#083a30' }, { h: 72, w: 24, c: '#c8932f' }, { h: 80, w: 38, c: '#4b78a6' },
  { h: 66, w: 30, c: '#94341a' }, { h: 88, w: 28, c: '#0b4a3d' }, { h: 74, w: 34, c: '#a6d2c5' },
  { h: 62, w: 26, c: '#384136' }, { h: 86, w: 36, c: '#d9754f' }, { h: 76, w: 30, c: '#1f8068' },
]

/** Decorative shelf of book spines - the signature motif of the sign-in screens. */
export function SpineWall({ className }: Readonly<{ className?: string }>) {
  return (
    <div aria-hidden className={`flex items-end gap-1.5 ${className ?? ''}`}>
      {SPINES.map((s, i) => (
        <span
          key={i}
          className="spine shelf-in rounded-t-[3px]"
          style={
            {
              height: `${s.h}%`,
              width: s.w,
              background: s.c,
              animationDelay: `${i * 55}ms`,
            } as CSSProperties
          }
        />
      ))}
    </div>
  )
}
