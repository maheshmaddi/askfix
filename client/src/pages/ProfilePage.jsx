import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { MessageSquareText, Award, CircleHelp, Pencil, Check, X, Loader2 } from 'lucide-react'
import { getProfile, getUserQuestions, getUserAnswers, updateMe, errorMessage } from '../lib/api'
import { compactNumber, timeAgo } from '../lib/format'
import Avatar from '../components/common/Avatar'
import QuestionCard from '../components/question/QuestionCard'
import RichContent from '../components/common/RichContent'
import { useAuth } from '../store/auth'
import { FeedSkeleton, QuestionPageSkeleton } from '../components/common/Skeletons'
import EmptyState from '../components/common/EmptyState'

function AnswerRow({ item }) {
  return (
    <div className="card p-5">
      <Link to={`/question/${item.questionId}`} className="text-[15px] font-bold hover:text-brand leading-snug">
        {item.questionTitle}
      </Link>
      <div className="flex items-center gap-2 mt-2 mb-3 text-[12.5px] text-ink-soft">
        <Avatar name={item.answer.author.displayName} hue={item.answer.author.avatarHue} size={20} />
        <span>
          {item.answer.upvoteCount} upvotes
          {item.answer.isAccepted && (
            <span className="text-emerald-700 font-semibold ml-1.5">· marked as worked</span>
          )}
          <span className="text-ink-faint"> · {timeAgo(item.answer.createdAt)}</span>
        </span>
      </div>
      <RichContent html={item.answer.bodyHtml} className="max-h-32 overflow-hidden relative [&>*:nth-child(n+3)]:hidden" />
      <Link to={`/question/${item.questionId}`} className="inline-block mt-2.5 text-[13px] font-semibold text-brand hover:underline">
        Read full answer →
      </Link>
    </div>
  )
}

