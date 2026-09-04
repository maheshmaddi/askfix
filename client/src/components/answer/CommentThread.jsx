import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { MessageSquareText, Send, Loader2, Trash2 } from 'lucide-react'
import Avatar from '../common/Avatar'
import { getComments, addComment, deleteComment, errorMessage } from '../../lib/api'
import { useAuth } from '../../store/auth'
import { timeAgo } from '../../lib/format'

export default function CommentThread({ answer }) {
  const { user, isAdmin } = useAuth()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [body, setBody] = useState('')
  const [error, setError] = useState('')

  const { data: comments = [], isLoading } = useQuery({
    queryKey: ['comments', answer.id],
    queryFn: () => getComments(answer.id),
    enabled: open,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['comments', answer.id] })
    queryClient.invalidateQueries({ queryKey: ['answers', answer.questionId] })
  }

  const add = useMutation({
    mutationFn: () => addComment(answer.id, body.trim()),
    onSuccess: () => {
      setBody('')
      invalidate()
    },
    onError: (e) => setError(errorMessage(e)),
  })

  const remove = useMutation({ mutationFn: deleteComment, onSuccess: invalidate })

  return (
    <>
      <button className="btn-ghost" onClick={() => setOpen((v) => !v)}>
        <MessageSquareText size={15.5} />
        {answer.commentCount > 0 ? answer.commentCount : ''}
      </button>

      {open && (
        <div className="absolute left-0 right-0 top-full mt-1 card p-4 shadow-pop z-20 rounded-xl">
          <div className="text-[13px] font-bold mb-3">
            {answer.commentCount > 0 ? `${answer.commentCount} comment${answer.commentCount === 1 ? '' : 's'}` : 'Comments'}
          </div>

          {isLoading ? (
            <div className="flex justify-center py-3">
              <Loader2 size={17} className="animate-spin text-ink-faint" />
            </div>
          ) : (
            <ul className="space-y-3.5 max-h-72 overflow-y-auto">
              {comments.length === 0 && (
                <li className="text-[13px] text-ink-faint">No comments yet. Add one to clarify or thank the author.</li>
              )}
              {comments.map((c) => (
                <li key={c.id} className="flex gap-2.5 group">
                  <Avatar name={c.author.displayName} hue={c.author.avatarHue} size={26} />
                  <div className="min-w-0 flex-1">
                    <div className="text-[12.5px] leading-snug">
                      <span className="font-bold">{c.author.displayName}</span>
                      <span className="text-ink-faint ml-1.5">{timeAgo(c.createdAt)}</span>
                    </div>
                    <p className="text-[13.5px] text-ink/90 leading-[1.5] whitespace-pre-wrap break-words mt-0.5">{c.body}</p>
                  </div>
                  {(c.viewerIsAuthor || isAdmin) && (
                    <button
                      className="opacity-0 group-hover:opacity-100 text-ink-faint hover:text-brand p-1 transition-opacity"
                      onClick={() => remove.mutate(c.id)}
                      aria-label="Delete comment"
                    >
                      <Trash2 size={13} />
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )}

          <form
            className="flex items-center gap-2 mt-3.5 pt-3 border-t border-line/70"
            onSubmit={(e) => {
              e.preventDefault()
              if (body.trim().length >= 2) add.mutate()
            }}
          >
            <Avatar name={user?.displayName} hue={user?.avatarHue} size={26} />
            <input
              value={body}
              onChange={(e) => setBody(e.target.value)}
              placeholder="Add a comment…"
              maxLength={1000}
              className="flex-1 bg-ink/[0.04] hover:bg-ink/[0.055] focus:bg-white border border-transparent focus:border-brand/40 rounded-full px-3.5 py-1.5 text-[13px] outline-none transition-colors"
            />
            <button
              type="submit"
              disabled={body.trim().length < 2 || add.isPending}
              className="p-2 rounded-full text-brand hover:bg-brand-50 disabled:opacity-40"
              aria-label="Send comment"
            >
              {add.isPending ? <Loader2 size={15} className="animate-spin" /> : <Send size={15} />}
            </button>
          </form>
          {error && <div className="mt-1.5 text-[12px] text-brand">{error}</div>}
        </div>
      )}
    </>
  )
}
