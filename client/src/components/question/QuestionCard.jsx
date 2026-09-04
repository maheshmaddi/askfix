import { Link, useNavigate } from 'react-router-dom'
import { ArrowBigUp, MessageSquareText, Eye, CheckCircle2 } from 'lucide-react'
import TagChip from '../common/TagChip'
import Avatar from '../common/Avatar'
import { compactNumber, plural, timeAgo } from '../../lib/format'

export default function QuestionCard({ question, onAnswer }) {
  const navigate = useNavigate()
  return (
    <article
      className="card p-5 hover:border-ink/20 transition-colors cursor-pointer group"
      onClick={() => navigate(`/question/${question.id}`)}
    >
      <div className="flex items-center gap-2 mb-2 text-[12.5px] text-ink-soft">
        <Avatar name={question.author.displayName} hue={question.author.avatarHue} size={24} />
        <Link
          to={`/profile/${question.author.id}`}
          className="font-semibold hover:underline"
          onClick={(e) => e.stopPropagation()}
        >
          {question.author.displayName}
        </Link>
        <span className="text-ink-faint">·</span>
        <span className="truncate">{question.author.department || question.author.badge}</span>
        <span className="text-ink-faint">·</span>
        <span className="shrink-0 text-ink-faint">{timeAgo(question.createdAt)}</span>
      </div>

      <h2 className="text-q-title text-ink group-hover:text-brand transition-colors mb-1.5">
        {question.title}
        {question.hasAccepted && (
          <CheckCircle2 size={17} className="inline-block ml-1.5 -mt-0.5 text-emerald-600 dark:text-emerald-400" aria-label="Has a working answer" />
        )}
      </h2>

      {question.excerpt && <p className="text-[14px] text-ink-soft leading-[1.55] line-clamp-2 mb-3">{question.excerpt}</p>}

      {question.tags?.length > 0 && (
        <div className="flex flex-wrap gap-1.5 mb-3.5">
          {question.tags.map((t) => (
            <TagChip key={t.id} tag={t} size="sm" />
          ))}
        </div>
      )}

      <div className="flex items-center gap-4 text-[13px] text-ink-soft">
        <button
          className="flex items-center gap-1 font-semibold hover:text-brand px-2 py-1 -ml-2 rounded-md hover:bg-brand-50"
          onClick={(e) => {
            e.stopPropagation()
            onAnswer ? onAnswer() : navigate(`/question/${question.id}#answer`)
          }}
        >
          <MessageSquareText size={15.5} /> {plural(question.answerCount, 'answer')}
        </button>
        <span className="flex items-center gap-1" title="Upvotes on answers">
          <ArrowBigUp size={16} className="text-ink-faint" />
          {compactNumber(question.totalUpvotes)}
        </span>
        <span className="flex items-center gap-1">
          <Eye size={14.5} className="text-ink-faint" />
          {compactNumber(question.viewCount)}
        </span>
        <span className="ml-auto text-[12.5px] text-ink-faint">{plural(question.followerCount, 'follower')}</span>
      </div>
    </article>
  )
}
