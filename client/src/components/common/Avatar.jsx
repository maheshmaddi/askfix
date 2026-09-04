import { avatarStyle, initials } from '../../lib/format'

export default function Avatar({ name, hue = 0, size = 32, className = '', ring = false }) {
  return (
    <span
      className={`inline-flex items-center justify-center rounded-full text-white font-semibold shrink-0 select-none ${
        ring ? 'ring-2 ring-white' : ''
      } ${className}`}
      style={{ ...avatarStyle(hue), width: size, height: size, fontSize: Math.max(10, size * 0.36) }}
      title={name}
      aria-hidden="true"
    >
      {initials(name)}
    </span>
  )
}
