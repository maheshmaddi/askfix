import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Tags as TagsIcon, CircleHelp } from 'lucide-react'
import { getTags, getFeed } from '../lib/api'
import TagChip from '../components/common/TagChip'
import QuestionCard from '../components/question/QuestionCard'
import { plural } from '../lib/format'
import { FeedSkeleton } from '../components/common/Skeletons'
import EmptyState from '../components/common/EmptyState'

export function TagsPage() {
  const { data: tags = [], isLoading } = useQuery({ queryKey: ['all-tags'], queryFn: () => getTags('popular', 200) })

  return (
    <div>
      <h1 className="text-q-page mb-1.5">Tags</h1>
      <p className="text-[14px] text-ink-soft mb-6">Tags group questions by tool or area. Following a tag keeps its questions easy to find.</p>

      {isLoading ? (
        <div className="grid sm:grid-cols-2 gap-3">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="card p-4">
              <div className="skeleton h-5 w-24 mb-2.5" />
              <div className="skeleton h-3.5 w-full" />
            </div>
          ))}
        </div>
      ) : tags.length === 0 ? (
        <EmptyState compact icon={TagsIcon} title="No tags yet" subtitle="Tags appear here as soon as questions are asked." />
      ) : (
        <div className="grid sm:grid-cols-2 gap-3">
          {tags.map((t) => (
            <Link
              key={t.id}
              to={`/tag/${t.slug}`}
              className="card p-4 flex items-start gap-3.5 hover:border-ink/25 transition-colors group"
            >
              <span
                className="w-10 h-10 rounded-xl flex items-center justify-center text-white font-extrabold text-[15px] shrink-0"
                style={{ backgroundColor: t.color }}
              >
                {t.name[0]}
              </span>
              <span className="min-w-0">
                <span className="block font-bold text-[15px] group-hover:text-brand transition-colors">{t.name}</span>
                <span className="block text-[13px] text-ink-soft leading-snug mt-0.5 line-clamp-2">
                  {t.description || 'No description'}
                </span>
                <span className="block text-[12px] text-ink-faint mt-1">{plural(t.questionCount, 'question')}</span>
              </span>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}

export function TagDetailPage() {
  const { slug } = useParams()
  const { data, isLoading } = useQuery({
    queryKey: ['feed', 'latest', slug],
    queryFn: () => getFeed({ tab: 'latest', tag: slug, pageSize: 50 }),
  })
  const { data: tag } = useQuery({
    queryKey: ['tag', slug],
    queryFn: () => getTags('popular', 200).then((all) => all.find((t) => t.slug === slug)),
  })

  return (
    <div>
      {tag && (
        <div className="card p-5 mb-4 flex items-center gap-3.5">
          <span className="w-11 h-11 rounded-xl flex items-center justify-center text-white font-extrabold text-[17px]" style={{ backgroundColor: tag.color }}>
            {tag.name[0]}
          </span>
          <div>
            <h1 className="text-[18px] font-extrabold">{tag.name}</h1>
            <p className="text-[13px] text-ink-soft">{tag.description || `Questions about ${tag.name}`}</p>
          </div>
          <span className="ml-auto text-[13px] text-ink-faint">{plural(tag.questionCount, 'question')}</span>
        </div>
      )}

      {isLoading ? (
        <>
          <FeedSkeleton />
          <FeedSkeleton />
        </>
      ) : data?.items?.length ? (
        <div className="space-y-4">
          {data.items.map((q) => (
            <QuestionCard key={q.id} question={q} />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={CircleHelp}
          title={`No questions tagged “${tag?.name ?? slug}” yet`}
          subtitle="Ask the first one — tag it so others find it."
          action={<Link to="/ask" className="btn-primary">Ask a question</Link>}
        />
      )}
    </div>
  )
}
