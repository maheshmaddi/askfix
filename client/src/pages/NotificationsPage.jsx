import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  MessageSquareText, ArrowBigUp, BadgeCheck, BellPlus, MessageCircle, BellOff, CheckCheck,
} from 'lucide-react'
import { getNotifications, markAllRead, markRead } from '../lib/api'
import { timeAgo } from '../lib/format'
import Avatar from '../components/common/Avatar'
import EmptyState from '../components/common/EmptyState'

const TYPE_META = {
  Answer: { icon: MessageSquareText, verb: 'answered your question', color: 'text-brand bg-brand-50' },
  Upvote: { icon: ArrowBigUp, verb: 'upvoted your answer', color: 'text-amber-600 bg-amber-50' },
  Comment: { icon: MessageCircle, verb: 'commented on your answer', color: 'text-sky-600 bg-sky-50' },
  Accepted: { icon: BadgeCheck, verb: 'marked your answer as the fix', color: 'text-emerald-600 bg-emerald-50' },
  Follow: { icon: BellPlus, verb: 'followed your question', color: 'text-violet-600 bg-violet-50' },
}

export default function NotificationsPage() {
  const queryClient = useQueryClient()
  const { data: page, isLoading } = useQuery({ queryKey: ['notifications'], queryFn: () => getNotifications() })
  const readAll = useMutation({
    mutationFn: markAllRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
      queryClient.invalidateQueries({ queryKey: ['unread-count'] })
    },
  })
  const readOne = useMutation({
    mutationFn: markRead,
    onMutate: (id) => {
      // optimistic mark-read
      const old = queryClient.getQueryData(['notifications'])
      return { old }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['unread-count'] })
    },
  })

  const items = page?.items ?? []
  const unread = items.filter((n) => !n.isRead).length

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-q-page">Notifications</h1>
        {unread > 0 && (
          <button className="btn-secondary" onClick={() => readAll.mutate()}>
            <CheckCheck size={15} /> Mark all read
          </button>
        )}
      </div>

      {isLoading ? (
        <div className="card divide-y divide-line/70">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="p-4 flex gap-3">
              <div className="skeleton w-9 h-9 rounded-full" />
              <div className="flex-1">
                <div className="skeleton h-4 w-2/3 mb-2" />
                <div className="skeleton h-3 w-1/3" />
              </div>
            </div>
          ))}
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          icon={BellOff}
          title="You're all caught up"
          subtitle="Answer questions or follow one — activity shows up here."
          action={<Link to="/?tab=unanswered" className="btn-primary">Find questions to answer</Link>}
        />
      ) : (
        <div className="card divide-y divide-line/70 overflow-hidden">
          {items.map((n) => {
            const meta = TYPE_META[n.type] ?? TYPE_META.Answer
            const Icon = meta.icon
            return (
              <Link
                key={n.id}
                to={`/question/${n.questionId}${n.answerId ? `#answer-${n.answerId}` : ''}`}
                className={`flex items-start gap-3 p-4 transition-colors ${
                  n.isRead ? 'hover:bg-ink/[0.03]' : 'bg-brand-50/40 hover:bg-brand-50/70'
                }`}
                onClick={() => !n.isRead && readOne.mutate(n.id)}
              >
                <span className={`w-9 h-9 rounded-full flex items-center justify-center shrink-0 ${meta.color}`}>
                  <Icon size={16} />
                </span>
                <Avatar name={n.actorName} hue={n.actorAvatarHue} size={30} />
                <div className="min-w-0 flex-1">
                  <p className="text-[14px] leading-snug">
                    <span className="font-bold">{n.actorName}</span> <span className="text-ink-soft">{meta.verb}</span>{' '}
                    <span className="font-semibold text-ink">“{n.questionTitle}”</span>
                  </p>
                  <span className="text-[12px] text-ink-faint">{timeAgo(n.createdAt)}</span>
                </div>
                {!n.isRead && <span className="w-2 h-2 rounded-full bg-brand shrink-0 mt-2" aria-label="Unread" />}
              </Link>
            )
          })}
        </div>
      )}
    </div>
  )
}
