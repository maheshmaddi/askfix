import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BellPlus, BellOff, Bookmark, BookmarkCheck, Share2, Eye, MessageSquareText,
  Pencil, Trash2, Loader2, CircleHelp, BadgeCheck, ChevronDown, X,
} from 'lucide-react'
import {
  getQuestion, getAnswers, toggleFollow, toggleBookmark, createAnswer,
  updateQuestion, deleteQuestion, errorMessage,
} from '../lib/api'
import { compactNumber, timeAgo } from '../lib/format'
import Avatar from '../components/common/Avatar'
import TagChip from '../components/common/TagChip'
import TagInput from '../components/common/TagInput'
import RichContent from '../components/common/RichContent'
import RichEditor from '../components/answer/RichEditor'
import AnswerCard from '../components/answer/AnswerCard'
import ConfirmDialog from '../components/common/ConfirmDialog'
import { QuestionPageSkeleton } from '../components/common/Skeletons'
import EmptyState from '../components/common/EmptyState'
import { useAuth } from '../store/auth'

function QuestionHeader({ question }) {
  const queryClient = useQueryClient()
  const { user, isAdmin } = useAuth()
  const [following, setFollowing] = useState(question.isFollowing)
  const [bookmarked, setBookmarked] = useState(question.isBookmarked)
  const [copied, setCopied] = useState(false)
  const [editing, setEditing] = useState(false)
  const [edit, setEdit] = useState({ title: question.title, bodyHtml: question.bodyHtml || '' })
  const [tags, setTags] = useState(question.tags)
  const [confirming, setConfirming] = useState(false)
  const [error, setError] = useState('')
  const navigate = useNavigate()

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['question', question.id] })
    queryClient.invalidateQueries({ queryKey: ['feed'] })
  }

  const followMu = useMutation({
    mutationFn: toggleFollow,
    onSuccess: (r) => {
      setFollowing(r.enabled)
      invalidate()
    },
  })
  const bookmarkMu = useMutation({ mutationFn: toggleBookmark, onSuccess: (r) => setBookmarked(r.enabled) })
  const saveMu = useMutation({
    mutationFn: () => updateQuestion(question.id, { title: edit.title.trim(), bodyHtml: edit.bodyHtml || null, tagNames: tags.map((t) => t.name) }),
    onSuccess: () => {
      setEditing(false)
      invalidate()
      queryClient.invalidateQueries({ queryKey: ['feed'] })
    },
    onError: (e) => setError(errorMessage(e)),
  })
  const deleteMu = useMutation({
    mutationFn: () => deleteQuestion(question.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feed'] })
      navigate('/')
    },
  })

  const share = async () => {
    try {
      await navigator.clipboard.writeText(`${location.origin}/question/${question.id}`)
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    } catch {
      /* ignore */
    }
  }

  const viewerIsAuthor = user?.id === question.author.id

  return (
    <div className="card p-6">
      <div className="flex items-start justify-between gap-3">
        <h1 className="text-q-page leading-[1.22] flex-1">{question.title}</h1>
        <div className="flex items-center gap-1 shrink-0">
          {(viewerIsAuthor || isAdmin) && !editing && (
            <>
              <button className="btn-ghost" onClick={() => setEditing(true)} title="Edit question">
                <Pencil size={15.5} />
              </button>
              <button className="btn-ghost hover:!text-brand" onClick={() => setConfirming(true)} title="Delete question">
                <Trash2 size={15.5} />
              </button>
            </>
          )}
        </div>
      </div>

      {editing ? (
        <div className="mt-4 space-y-4">
          <textarea
            value={edit.title}
            onChange={(e) => setEdit({ ...edit, title: e.target.value.slice(0, 300) })}
            rows={2}
            className="input text-[19px] font-bold resize-none"
          />
          <RichEditor value={edit.bodyHtml} onChange={(html) => setEdit({ ...edit, bodyHtml: html })} />
          <TagInput tags={tags} onChange={setTags} />
          {error && <div className="text-[13px] text-brand">{error}</div>}
          <div className="flex justify-end gap-2">
            <button className="btn-secondary" onClick={() => setEditing(false)}>Cancel</button>
            <button className="btn-primary" onClick={() => saveMu.mutate()} disabled={saveMu.isPending}>
              {saveMu.isPending && <Loader2 size={14} className="animate-spin" />} Save changes
            </button>
          </div>
        </div>
      ) : (
        <>
          <div className="flex flex-wrap items-center gap-1.5 mt-3">
            {question.tags.map((t) => (
              <TagChip key={t.id} tag={t} />
            ))}
          </div>

          {question.bodyHtml && (
            <div className="mt-4">
              <RichContent html={question.bodyHtml} />
            </div>
          )}

          <div className="flex flex-wrap items-center gap-2 mt-5 pt-4 border-t border-line/70">
            <Avatar name={question.author.displayName} hue={question.author.avatarHue} size={30} />
            <Link to={`/profile/${question.author.id}`} className="text-[13.5px] font-bold hover:underline">
              {question.author.displayName}
            </Link>
            <span className="text-[12.5px] text-ink-soft">{question.author.department}</span>
            <span className="text-[12.5px] text-ink-faint">· asked {timeAgo(question.createdAt)}</span>

            <span className="flex-1" />

            <span className="hidden sm:flex items-center gap-1 text-[12.5px] text-ink-faint" title="Views">
              <Eye size={14} /> {compactNumber(question.viewCount)}
            </span>
            <button
              className={`btn text-[13px] font-semibold px-3 h-8 gap-1.5 ${
                following ? 'text-brand bg-brand-50 hover:bg-brand-100' : 'text-ink-soft hover:bg-ink/[0.06]'
              }`}
              onClick={() => followMu.mutate(question.id)}
            >
              {following ? <BellOff size={15} /> : <BellPlus size={15} />}
              {following ? 'Following' : 'Follow'}
              {question.followerCount > 0 && <span className="font-bold">· {question.followerCount}</span>}
            </button>
            <button
              className={`btn px-2.5 h-8 ${bookmarked ? 'text-brand bg-brand-50' : 'text-ink-soft hover:bg-ink/[0.06]'}`}
              onClick={() => bookmarkMu.mutate(question.id)}
              aria-label={bookmarked ? 'Remove bookmark' : 'Bookmark'}
              title={bookmarked ? 'Remove bookmark' : 'Save to bookmarks'}
            >
              {bookmarked ? <BookmarkCheck size={16} /> : <Bookmark size={16} />}
            </button>
            <button className="btn px-2.5 h-8 text-ink-soft hover:bg-ink/[0.06]" onClick={share} title="Copy link">
              <Share2 size={15} />
              {copied && <span className="text-[11.5px] font-semibold text-emerald-600">Copied!</span>}
            </button>
          </div>
        </>
      )}

      <ConfirmDialog
        open={confirming}
        title="Delete this question?"
        message="The question with all its answers, comments and votes will be permanently removed."
        onConfirm={() => deleteMu.mutate()}
        onClose={() => setConfirming(false)}
      />
    </div>
  )
}

