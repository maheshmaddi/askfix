/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        // CSS-variable tokens so both themes work without per-element dark: variants.
        // Values live in index.css (:root = light, .dark = dark). <alpha-value> keeps
        // utilities like bg-ink/[0.06] working in both modes.
        brand: {
          DEFAULT: 'rgb(var(--c-brand) / <alpha-value>)',
          dark: 'rgb(var(--c-brand-dark) / <alpha-value>)',
          soft: 'rgb(var(--c-brand-soft) / <alpha-value>)',
          50: 'rgb(var(--c-brand-50) / <alpha-value>)',
          100: 'rgb(var(--c-brand-100) / <alpha-value>)',
          violet: '#7C4FD8',
        },
        ink: {
          DEFAULT: 'rgb(var(--c-ink) / <alpha-value>)',
          soft: 'rgb(var(--c-ink-soft) / <alpha-value>)',
          faint: 'rgb(var(--c-ink-faint) / <alpha-value>)',
        },
        line: 'rgb(var(--c-line) / <alpha-value>)',
        canvas: 'rgb(var(--c-canvas) / <alpha-value>)',
        surface: 'rgb(var(--c-surface) / <alpha-value>)',
      },
      fontFamily: {
        sans: ['Inter', 'Segoe UI', 'system-ui', 'sans-serif'],
      },
      boxShadow: {
        card: '0 1px 2px rgba(0,0,0,0.08), 0 0 0 1px rgba(0,0,0,0.04)',
        pop: '0 4px 16px rgba(0,0,0,0.14)',
      },
      fontSize: {
        // optical size tuning for readability on light backgrounds
        'q-title': ['17px', { lineHeight: '1.35', fontWeight: '600' }],
        'q-page': ['24px', { lineHeight: '1.25', fontWeight: '700' }],
      },
    },
  },
  plugins: [],
}
