/**
 * Theme handling: light / dark, persisted in localStorage, follows the OS
 * setting by default. The .dark class on <html> drives all CSS-variable tokens;
 * index.html applies it before the bundle loads to avoid a flash.
 */

const THEME_KEY = 'askfix_theme'

export function storedTheme() {
  try {
    return localStorage.getItem(THEME_KEY) // 'light' | 'dark' | null (= system)
  } catch {
    return null
  }
}

export function isDark() {
  return document.documentElement.classList.contains('dark')
}

export function effectiveTheme() {
  const t = storedTheme()
  return t ?? 'system'
}

export function applyTheme(theme) {
  const dark = theme === 'dark' || (theme !== 'light' && systemPrefersDark())
  document.documentElement.classList.toggle('dark', dark)
  return dark
}

export function setTheme(theme) {
  try {
    if (theme === 'system') localStorage.removeItem(THEME_KEY)
    else localStorage.setItem(THEME_KEY, theme)
  } catch {
    /* storage unavailable */
  }
  return applyTheme(theme === 'system' ? null : theme)
}

export function toggleTheme() {
  return setTheme(isDark() ? 'light' : 'dark')
}

function systemPrefersDark() {
  return typeof matchMedia === 'function' && matchMedia('(prefers-color-scheme: dark)').matches
}
