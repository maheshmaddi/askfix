import { useEffect, useRef } from 'react'
import hljs from 'highlight.js/lib/common'
import { sanitizeHtml, enforceSafeLinks } from '../../lib/rich'

/**
 * Renders sanitized user HTML and decorates code blocks after mount:
 * syntax highlighting, a language label and a copy button.
 */
export default function RichContent({ html, className = '' }) {
  const ref = useRef(null)

  useEffect(() => {
    const root = ref.current
    if (!root) return
    enforceSafeLinks(root)

    root.querySelectorAll('pre').forEach((pre) => {
      if (pre.dataset.decorated) return
      const code = pre.querySelector('code')
      if (!code) return
      pre.dataset.decorated = '1'
      pre.classList.add('group')

      const match = /language-([\w+#.-]+)/.exec(code.className || '')
      if (match) {
        const lang = document.createElement('span')
        lang.className = 'code-lang'
        lang.textContent = match[1]
        pre.appendChild(lang)
      }
      try {
        hljs.highlightElement(code)
      } catch {
        /* leave as plain text if highlighting fails */
      }

      const btn = document.createElement('button')
      btn.type = 'button'
      btn.className = 'code-copy'
      btn.textContent = 'Copy'
      btn.addEventListener('click', () => {
        navigator.clipboard?.writeText(code.innerText).then(() => {
          btn.textContent = 'Copied!'
          btn.classList.add('copied')
          setTimeout(() => {
            btn.textContent = 'Copy'
            btn.classList.remove('copied')
          }, 1400)
        })
      })
      pre.appendChild(btn)
    })
  }, [html])

  return (
    <div
      ref={ref}
      className={`rich-content ${className}`}
      dangerouslySetInnerHTML={{ __html: sanitizeHtml(html) }}
    />
  )
}
