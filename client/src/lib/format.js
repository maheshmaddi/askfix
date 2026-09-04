export function timeAgo(input) {
  const date = typeof input === 'string' ? new Date(input) : input
  const seconds = Math.max(0, (Date.now() - date.getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = seconds / 60
  if (minutes < 60) return `${Math.floor(minutes)}m ago`
  const hours = minutes / 60
  if (hours < 24) return `${Math.floor(hours)}h ago`
  const days = hours / 24
  if (days < 7) return `${Math.floor(days)}d ago`
  const weeks = days / 7
  if (days < 30) return `${Math.floor(weeks)}w ago`
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: date.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined })
}

export function compactNumber(n) {
  if (n == null) return '0'
  if (Math.abs(n) < 1000) return `${n}`
  if (Math.abs(n) < 1_000_000) return `${(n / 1000).toFixed(n % 1000 >= 100 ? 1 : 0)}k`
  return `${(n / 1_000_000).toFixed(1)}m`
}

export function initials(name = '') {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export function avatarStyle(hue = 0) {
  return {
    backgroundColor: `hsl(${hue} 62% 46%)`,
  }
}

export function plural(n, word, pluralWord) {
  return `${compactNumber(n)} ${n === 1 ? word : (pluralWord ?? `${word}s`)}`
}