function AnswerEditor({ questionId }) {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [expanded, setExpanded] = useState(false)
  const [bodyHtml, setBodyHtml] = useState('')
  const [error, setError] = useState('')

  const submit = useMutation({
    mutationFn: () => createAnswer(questionId, bodyHtml),
    onSuccess: () => {
      setBodyHtml('')
      setExpanded(false)
      queryClient.invalidateQueries({ queryKey: ['answers', questionId] })
      queryClient.invalidateQueries({ queryKey: ['question', questionId] })
    },
    onError: (e) => setError(errorMessage(e)),
  })

  if (!expanded) {
    return (
      <button
        onClick={() => setExpanded(true)}
        className="card w-full p-4 flex items-center gap-3.5 hover:border-brand/40 transition-colors text-left"
        id="answer"
      >
        <Avatar name={user?.displayName} hue={user?.avatarHue} size={34} />
        <span className="text-[14.5px] text-ink-faint">Add your answer — what fixed it for you?</span>
      </button>
    )
  }

  return (
    <div className="card p-5" id="answer">
      <div className="flex items-center justify-between mb-3.5">
        <div className="flex items-center gap-2.5">
          <Avatar name={user?.displayName} hue={user?.avatarHue} size={34} />
          <div>
            <div className="text-[14px] font-bold leading-tight">{user?.displayName}</div>
            <div className="text-[12px] text-ink-faint">{user?.department}</div>
          </div>
        </div>
        <button className="btn-ghost" onClick={() => setExpanded(false)} aria-label="Close editor">
          <X size={17} />
        </button>
      </div>
      <RichEditor value={bodyHtml} onChange={setBodyHtml} placeholder="Share what fixed it — steps, commands, screenshots…" minHeight={170} onError={(e) => setError(errorMessage(e))} />
      {error && <div className="mt-2.5 text-[13px] text-brand">{error}</div>}
      <div className="flex justify-end mt-4">
        <button className="btn-primary h-10 px-5" onClick={() => submit.mutate()} disabled={submit.isPending || !bodyHtml.trim()}>
          {submit.isPending && <Loader2 size={15} className="animate-spin" />}
          {submit.isPending ? 'Posting…' : 'Post answer'}
        </button>
      </div>
    </div>
  )
}

