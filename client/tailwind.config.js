/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: '#5457D6',
          dark: '#3F42BD',
          soft: '#F1F2FC',
          50: '#EEEEFC',
          100: '#DFE2F9',
          violet: '#7C4FD8',
        },
        ink: {
          DEFAULT: '#191919',
          soft: '#636466',
          faint: '#939598',
        },
        line: '#DFE0E1',
        canvas: '#F7F7F8',
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