export default function ProfilePage() {
  const { id } = useParams()
  const userId = Number(id)
  const [tab, setTab] = useState('questions')
  const [bio, setBio] = useState(null)
  const [bioDraft, setBioDraft] = useState('')
  const [error, setError] = useState('')
  const queryClient = useQueryClient()
  const { user: viewer } = useAuth()

  const { data: profile, isLoading } = useQuery({
    queryKey: ['profile', userId],
    queryFn: () => getProfile(userId),
    retry: false,
  })
  const { data: questions = [], isLoading: loadingQ } = useQuery({
    queryKey: ['user-questions', userId],
    queryFn: () => getUserQuestions(userId),
    enabled: !!profile,
  })
  const { data: answers = [], isLoading: loadingA } = useQuery({
    queryKey: ['user-answers', userId],
    queryFn: () => getUserAnswers(userId),
    enabled: !!profile,
  })

  const saveBio = useMutation({
    mutationFn: () => updateMe(bioDraft.trim()),
    onSuccess: () => {
      setBio(null)
      queryClient.invalidateQueries({ queryKey: ['profile', userId] })
    },
    onError: (e) => setError(errorMessage(e)),
  })

  if (isLoading) return <QuestionPageSkeleton />
  if (!profile) {
    return (
      <EmptyState
        icon={CircleHelp}
        title="User not found"
        action={<Link to="/" className="btn-primary">Back to home</Link>}
      />
    )
  }

  const isViewer = viewer?.id === profile.id
  const stats = [
    { label: 'Questions', value: profile.questionCount },
    { label: 'Answers', value: profile.answerCount },
    { label: 'Upvotes earned', value: profile.upvotesReceived },
    { label: 'Marked as worked', value: profile.answersAccepted },
  ]

  return (
    <div>
      <div className="card p-6">
        <div className="flex flex-col sm:flex-row sm:items-center gap-4">
          <Avatar name={profile.displayName} hue={profile.avatarHue} size={76} />
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-[21px] font-extrabold">{profile.displayName}</h1>
              <span className="chip bg-brand-50 text-brand font-bold">
                <Award size={12.5} /> {profile.badge}
              </span>
            </div>
            <div className="text-[14px] text-ink-soft mt-0.5">
              {profile.department} {profile.email && <span className="text-ink-faint">· {profile.email}</span>}
            </div>
            <div className="text-[12.5px] text-ink-faint mt-0.5">
              Joined {timeAgo(profile.createdAt)} · {compactNumber(profile.reputation)} reputation
            </div>

            {isViewer && bio === null ? (
              <div className="group flex items-start gap-2 mt-2.5">
                <p className="text-[13.5px] text-ink-soft leading-relaxed flex-1">
                  {profile.bio || <span className="text-ink-faint italic">Add a short bio — what you know well, what you help with.</span>}
                </p>
                <button
                  className="opacity-0 group-hover:opacity-100 btn-ghost p-1.5"
                  onClick={() => {
                    setBioDraft(profile.bio ?? '')
                    setBio('editing')
                  }}
                  aria-label="Edit bio"
                >
                  <Pencil size={13.5} />
                </button>
              </div>
            ) : bio === 'editing' ? (
              <div className="mt-2.5 flex items-center gap-2">
                <input
                  className="input !py-2 text-[13.5px]"
                  value={bioDraft}
                  onChange={(e) => setBioDraft(e.target.value)}
                  maxLength={400}
                  placeholder="What you know well, what you help with…"
                  autoFocus
                />
                <button className="btn-primary !h-9 !px-3.5" onClick={() => saveBio.mutate()} disabled={saveBio.isPending}>
                  {saveBio.isPending ? <Loader2 size={14} className="animate-spin" /> : <Check size={15} />}
                </button>
                <button className="btn-secondary !h-9 !px-3" onClick={() => setBio(null)}>
                  <X size={15} />
                </button>
              </div>
            ) : (
              profile.bio && <p className="text-[13.5px] text-ink-soft leading-relaxed mt-2.5">{profile.bio}</p>
            )}
            {error && <div className="text-[12.5px] text-brand mt-1.5">{error}</div>}
          </div>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mt-6 pt-5 border-t border-line/70">
          {stats.map((s) => (
            <div key={s.label} className="text-center sm:text-left">
              <div className="text-[20px] font-extrabold text-brand">{compactNumber(s.value)}</div>
              <div className="text-[12px] text-ink-soft">{s.label}</div>
            </div>
          ))}
        </div>
      </div>

      <div className="flex items-center gap-1 mt-6 mb-4 border-b border-line">
        {[
          { key: 'questions', label: `Questions (${profile.questionCount})` },
          { key: 'answers', label: `Answers (${profile.answerCount})` },
        ].map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`px-4 pb-2.5 pt-1 text-[14px] font-semibold border-b-2 -mb-px transition-colors ${
              tab === t.key ? 'text-brand border-brand' : 'text-ink-soft border-transparent hover:text-ink'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="space-y-4">
        {tab === 'questions' ? (
          loadingQ ? (
            <>
              <FeedSkeleton />
              <FeedSkeleton />
            </>
          ) : questions.length === 0 ? (
            <EmptyState compact icon={MessageSquareText} title="No questions yet" subtitle={isViewer ? 'Ask your first question — it probably helps someone else too.' : undefined} action={isViewer && <Link to="/ask" className="btn-primary">Ask a question</Link>} />
          ) : (
            questions.map((q) => <QuestionCard key={q.id} question={q} />)
          )
        ) : loadingA ? (
          <>
            <FeedSkeleton />
            <FeedSkeleton />
          </>
        ) : answers.length === 0 ? (
          <EmptyState compact icon={MessageSquareText} title="No answers yet" subtitle={isViewer ? 'Find a question you can help with and share what worked.' : undefined} action={isViewer && <Link to="/?tab=unanswered" className="btn-primary">Answer a question</Link>} />
        ) : (
          answers.map((a) => <AnswerRow key={a.answer.id} item={a} />)
        )}
      </div>
    </div>
  )
}
