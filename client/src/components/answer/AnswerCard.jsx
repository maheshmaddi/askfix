import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  ArrowBigUp, ArrowBigDown, MessageSquareText, CheckCircle2, MoreHorizontal,
  Pencil, Trash2, BadgeCheck, Share2, Loader2,
} from 'lucide-react'
import RichContent from '../common/RichContent'
import Avatar from '../common/Avatar'
import ConfirmDialog from '../common/ConfirmDialog'
import RichEditor from './RichEditor'
import { voteAnswer, acceptAnswer, deleteAnswer, updateAnswer, errorMessage } from '../../lib/api'
import { useAuth } from '../../store/auth'
import { timeAgo, compactNumber } from '../../lib/format'
import CommentThread from './CommentThread'

export default function AnswerCard({ answer, question }) {
  const { user, isAdmin } = useAuth()
  const queryClient = useQueryClient()
  const [vote, setVote] = useState({ up: answer.upvoteCount, down: answer.downvoteCount, mine: answer.myVote })
  const [menuOpen, setMenuOpen] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [editing, setEditing] = useState(false)
  const [editHtml, setEditHtml] = useState(answer.bodyHtml)
  const [error, setError] = useState('')

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['answers', answer.questionId] })
    queryClient.invalidateQueries({ queryKey: ['question', answer.questionId] })
    queryClient.invalidateQueries({ queryKey: ['notifications'] })
  }

  const voteMu = useMutation({
    mutationFn: (value) => voteAnswer(answer.id, value),
    onMutate: (value) => {
      // optimistic: compute expected state
      setVote((v) => {
        const next = value === 0 ? 0 : value
        if (v.mine === 1) return { ...v, up: v.up - 1, mine: next }
        if (v.mine === -1) return { ...v, down: v.down - 1, mine: next }
        return { ...v, [next === 1 ? 'up' : 'down']: v[next === 1 ? 'up' : 'down'] + 1, mine: next }
      })
    },
    onError: (err, _, ctx) => {
      setVote({ up: answer.upvoteCount, down: answer.downvoteCount, mine: answer.myVote })
      setError(errorMessage(err))
    },
    onSuccess: (data) => setVote({ up: data.upvoteCount, down: data.downvoteCount, mine: data.myVote }),
  })

  const acceptMu = useMutation({ mutationFn: () => acceptAnswer(answer.id), onSuccess: invalidate })
  const deleteMu = useMutation({ mutationFn: () => deleteAnswer(answer.id), onSuccess: invalidate })
  const saveEdit = useMutation({
    mutationFn: () => updateAnswer(answer.id, editHtml),
    onSuccess: () => {
      setEditing(false)
      invalidate()
    },
    onError: (e) => setError(errorMessage(e)),
  })

  const score = vote.up - vote.down
  const viewerIsAuthor = user?.id === answer.author.id
  const canAccept = user?.id === question.author.id || isAdmin
  const share = async () => {
    const url = `${location.origin}/question/${question.id}#answer-${answer.id}`
    try {
      await navigator.clipboard.writeText(url)
    } catch {
      /* clipboard unavailable */
    }
  }

  return (
    <article id={`answer-${answer.id}`} className={`card p-5 ${answer.isAccepted ? 'ring-1 ring-emerald-500/30' : ''}`}>
      {answer.isAccepted && (
        <div className="flex items-center gap-2 text-[13px] font-bold text-emerald-700 bg-emerald-50 rounded-lg px-3 py-2 mb-4">
          <BadgeCheck size={16} />
          This answer worked for the asker
        </div>
      )}

      <div className="flex items-center gap-2.5 mb-3.5">
        <Link to={`/profile/${answer.author.id}`}>
          <Avatar name={answer.author.displayName} hue={answer.author.avatarHue} size={34} />
        </Link>
        <div className="min-w-0">
          <Link to={`/profile/${answer.author.id}`} className="text-[14px] font-bold hover:underline">
            {answer.author.displayName}
          </Link>
          <div className="text-[12px] text-ink-soft truncate">
            {answer.author.department} · {answer.author.badge}
          </div>
        </div>
        <span className="ml-auto text-[12.5px] text-ink-faint shrink-0">
          Answered {timeAgo(answer.createdAt)}
          {answer.updatedAt ? ' · edited' : ''}
        </span>
      </div>

      {editing ? (
        <div>
          <RichEditor value={editHtml} onChange={setEditHtml} minHeight={160} />
          <div className="flex justify-end gap-2 mt-3">
            <button className="btn-secondary" onClick={() => setEditing(false)}>Cancel</button>
            <button className="btn-primary" onClick={() => saveEdit.mutate()} disabled={saveEdit.isPending}>
              {saveEdit.isPending && <Loader2 size={14} className="animate-spin" />} Save edit
            </button>
          </div>
        </div>
      ) : (
        <RichContent html={answer.bodyHtml} />
      )}

      <div className="relative flex items-center gap-1 mt-4 pt-3.5 border-t border-line/70">
        <button
          className={`flex items-center gap-1 px-3 py-1.5 rounded-full text-[13.5px] font-bold transition-all ${
            vote.mine === 1 ? 'bg-brand text-white shadow-sm' : 'text-ink-soft hover:bg-brand-50 hover:text-brand'
          }`}
          onClick={() => voteMu.mutate(vote.mine === 1 ? 0 : 1)}
          aria-pressed={vote.mine === 1}
          aria-label="Upvote"
        >
          <ArrowBigUp size={17} className={vote.mine === 1 ? '' : 'fill-current/20'} />
          {compactNumber(vote.up)}
        </button>
        <button
          className={`flex items-center gap-1 px-2.5 py-1.5 rounded-full text-[13px] font-semibold transition-colors ${
            vote.mine === -1 ? 'bg-ink text-white' : 'text-ink-faint hover:bg-ink/[0.06] hover:text-ink'
          }`}
          onClick={() => voteMu.mutate(vote.mine === -1 ? 0 : -1)}
          aria-pressed={vote.mine === -1}
          aria-label="Downvote"
          title="Not helpful"
        >
          <ArrowBigDown size={16} />
        </button>
        <span className="text-[12.5px] text-ink-faint mx-1.5" title={`${vote.up} up · ${vote.down} down`}>
          {score > 0 ? `+${compactNumber(score)}` : compactNumber(score)}
        </span>

        <span className="flex-1" />

        {canAccept && (
          <button
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-[13px] font-semibold transition-colors ${
              answer.isAccepted
                ? 'text-emerald-700 bg-emerald-50 hover:bg-emerald-100'
                : 'text-ink-soft hover:bg-emerald-50 hover:text-emerald-700'
            }`}
            onClick={() => acceptMu.mutate()}
            title={answer.isAccepted ? 'Unmark as worked' : 'Mark this as the fix that worked'}
          >
            <CheckCircle2 size={16} />
            {answer.isAccepted ? 'Worked ✓' : 'This worked'}
          </button>
        )}

        <CommentThread answer={answer} />

        <button className="btn-ghost" onClick={share} title="Copy link">
          <Share2 size={15.5} />
        </button>

        {(viewerIsAuthor || isAdmin) && (
          <div className="relative">
            <button className="btn-ghost" onClick={() => setMenuOpen((v) => !v)} aria-label="More options">
              <MoreHorizontal size={16.5} />
            </button>
            {menuOpen && (
              <div className="pop-in absolute right-0 mt-1 w-40 card p-1.5 z-10 shadow-pop" onMouseLeave={() => setMenuOpen(false)}>
                {viewerIsAuthor && (
                  <button
                    className="w-full flex items-center gap-2 px-3 py-1.5 rounded-md text-[13.5px] hover:bg-ink/[0.05]"
                    onClick={() => {
                      setEditing(true)
                      setMenuOpen(false)
                    }}
                  >
                    <Pencil size={14} className="text-ink-soft" /> Edit answer
                  </button>
                )}
                <button
                  className="w-full flex items-center gap-2 px-3 py-1.5 rounded-md text-[13.5px] text-brand hover:bg-brand-50"
                  onClick={() => setConfirming(true)}
                >
                  <Trash2 size={14} /> Delete
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      {error && <div className="mt-2 text-[12.5px] text-brand">{error}</div>}

      <ConfirmDialog
        open={confirming}
        title="Delete this answer?"
        message="The answer, its comments and votes will be removed. This cannot be undone."
        onConfirm={() => deleteMu.mutate()}
        onClose={() => setConfirming(false)}
      />
    </article>
  )
}
