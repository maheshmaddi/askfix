import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Search, MessageSquareText, Tags as TagsIcon, CircleHelp } from 'lucide-react'
import { search } from '../lib/api'
import QuestionCard from '../components/question/QuestionCard'
import RichContent from '../components/common/RichContent'
import TagChip from '../components/common/TagChip'
import { timeAgo } from '../lib/format'
import { FeedSkeleton } from '../components/common/Skeletons'
import EmptyState from '../components/common/EmptyState'
import { plural } from '../lib/format'

export default function SearchPage() {
  const [params] = useSearchParams()
  const q = params.get('q') ?? ''
  const [tab, setTab] = useState('all')

  const { data, isLoading } = useQuery({
    queryKey: ['search', q],
    queryFn: () => search(q),
    enabled: q.trim().length >= 2,
  })

  const TABS = [
    { key: 'all', label: 'All', count: data?.total },
    { key: 'questions', label: 'Questions', count: data?.questions?.length },
    { key: 'answers', label: 'Answers', count: data?.answers?.length },
    { key: 'tags', label: 'Tags', count: data?.tags?.length },
  ]

  return (
    <div>
      <div className="flex items-center gap-2.5 mb-1">
        <Search size={20} className="text-ink-faint" />
        <h1 className="text-[20px] font-extrabold truncate">Results for “{q}”</h1>
      </div>
      <p className="text-[13.5px] text-ink-soft mb-5">{data ? `${data.total} matches` : 'Searching…'}</p>

      <div className="flex items-center gap-1 mb-5 border-b border-line">
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`px-3.5 pb-2.5 pt-1 text-[14px] font-semibold border-b-2 -mb-px transition-colors ${
              tab === t.key ? 'text-brand border-brand' : 'text-ink-soft border-transparent hover:text-ink'
            }`}
          >
            {t.label}
            {t.count != null && <span className="ml-1 text-ink-faint font-normal">{t.count}</span>}
          </button>
        ))}
      </div>

      {isLoading && (
        <>
          <FeedSkeleton />
          <FeedSkeleton />
        </>
      )}

      {data && data.total === 0 && (
        <EmptyState
          icon={CircleHelp}
          title="No matches found"
          subtitle={`Nothing found for “${q}”. Try different words, or ask the question yourself.`}
          action={<Link to="/ask" className="btn-primary">Ask this question</Link>}
        />
      )}

      {data && tab !== 'answers' && tab !== 'tags' && data.questions.length > 0 && (
        <div className="space-y-4 mb-6">
          {(tab === 'all' ? data.questions.slice(0, 5) : data.questions).map((item) => (
            <QuestionCard key={`q-${item.id}`} question={item} />
          ))}
        </div>
      )}

      {data && tab !== 'questions' && tab !== 'tags' && data.answers.length > 0 && (
        <div>
          {tab === 'all' && <h3 className="text-[14.5px] font-extrabold mb-3">Matching answers</h3>}
          <div className="space-y-4">
            {(tab === 'all' ? data.answers.slice(0, 3) : data.answers).map((item) => (
              <div key={`a-${item.answer.id}`} className="card p-5">
                <Link to={`/question/${item.questionId}`} className="text-[15px] font-bold hover:text-brand">
                  {item.questionTitle}
                </Link>
                <div className="text-[12.5px] text-ink-soft mt-1.5 mb-3">
                  {item.answer.author.displayName} · {item.answer.upvoteCount} upvotes · {timeAgo(item.answer.createdAt)}
                </div>
                <RichContent html={item.answer.bodyHtml} className="max-h-28 overflow-hidden" />
                <Link to={`/question/${item.questionId}`} className="inline-block mt-2 text-[13px] font-semibold text-brand hover:underline">
                  View full answer →
                </Link>
              </div>
            ))}
          </div>
        </div>
      )}

      {data && tab !== 'questions' && tab !== 'answers' && data.tags.length > 0 && (
        <div>
          {tab === 'all' && <h3 className="text-[14.5px] font-extrabold mb-3">Matching tags</h3>}
          <div className="card p-5 flex flex-wrap gap-2">
            {data.tags.map((t) => (
              <TagChip key={t.id} tag={t} />
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
