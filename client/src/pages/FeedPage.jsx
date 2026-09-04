import { useEffect, useRef } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { PenSquare, MessageSquarePlus, Clock, Flame, CircleHelp } from 'lucide-react'
import { getFeed, getTag } from '../lib/api'
import QuestionCard from '../components/question/QuestionCard'
import { FeedSkeleton } from '../components/common/Skeletons'
import EmptyState from '../components/common/EmptyState'
import { useAuth } from '../store/auth'

const TABS = [
  { key: 'latest', label: 'Latest', icon: Clock },
  { key: 'trending', label: 'Trending', icon: Flame },
  { key: 'unanswered', label: 'Unanswered', icon: CircleHelp },
]

function AskPrompt() {
  const { user } = useAuth()
  return (
    <div className="card p-4 mb-4 flex items-center gap-3.5">
      <span
        className="w-9 h-9 rounded-full bg-gradient-to-br from-brand to-brand-violet text-white flex items-center justify-center shrink-0 font-bold"
        aria-hidden="true"
      >
        A
      </span>
      <Link
        to="/ask"
        className="flex-1 rounded-full border border-line hover:border-brand/40 bg-ink/[0.03] hover:bg-white px-4 py-2 text-[14px] text-ink-faint transition-colors"
      >
        What tool is giving you trouble, {user?.displayName?.split(' ')[0]}?
      </Link>
      <Link to="/ask" className="btn-primary hidden sm:inline-flex">
        <PenSquare size={15} /> Ask
      </Link>
    </div>
  )
}

export default function FeedPage() {
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') ?? 'latest'
  const tag = params.get('tag') ?? undefined
  const sentinelRef = useRef(null)

  const { data: tagInfo } = useQuery({
    queryKey: ['tag', tag],
    queryFn: () => getTag(tag),
    enabled: !!tag,
  })

  const feed = useInfiniteQuery({
    queryKey: ['feed', tab, tag],
    queryFn: ({ pageParam }) => getFeed({ tab, tag, page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (last, all) => (last.hasMore ? all.length + 1 : undefined),
  })

  useEffect(() => {
    const el = sentinelRef.current
    if (!el) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && feed.hasNextPage && !feed.isFetchingNextPage) feed.fetchNextPage()
      },
      { rootMargin: '600px' },
    )
    observer.observe(el)
    return () => observer.disconnect()
  }, [feed.hasNextPage, feed.isFetchingNextPage, feed.fetchNextPage])

  const items = feed.data?.pages.flatMap((p) => p.items) ?? []

  return (
    <div>
      {tag && (
        <div className="card px-5 py-4 mb-4 flex items-center gap-3">
          <span className="w-10 h-10 rounded-xl flex items-center justify-center text-white font-extrabold" style={{ backgroundColor: tagInfo?.color ?? '#5457D6' }}>
            {tagInfo?.name?.[0] ?? '#'}
          </span>
          <div className="min-w-0">
            <h1 className="font-bold text-[17px]">{tagInfo?.name ?? tag}</h1>
            <p className="text-[13px] text-ink-soft truncate">{tagInfo?.description ?? 'Questions tagged with this topic'}</p>
          </div>
          <button className="btn-secondary ml-auto shrink-0" onClick={() => setParams({ tab })}>
            Clear filter
          </button>
        </div>
      )}

      <AskPrompt />

      <div className="flex items-center gap-1 mb-4 border-b border-line">
        {TABS.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setParams(tag ? { tab: key, tag } : { tab: key })}
            className={`flex items-center gap-1.5 px-3.5 pb-2.5 pt-1 text-[14px] font-semibold border-b-2 -mb-px transition-colors ${
              tab === key ? 'text-brand border-brand' : 'text-ink-soft border-transparent hover:text-ink'
            }`}
          >
            <Icon size={15.5} /> {label}
          </button>
        ))}
        {feed.data?.pages[0] && (
          <span className="ml-auto text-[12.5px] text-ink-faint pb-2.5">{feed.data.pages[0].total} questions</span>
        )}
      </div>

      <div className="space-y-4">
        {feed.isLoading && (
          <>
            <FeedSkeleton />
            <FeedSkeleton />
            <FeedSkeleton />
          </>
        )}

        {feed.isError && (
          <EmptyState
            icon={CircleHelp}
            title="Couldn't load the feed"
            subtitle="The server didn't respond. Refresh to try again."
            action={<button className="btn-primary" onClick={() => feed.refetch()}>Retry</button>}
          />
        )}

        {!feed.isLoading && !feed.isError && items.length === 0 && (
          <EmptyState
            icon={MessageSquarePlus}
            title={tab === 'unanswered' ? 'No unanswered questions right now 🎉' : 'No questions here yet'}
            subtitle={
              tab === 'unanswered'
                ? 'Every question has at least one answer. Check back later.'
                : 'Be the first to ask — your question probably helps someone else too.'
            }
            action={<Link to="/ask" className="btn-primary"><PenSquare size={15} /> Ask the first question</Link>}
          />
        )}

        {items.map((q) => (
          <QuestionCard key={q.id} question={q} />
        ))}

        {feed.isFetchingNextPage && <FeedSkeleton />}
        <div ref={sentinelRef} className="h-1" />
      </div>
    </div>
  )
}
