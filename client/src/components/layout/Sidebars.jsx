import { NavLink, Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Home, Tags, Bookmark, Bell, PenSquare, Flame, TrendingUp, Shield } from 'lucide-react'
import { useAuth } from '../../store/auth'
import { getStats, getFeed, getTags } from '../../lib/api'
import { compactNumber, plural } from '../../lib/format'
import TagChip from '../common/TagChip'

const navItem = 'flex items-center gap-3 px-3 py-2 rounded-lg text-[14.5px] font-medium transition-colors'
const navIdle = 'text-ink-soft hover:bg-ink/[0.05] hover:text-ink'
const navActive = ({ isActive }) =>
  `${navItem} ${isActive ? 'bg-brand-50 text-brand' : navIdle}`

export function LeftSidebar() {
  const { user, isAdmin } = useAuth()
  return (
    <aside className="hidden lg:block w-[210px] shrink-0">
      <nav className="sticky top-[73px] flex flex-col gap-1">
        <NavLink to="/" end className={navActive}>
          <Home size={19} strokeWidth={2.1} /> Home
        </NavLink>
        <NavLink to="/tags" className={navActive}>
          <Tags size={19} strokeWidth={2.1} /> Tags
        </NavLink>
        <NavLink to="/bookmarks" className={navActive}>
          <Bookmark size={19} strokeWidth={2.1} /> Bookmarks
        </NavLink>
        <NavLink to="/notifications" className={navActive}>
          <Bell size={19} strokeWidth={2.1} /> Notifications
        </NavLink>
        <NavLink to={`/profile/${user?.id ?? ''}`} className={navActive}>
          <span className="w-[19px] h-[19px] rounded-full bg-ink-soft/30 inline-flex items-center justify-center text-[10px] font-bold">
            {user?.displayName?.[0] ?? ''}
          </span>
          Profile
        </NavLink>
        {isAdmin && (
          <NavLink to="/admin" className={navActive}>
            <Shield size={19} strokeWidth={2.1} /> Admin
          </NavLink>
        )}
        <Link to="/ask" className="btn-primary w-full mt-3">
          <PenSquare size={15} /> Ask question
        </Link>
      </nav>
    </aside>
  )
}

export function MobileTabBar() {
  return (
    <nav className="lg:hidden fixed bottom-0 inset-x-0 z-40 bg-white border-t border-line flex">
      {[
        { to: '/', icon: Home, label: 'Home', end: true },
        { to: '/tags', icon: Tags, label: 'Tags' },
        { to: '/ask', icon: PenSquare, label: 'Ask' },
        { to: '/notifications', icon: Bell, label: 'Alerts' },
        { to: '/bookmarks', icon: Bookmark, label: 'Saved' },
      ].map(({ to, icon: Icon, label, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          className={({ isActive }) =>
            `flex-1 flex flex-col items-center gap-0.5 py-2 text-[10.5px] font-semibold ${
              isActive ? 'text-brand' : 'text-ink-faint'
            }`
          }
        >
          <Icon size={20} strokeWidth={2.1} />
          {label}
        </NavLink>
      ))}
    </nav>
  )
}

function SidebarCard({ title, icon: Icon, children, moreLink, moreLabel = 'See all' }) {
  return (
    <div className="card p-4 mb-4">
      <div className="flex items-center gap-2 mb-3">
        {Icon && <Icon size={15} className="text-brand" />}
        <h3 className="font-bold text-[14.5px]">{title}</h3>
      </div>
      {children}
      {moreLink && (
        <Link to={moreLink} className="inline-block mt-3 text-[13px] font-semibold text-brand hover:underline">
          {moreLabel} →
        </Link>
      )}
    </div>
  )
}

export function RightSidebar() {
  const { id: questionId } = useParams()
  const { data: stats } = useQuery({ queryKey: ['stats'], queryFn: getStats, staleTime: 60_000 })
  const { data: trending } = useQuery({
    queryKey: ['trending'],
    queryFn: () => getFeed({ tab: 'trending', pageSize: 5 }),
    staleTime: 60_000,
  })
  const { data: tags } = useQuery({ queryKey: ['popular-tags'], queryFn: () => getTags('popular', 8), staleTime: 60_000 })
  const { data: related } = useQuery({
    queryKey: ['related', questionId],
    queryFn: () => relatedQuestions(questionId),
    enabled: questionId != null,
    staleTime: 60_000,
  })

  return (
    <aside className="hidden xl:block w-[300px] shrink-0">
      <div className="sticky top-[73px]">
        {related && related.length > 0 && (
          <SidebarCard title="Related questions" icon={TrendingUp}>
            <ul className="space-y-2.5">
              {related.map((q) => (
                <li key={q.id}>
                  <Link to={`/question/${q.id}`} className="text-[13.5px] font-semibold leading-snug hover:text-brand line-clamp-2">
                    {q.title}
                  </Link>
                  <div className="text-[11.5px] text-ink-faint mt-0.5">{plural(q.answerCount, 'answer')}</div>
                </li>
              ))}
            </ul>
          </SidebarCard>
        )}

        <SidebarCard title="Trending this month" icon={Flame}>
          <ol className="space-y-2.5">
            {(trending?.items ?? []).map((q, i) => (
              <li key={q.id} className="flex gap-2.5">
                <span className="text-[13px] font-extrabold text-brand/70 w-4 shrink-0">{i + 1}</span>
                <Link to={`/question/${q.id}`} className="text-[13.5px] font-semibold leading-snug hover:text-brand line-clamp-2">
                  {q.title}
                </Link>
              </li>
            ))}
            {trending?.items?.length === 0 && <li className="text-[13px] text-ink-faint">Nothing trending yet.</li>}
          </ol>
          <Link to="/?tab=trending" className="inline-block mt-3 text-[13px] font-semibold text-brand hover:underline">
            See all →
          </Link>
        </SidebarCard>

        <SidebarCard title="Popular tags" icon={Tags} moreLink="/tags">
          <div className="flex flex-wrap gap-1.5">
            {(tags ?? []).map((t) => (
              <TagChip key={t.id} tag={t} size="sm" />
            ))}
          </div>
        </SidebarCard>

        {stats && (
          <div className="card px-4 py-3.5 text-[12px] text-ink-faint leading-relaxed">
            <span className="font-semibold text-ink">{compactNumber(stats.questions)}</span> questions ·{' '}
            <span className="font-semibold text-ink">{compactNumber(stats.answers)}</span> answers ·{' '}
            <span className="font-semibold text-ink">{compactNumber(stats.users)}</span> people
            <br />
            <span className="font-semibold text-ink">{compactNumber(stats.unanswered)}</span> questions still need an answer —{' '}
            <Link to="/?tab=unanswered" className="text-brand font-semibold hover:underline">
              help out
            </Link>
          </div>
        )}
      </div>
    </aside>
  )
}
