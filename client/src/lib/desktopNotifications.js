/**
 * Desktop (browser) notifications with graceful degradation.
 * Works while an AskFix tab is open; some browsers only allow the Notification
 * API on secure origins (HTTPS or localhost) — capability() reports that.
 */

const PREF_KEY = 'askfix_browserNotifs'

export function isSupported() {
  try {
    return typeof window !== 'undefined' && 'Notification' in window
  } catch {
    return false
  }
}

export function permission() {
  if (!isSupported()) return 'unsupported'
  return Notification.permission // "granted" | "denied" | "default"
}

export function isEnabled() {
  return localStorage.getItem(PREF_KEY) === '1' && permission() === 'granted'
}

export function setEnabled(on) {
  localStorage.setItem(PREF_KEY, on ? '1' : '0')
}

export async function requestPermission() {
  if (!isSupported()) return 'unsupported'
  try {
    return await Notification.requestPermission()
  } catch {
    return 'denied'
  }
}

/** Show one desktop notification; clicking focuses the tab and navigates. */
export function showDesktop(title, body, url) {
  if (!isEnabled()) return false
  try {
    const n = new Notification(title, { body, icon: '/favicon.svg', tag: url })
    n.onclick = () => {
      window.focus()
      if (url) window.location.href = url
    }
    return true
  } catch {
    return false // some browsers reject insecure origins here
  }
}