export default function QuestionPage() {
  const { id } = useParams()
  const questionId = Number(id)
  const [sort, setSort] = useState('top')

  const { data: question, isError: qError } = useQuery({
    queryKey: ['question', questionId],
    queryFn: () => getQuestion(questionId),
    retry: false,
  })
  const { data: answersPage } = useQuery({
    queryKey: ['answers', questionId, sort],
    queryFn: () => getAnswers(questionId, sort),
    enabled: !!question,
  })

  useEffect(() => {
    if (question) document.title = `${question.title} — AskFix`
    return () => {
      document.title = 'AskFix — ask. answer. fix.'
    }
  }, [question])

  const answers = useMemo(() => answersPage?.items ?? [], [answersPage])
  if (!question) {
    return qError ? (
      <EmptyState
        icon={CircleHelp}
        title="Question not found"
        subtitle="It may have been deleted, or the link is wrong."
        action={<Link to="/" className="btn-primary">Back to home</Link>}
      />
    ) : (
      <QuestionPageSkeleton />
    )
  }

  const hasAccepted = answers.some((a) => a.isAccepted)
  return (
    <div>
      <QuestionHeader question={question} />

      <div className="flex items-center gap-3 mt-6 mb-3.5">
        <h2 className="text-[16.5px] font-extrabold">{question.answerCount} {question.answerCount === 1 ? 'Answer' : 'Answers'}</h2>
        {hasAccepted && (
          <span className="flex items-center gap-1 chip bg-emerald-50 text-emerald-700 font-bold">
            <BadgeCheck size={13.5} /> Contains a working fix
          </span>
        )}
        <div className="ml-auto relative">
          <select
            value={sort}
            onChange={(e) => setSort(e.target.value)}
            className="appearance-none bg-white border border-line rounded-full pl-3.5 pr-8 py-1.5 text-[13px] font-semibold cursor-pointer hover:border-ink/25 outline-none"
            aria-label="Sort answers"
          >
            <option value="top">Top rated</option>
            <option value="new">Newest</option>
          </select>
          <ChevronDown size={13} className="absolute right-2.5 top-1/2 -translate-y-1/2 text-ink-faint pointer-events-none" />
        </div>
      </div>

      <div className="space-y-4">
        {answers.map((a) => (
          <AnswerCard key={a.id} answer={a} question={question} />
        ))}
        {answers.length === 0 && (
          <div className="card p-8 text-center">
            <MessageSquareText size={26} className="mx-auto text-ink-faint mb-2.5" />
            <p className="text-[14.5px] font-semibold">No answers yet</p>
            <p className="text-[13.5px] text-ink-soft mt-1 mb-4">Be the first to help — even partial leads count.</p>
          </div>
        )}
      </div>

      <div className="mt-5">
        <AnswerEditor questionId={questionId} />
      </div>
    </div>
  )
}
