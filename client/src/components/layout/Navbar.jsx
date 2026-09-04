import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, Bell, LogOut, Bookmark, UserRound, ChevronDown, MessageSquareText, Settings, Shield } from 'lucide-react'
import Avatar from '../common/Avatar'
import { useAuth } from '../../store/auth'
import { unreadCount, markAllRead, logout, getNotifications } from '../../lib/api'
import * as desktop from '../../lib/desktopNotifications'

const NOTIF_VERBS = {
  Answer: 'answered your question',
  Upvote: 'upvoted your answer',
  Comment: 'commented on your answer',
  Accepted: 'marked your answer as the fix',
  Follow: 'followed your question',
}

function describeNotification(n) {
  const verb = NOTIF_VERBS[n.type] ?? 'interacted with you'
  const title = n.questionTitle?.length > 46 ? `${n.questionTitle.slice(0, 46)}…` : n.questionTitle
  return `${n.actorName} ${verb}: ${title}`
}

function NotificationBell() {
  const [open, setOpen] = useState(false)
  const ref = useRef(null)
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { user } = useAuth()
  const { data: count = 0 } = useQuery({
    queryKey: ['unread-count'],
    queryFn: unreadCount,
    refetchInterval: 30_000,
  })

  // desktop notifications: on a rising unread count, surface the new items (max 3)
  useEffect(() => {
    if (!user?.id || !desktop.isEnabled() || count === 0) return
    let cancelled = false
    ;(async () => {
      try {
        const seenKey = `askfix_lastNotifId_${user.id}`
        const lastSeen = Number(localStorage.getItem(seenKey) ?? 0)
        const page = await getNotifications({ page: 1 })
        if (cancelled) return
        const fresh = page.items.filter((n) => n.id > lastSeen)
        const maxId = Math.max(lastSeen, ...page.items.map((n) => n.id))
        if (maxId > lastSeen) localStorage.setItem(seenKey, String(maxId))
        for (const n of fresh.slice(0, 3)) {
          desktop.showDesktop(
            'AskFix',
            describeNotification(n),
            `/question/${n.questionId}`,
          )
        }
      } catch {
        /* offline or logged out — ignore */
      }
    })()
    return () => { cancelled = true }
  }, [count, user?.id])

  useEffect(() => {
    const onClick = (e) => ref.current && !ref.current.contains(e.target) && setOpen(false)
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [])

  const markAll = useMutation({
    mutationFn: markAllRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['unread-count'] }),
  })

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        className="relative p-2 rounded-full text-ink-soft hover:bg-ink/[0.06] hover:text-ink transition-colors"
        onClick={() => setOpen((v) => !v)}
        aria-label={`Notifications${count ? `, ${count} unread` : ''}`}
      >
        <Bell size={21} />
        {count > 0 && (
          <span className="unread-dot absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-brand text-white text-[10.5px] font-bold flex items-center justify-center border-2 border-white">
            {count > 99 ? '99+' : count}
          </span>
        )}
      </button>
      {open && (
        <div className="pop-in absolute right-0 mt-2 w-80 card p-0 overflow-hidden z-50">
          <div className="flex items-center justify-between px-4 py-3 border-b border-line/80">
            <span className="font-bold text-[14.5px]">Notifications</span>
            {count > 0 && (
              <button className="text-[12.5px] text-brand font-semibold hover:underline" onClick={() => markAll.mutate()}>
                Mark all read
              </button>
            )}
          </div>
          <div className="max-h-80 overflow-y-auto py-1">
            <button
              className="w-full text-left px-4 py-3 hover:bg-ink/[0.03] text-[13.5px] text-ink-soft flex items-center gap-2"
              onClick={() => {
                setOpen(false)
                navigate('/notifications')
              }}
            >
              <MessageSquareText size={16} className="text-brand" />
              View all notifications
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

function UserMenu() {
  const [open, setOpen] = useState(false)
  const ref = useRef(null)
  const navigate = useNavigate()
  const { user, refresh, isAdmin } = useAuth()

  useEffect(() => {
    const onClick = (e) => ref.current && !ref.current.contains(e.target) && setOpen(false)
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [])

  const doLogout = async () => {
    try {
      await logout()
    } finally {
      refresh()
      location.href = '/login'
    }
  }

  return (
    <div className="relative" ref={ref}>
      <button type="button" className="flex items-center gap-1 p-0.5 rounded-full hover:ring-2 hover:ring-ink/10" onClick={() => setOpen((v) => !v)}>
        <Avatar name={user?.displayName} hue={user?.avatarHue} size={34} />
        <ChevronDown size={14} className="text-ink-faint" />
      </button>
      {open && (
        <div className="pop-in absolute right-0 mt-2 w-64 card p-0 overflow-hidden z-50">
          <div className="px-4 pt-4 pb-3 border-b border-line/80">
            <div className="font-bold text-[14.5px] truncate">{user?.displayName}</div>
            <div className="text-[12.5px] text-ink-faint truncate">{user?.email}</div>
            <div className="mt-2 inline-flex items-center gap-1.5 chip bg-brand-50 text-brand">
              {user?.badge} · {user?.reputation} rep
            </div>
          </div>
          <div className="py-1.5">
            <Link to={`/profile/${user?.id}`} className="flex items-center gap-2.5 px-4 py-2 text-[14px] hover:bg-ink/[0.04]" onClick={() => setOpen(false)}>
              <UserRound size={16} className="text-ink-soft" /> Profile
            </Link>
            <Link to="/bookmarks" className="flex items-center gap-2.5 px-4 py-2 text-[14px] hover:bg-ink/[0.04]" onClick={() => setOpen(false)}>
              <Bookmark size={16} className="text-ink-soft" /> Bookmarks
            </Link>
            <Link to="/settings" className="flex items-center gap-2.5 px-4 py-2 text-[14px] hover:bg-ink/[0.04]" onClick={() => setOpen(false)}>
              <Settings size={16} className="text-ink-soft" /> Settings
            </Link>
            {isAdmin && (
              <Link to="/admin" className="flex items-center gap-2.5 px-4 py-2 text-[14px] hover:bg-ink/[0.04] font-semibold text-brand" onClick={() => setOpen(false)}>
                <Shield size={16} /> Admin panel
              </Link>
            )}
            <button className="w-full flex items-center gap-2.5 px-4 py-2 text-[14px] hover:bg-ink/[0.04] text-left" onClick={doLogout}>
              <LogOut size={16} className="text-ink-soft" /> Sign out
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

export default function Navbar() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [q, setQ] = useState(searchParams.get('q') ?? '')
  const inputRef = useRef(null)

  useEffect(() => {
    const onKey = (e) => {
      if (e.key === '/' && document.activeElement?.tagName !== 'INPUT' && !document.activeElement?.isContentEditable) {
        e.preventDefault()
        inputRef.current?.focus()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  const submit = (e) => {
    e.preventDefault()
    if (q.trim()) navigate(`/search?q=${encodeURIComponent(q.trim())}`)
  }

  return (
    <header className="sticky top-0 z-40 bg-white/95 backdrop-blur border-b border-line">
      <div className="mx-auto max-w-[1280px] px-4 h-[57px] flex items-center gap-3">
        <Link to="/" className="flex items-center gap-2 shrink-0" aria-label="AskFix home">
          <span className="w-[30px] h-[30px] rounded-[9px] bg-gradient-to-br from-brand-dark to-brand-violet flex items-center justify-center shadow-sm">
            <svg viewBox="0 0 24 24" className="w-[18px] h-[18px]" fill="#fff" aria-hidden="true">
              <path d="M11.2 5.2 16.8 18.4h-2.4l-1.2-3h-4.4l-1.2 3H5.2L10.8 5.2Zm-.5 7.9h2.6L12 9.7Z" />
              <circle cx="17.2" cy="6.6" r="1.55" />
            </svg>
          </span>
          <span className="text-[19px] font-extrabold tracking-tight text-ink hidden sm:block">
            Ask<span className="text-brand">Fix</span>
          </span>
        </Link>

        <form onSubmit={submit} className="flex-1 max-w-[560px] mx-2 relative">
          <Search size={17} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-ink-faint pointer-events-none" />
          <input
            ref={inputRef}
            value={q}
            onChange={(e) => setQ(e.target.value)}
            className="w-full h-[38px] pl-10 pr-14 rounded-full bg-ink/[0.055] text-[14px] border border-transparent
                       focus:bg-white focus:border-brand/40 focus:ring-2 focus:ring-brand/15 outline-none transition-all
                       placeholder:text-ink-faint py-2"
            placeholder="Search questions, answers, tags…"
            aria-label="Search"
          />
          <kbd className="absolute right-3.5 top-1/2 -translate-y-1/2 hidden md:block text-[10.5px] text-ink-faint border border-line rounded px-1.5 py-0.5 bg-white">
            /
          </kbd>
        </form>

        <div className="flex items-center gap-1.5 ml-auto">
          <Link to="/ask" className="btn-primary hidden sm:inline-flex">
            Ask question
          </Link>
          <NotificationBell />
          <UserMenu />
        </div>
      </div>
    </header>
  )
}
