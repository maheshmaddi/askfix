import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Bookmark } from 'lucide-react'
import { getMyBookmarks } from '../lib/api'
import QuestionCard from '../components/question/QuestionCard'
import { FeedSkeleton } from '../components/common/Skeletons'
import EmptyState from '../components/common/EmptyState'

export default function BookmarksPage() {
  const { data: items = [], isLoading } = useQuery({ queryKey: ['bookmarks'], queryFn: getMyBookmarks })

  return (
    <div>
      <h1 className="text-q-page mb-5">Bookmarks</h1>
      {isLoading ? (
        <>
          <FeedSkeleton />
          <FeedSkeleton />
        </>
      ) : items.length === 0 ? (
        <EmptyState
          icon={Bookmark}
          title="Nothing saved yet"
          subtitle="Tap the bookmark icon on any question to keep it handy for later."
          action={<Link to="/" className="btn-primary">Browse questions</Link>}
        />
      ) : (
        <div className="space-y-4">
          {items.map((q) => (
            <QuestionCard key={q.id} question={q} />
          ))}
        </div>
      )}
    </div>
  )
}
