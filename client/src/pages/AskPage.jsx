import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { CircleHelp, Sparkles, PenSquare, Loader2 } from 'lucide-react'
import { createQuestion, similarQuestions, errorMessage } from '../lib/api'
import { useDebounce } from '../hooks/useDebounce'
import RichEditor from '../components/answer/RichEditor'
import TagInput from '../components/common/TagInput'
import Avatar from '../components/common/Avatar'
import { useAuth } from '../store/auth'

function useDebouncedSimilar(title) {
  const debounced = useDebounce(title, 400)
  const enabled = debounced.trim().length >= 12
  return useQuery({
    queryKey: ['similar', debounced],
    queryFn: () => similarQuestions(debounced),
    enabled,
    staleTime: 30_000,
  })
}

export default function AskPage() {
  const [title, setTitle] = useState('')
  const [bodyHtml, setBodyHtml] = useState('')
  const [tags, setTags] = useState([])
  const [error, setError] = useState('')
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const similar = useDebouncedSimilar(title)

  const create = useMutation({
    mutationFn: () => createQuestion({ title: title.trim(), bodyHtml: bodyHtml || null, tagNames: tags.map((t) => t.name) }),
    onSuccess: (q) => {
      queryClient.invalidateQueries({ queryKey: ['feed'] })
      queryClient.invalidateQueries({ queryKey: ['trending'] })
      navigate(`/question/${q.id}`, { state: { justAsked: true } })
    },
    onError: (err) => setError(errorMessage(err)),
  })

  const titleValid = title.trim().length >= 10
  const canSubmit = titleValid && tags.length > 0 && !create.isPending

  return (
    <div className="max-w-[720px]">
      <h1 className="text-q-page mb-1">Ask a question</h1>
      <p className="text-[14.5px] text-ink-soft mb-6">
        Describe the problem exactly as you see it — error text, what you tried, what happened.
      </p>

      <div className="card p-6">
        <div className="flex items-center gap-2.5 mb-5">
          <Avatar name={user?.displayName} hue={user?.avatarHue} size={36} />
          <div>
            <div className="text-[14px] font-semibold leading-tight">{user?.displayName}</div>
            <div className="text-[12.5px] text-ink-faint">{user?.department}</div>
          </div>
        </div>

        <label className="block text-[13px] font-semibold mb-1.5" htmlFor="q-title">
          Question title <span className="text-ink-faint font-normal">(what's broken, in one line)</span>
        </label>
        <div className="relative">
          <textarea
            id="q-title"
            value={title}
            onChange={(e) => setTitle(e.target.value.slice(0, 300))}
            rows={title.length > 60 ? 3 : 2}
            placeholder="e.g. npm install fails with ETIMEDOUT behind the corporate proxy"
            className="input text-[17px] font-semibold resize-none leading-snug"
            autoFocus
          />
          <span className={`absolute right-3 bottom-2 text-[11px] ${title.length > 280 ? 'text-brand' : 'text-ink-faint'}`}>
            {title.length}/300
          </span>
        </div>

        {similar.data?.length > 0 && titleValid && (
          <div className="mt-3 rounded-xl border border-amber-200 bg-amber-50/70 p-3.5">
            <div className="flex items-center gap-1.5 text-[13px] font-bold text-amber-900 mb-2">
              <Sparkles size={14} /> Similar questions — maybe already answered:
            </div>
            <ul className="space-y-1.5">
              {similar.data.map((s) => (
                <li key={s.id}>
                  <Link to={`/question/${s.id}`} className="text-[13.5px] font-semibold text-ink hover:text-brand hover:underline">
                    {s.title}
                  </Link>
                  <span className="text-[11.5px] text-ink-faint ml-1.5">· {s.answerCount} answers</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="mt-5">
          <label className="block text-[13px] font-semibold mb-1.5">
            Details <span className="text-ink-faint font-normal">(optional — error output, screenshots, what you tried)</span>
          </label>
          <RichEditor value={bodyHtml} onChange={setBodyHtml} placeholder="Paste error messages, add screenshots, describe what you already tried…" minHeight={120} onError={(e) => setError(errorMessage(e))} />
        </div>

        <div className="mt-5">
          <label className="block text-[13px] font-semibold mb-1.5">Tags</label>
          <TagInput tags={tags} onChange={setTags} />
        </div>

        {error && <div className="mt-4 text-[13.5px] text-brand-dark bg-brand-50 border border-brand/25 rounded-lg px-3.5 py-2.5">{error}</div>}

        <div className="flex items-center justify-end gap-2.5 mt-6 pt-5 border-t border-line/70">
          <button className="btn-secondary" onClick={() => navigate(-1)} disabled={create.isPending}>
            Cancel
          </button>
          <button className="btn-primary" disabled={!canSubmit} onClick={() => create.mutate()}>
            {create.isPending ? <Loader2 size={15} className="animate-spin" /> : <PenSquare size={15} />}
            {create.isPending ? 'Posting…' : 'Post question'}
          </button>
        </div>
      </div>

      <div className="card p-5 mt-4 bg-gradient-to-br from-brand-50/60 to-white">
        <div className="flex gap-3">
          <CircleHelp size={20} className="text-brand shrink-0 mt-0.5" />
          <div className="text-[13.5px] text-ink-soft leading-relaxed">
            <span className="font-bold text-ink">Tips for fast answers:</span> include exact error text, the tool version,
            what you already tried, and a tag for the tool. Questions with code or error snippets get answered 3× faster.
          </div>
        </div>
      </div>
    </div>
  )
}
