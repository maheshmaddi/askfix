export function FeedSkeleton() {
  return (
    <div className="card p-5" aria-hidden="true">
      <div className="flex items-center gap-2.5 mb-3">
        <div className="skeleton w-7 h-7 rounded-full" />
        <div className="skeleton h-3.5 w-36" />
      </div>
      <div className="skeleton h-5 w-[85%] mb-2.5" />
      <div className="skeleton h-5 w-[60%] mb-3" />
      <div className="skeleton h-3.5 w-full mb-2" />
      <div className="skeleton h-3.5 w-[70%] mb-4" />
      <div className="flex gap-2">
        <div className="skeleton h-6 w-16 rounded-full" />
        <div className="skeleton h-6 w-20 rounded-full" />
        <div className="skeleton h-6 w-14 rounded-full" />
      </div>
    </div>
  )
}

export function QuestionPageSkeleton() {
  return (
    <div aria-hidden="true">
      <div className="card p-6 mb-4">
        <div className="skeleton h-6 w-[80%] mb-4" />
        <div className="skeleton h-4 w-full mb-2" />
        <div className="skeleton h-4 w-[90%] mb-2" />
        <div className="skeleton h-4 w-[50%] mb-5" />
        <div className="flex items-center gap-3">
          <div className="skeleton w-9 h-9 rounded-full" />
          <div className="skeleton h-4 w-40" />
        </div>
      </div>
      {[1, 2].map((i) => (
        <div key={i} className="card p-6 mb-4">
          <div className="flex items-center gap-2.5 mb-4">
            <div className="skeleton w-8 h-8 rounded-full" />
            <div className="skeleton h-4 w-44" />
          </div>
          <div className="skeleton h-4 w-full mb-2" />
          <div className="skeleton h-4 w-[95%] mb-2" />
          <div className="skeleton h-4 w-[75%] mb-4" />
          <div className="skeleton h-24 w-full rounded-lg" />
        </div>
      ))}
    </div>
  )
}
