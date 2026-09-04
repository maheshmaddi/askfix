import { useMemo, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { X } from 'lucide-react'
import { getTags } from '../../lib/api'

export default function TagInput({ tags, onChange, max = 5 }) {
  const [input, setInput] = useState('')
  const { data: allTags = [] } = useQuery({
    queryKey: ['popular-tags'],
    queryFn: () => getTags('popular', 100),
    staleTime: Infinity,
  })
  const inputRef = useRef(null)

  const suggestions = useMemo(() => {
    const q = input.trim().toLowerCase().replace(/^[#]/, '')
    if (!q) return []
    return allTags.filter((t) => !tags.some((x) => x.slug === t.slug) && t.name.toLowerCase().includes(q)).slice(0, 5)
  }, [input, allTags, tags])

  const addTag = (tag) => {
    if (tags.length >= max || tags.some((t) => t.slug === tag.slug)) return
    onChange([...tags, tag])
    setInput('')
    inputRef.current?.focus()
  }

  const addCustom = () => {
    const name = input.trim().replace(/^[#]/, '')
    if (!name || tags.length >= max) return
    addTag({ id: -1, name, slug: name.toLowerCase().replace(/\s+/g, '-'), color: '#5457D6', questionCount: 0 })
  }

  return (
    <div>
      <div className="flex flex-wrap items-center gap-1.5 border border-line rounded-lg px-2 py-2 focus-within:border-brand/50 focus-within:ring-2 focus-within:ring-brand/15 transition-shadow bg-surface">
        {tags.map((t) => (
          <span key={t.slug} className="chip bg-brand-50 text-brand gap-1.5">
            <span className="w-[7px] h-[7px] rounded-full" style={{ backgroundColor: t.color }} />
            {t.name}
            <button type="button" onClick={() => onChange(tags.filter((x) => x.slug !== t.slug))} aria-label={`Remove ${t.name}`}>
              <X size={12.5} className="hover:text-brand-dark" />
            </button>
          </span>
        ))}
        {tags.length < max && (
          <input
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault()
                suggestions.length > 0 ? addTag(suggestions[0]) : addCustom()
              } else if (e.key === 'Backspace' && !input && tags.length) {
                onChange(tags.slice(0, -1))
              }
            }}
            placeholder={tags.length ? 'Add another…' : 'e.g. VPN, Git, Outlook…'}
            className="flex-1 min-w-[140px] outline-none text-[14px] bg-transparent px-1 py-0.5 placeholder:text-ink-faint"
          />
        )}
      </div>
      {suggestions.length > 0 && (
        <div className="flex flex-wrap gap-1.5 mt-2">
          {suggestions.map((s) => (
            <button key={s.slug} type="button" className="chip border border-line bg-surface hover:border-brand/50 hover:text-brand" onClick={() => addTag(s)}>
              + {s.name}
            </button>
          ))}
        </div>
      )}
      <p className="text-[12px] text-ink-faint mt-1.5">Up to {max} tags — pick the tool or area your problem is about.</p>
    </div>
  )
}
