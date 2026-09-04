import { Link } from 'react-router-dom'
import { CircleHelp } from 'lucide-react'

export default function NotFoundPage() {
  return (
    <div className="card px-6 py-16 text-center">
      <div className="text-[64px] font-extrabold text-brand/15 leading-none select-none">404</div>
      <h1 className="text-[19px] font-extrabold mt-2 mb-2">This page took a sick day</h1>
      <p className="text-[14px] text-ink-soft mb-6">The page you're looking for doesn't exist or was removed.</p>
      <Link to="/" className="btn-primary">Back to home</Link>
    </div>
  )
}
