import DOMPurify from 'dompurify'

/** Sanitize user-authored HTML both on submit and before rendering (defense in depth). */
export function sanitizeHtml(html) {
  return DOMPurify.sanitize(html ?? '', {
    ALLOWED_TAGS: [
      'p', 'br', 'b', 'strong', 'i', 'em', 'u', 's', 'code', 'pre', 'blockquote',
      'ul', 'ol', 'li', 'a', 'img', 'hr', 'h1', 'h2', 'h3', 'span', 'div',
    ],
    ALLOWED_ATTR: ['href', 'src', 'alt', 'title', 'target', 'rel', 'class'],
    ADD_ATTR: ['target'],
  })
}

/** Ensure pasted/typed links open safely in a new tab. */
export function enforceSafeLinks(root) {
  if (!root) return
  root.querySelectorAll('a[href]').forEach((a) => {
    a.target = '_blank'
    a.rel = 'noopener noreferrer'
    const href = a.getAttribute('href') || ''
    if (href.toLowerCase().startsWith('javascript:')) a.removeAttribute('href')
  })
}
