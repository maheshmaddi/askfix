import { Link } from 'react-router-dom'

export default function TagChip({ tag, size = 'md', interactive = true }) {
  const cls =
    size === 'sm'
      ? 'text-[11.5px] px-2 py-[2px] gap-1'
      : 'text-[12.5px] px-2.5 py-[3px] gap-1.5'
  const body = (
    <>
      <span className="w-[7px] h-[7px] rounded-full shrink-0" style={{ backgroundColor: tag.color }} />
      {tag.name}
    </>
  )
  const base = `chip ${cls} border border-line bg-white hover:border-ink/25 text-ink`
  if (!interactive) return <span className={`${base} cursor-default`}>{body}</span>
  return (
    <Link to={`/tag/${tag.slug}`} className={`${base} transition-colors`} onClick={(e) => e.stopPropagation()}>
      {body}
    </Link>
  )
}
