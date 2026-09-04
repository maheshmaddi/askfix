import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Shield, LayoutDashboard, Users, Tags as TagsIcon, FileWarning, Mail, Loader2,
  Search, Trash2, Pencil, GitMerge, ShieldCheck, ShieldOff, ArrowBigUp, MessageSquareText, Plus,
} from 'lucide-react'
import {
  adminStats, adminUsers, adminToggleAdmin, adminTags, adminUpdateTag, adminMergeTag, adminDeleteTag,
  adminContent, getEmailSettings, saveEmailSettings, testEmailSettings, deleteQuestion, deleteAnswer, errorMessage,
} from '../lib/api'
import Avatar from '../components/common/Avatar'
import ConfirmDialog from '../components/common/ConfirmDialog'
import { useDebounce } from '../hooks/useDebounce'
import { timeAgo, compactNumber } from '../lib/format'
import { useAuth } from '../store/auth'

const TABS = [
  { key: 'overview', label: 'Overview', icon: LayoutDashboard },
  { key: 'users', label: 'Users', icon: Users },
  { key: 'tags', label: 'Tags', icon: TagsIcon },
  { key: 'content', label: 'Content', icon: FileWarning },
  { key: 'email', label: 'Email', icon: Mail },
]

export default function AdminPage() {
  const [tab, setTab] = useState('overview')
  return (
    <div>
      <div className="flex items-center gap-2.5 mb-1">
        <span className="w-9 h-9 rounded-xl bg-gradient-to-br from-brand-dark to-brand-violet text-white flex items-center justify-center">
          <Shield size={18} />
        </span>
        <h1 className="text-q-page">Admin panel</h1>
      </div>
      <p className="text-[14px] text-ink-soft mb-5">Site stats, user roles, tags, moderation and email settings.</p>

      <div className="flex items-center gap-1 mb-5 border-b border-line overflow-x-auto">
        {TABS.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            className={`flex items-center gap-1.5 px-3.5 pb-2.5 pt-1 text-[14px] font-semibold border-b-2 -mb-px transition-colors whitespace-nowrap ${
              tab === key ? 'text-brand border-brand' : 'text-ink-soft border-transparent hover:text-ink'
            }`}
          >
            <Icon size={15.5} /> {label}
          </button>
        ))}
      </div>

      {tab === 'overview' && <OverviewTab />}
      {tab === 'users' && <UsersTab />}
      {tab === 'tags' && <TagsTab />}
      {tab === 'content' && <ContentTab />}
      {tab === 'email' && <EmailTab />}
    </div>
  )
}

// ---- Overview -----------------------------------------------------------------------------

function StatCard({ label, value, accent }) {
  return (
    <div className="card p-4">
      <div className={`text-[24px] font-extrabold ${accent ? 'text-brand' : 'text-ink'}`}>{compactNumber(value)}</div>
      <div className="text-[12.5px] text-ink-soft mt-0.5">{label}</div>
    </div>
  )
}

function OverviewTab() {
  const { data, isLoading } = useQuery({ queryKey: ['admin-stats'], queryFn: adminStats })
  if (isLoading || !data) return <div className="skeleton h-64" />

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
        <StatCard label="Questions" value={data.stats.questions} />
        <StatCard label="Answers" value={data.stats.answers} accent />
        <StatCard label="Users" value={data.stats.users} />
        <StatCard label="Tags" value={data.stats.tags} />
        <StatCard label="Unanswered" value={data.stats.unanswered} accent={data.stats.unanswered > 0} />
      </div>

      <div className="grid sm:grid-cols-2 gap-4">
        <div className="card p-5">
          <h3 className="font-bold text-[15px] mb-3.5">Top contributors</h3>
          <ul className="space-y-3">
            {data.topContributors.map((c, i) => (
              <li key={c.id} className="flex items-center gap-2.5">
                <span className="text-[13px] font-extrabold text-brand/60 w-4">{i + 1}</span>
                <Avatar name={c.displayName} hue={c.avatarHue} size={28} />
                <div className="min-w-0 flex-1">
                  <Link to={`/profile/${c.id}`} className="text-[13.5px] font-semibold hover:underline truncate block">
                    {c.displayName}
                  </Link>
                  <span className="text-[11.5px] text-ink-faint">{c.answers} answers · {c.badge}</span>
                </div>
                <span className="chip bg-brand-50 text-brand font-bold">{compactNumber(c.reputation)}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="card p-5">
          <h3 className="font-bold text-[15px] mb-3.5">Needs an answer</h3>
          {data.oldestUnanswered.length === 0 ? (
            <p className="text-[13.5px] text-ink-faint">Every question has an answer 🎉</p>
          ) : (
            <ul className="space-y-2.5">
              {data.oldestUnanswered.map((q) => (
                <li key={q.id}>
                  <Link to={`/question/${q.id}`} className="text-[13.5px] font-semibold hover:text-brand line-clamp-2">
                    {q.title}
                  </Link>
                  <div className="text-[11.5px] text-ink-faint mt-0.5">
                    {q.authorName} · waiting {timeAgo(q.createdAt)}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <div className="card p-5">
        <h3 className="font-bold text-[15px] mb-3.5">Recent activity</h3>
        <ul className="divide-y divide-line/70">
          {data.recentActivity.map((a) => (
            <li key={`${a.type}-${a.id}`} className="py-2.5 flex items-center gap-3">
              {a.type === 'question'
                ? <MessageSquareText size={15} className="text-ink-faint shrink-0" />
                : <ArrowBigUp size={15} className="text-ink-faint shrink-0 rotate-180" />}
              <div className="min-w-0 flex-1 text-[13.5px]">
                <span className="font-semibold">{a.authorName}</span>
                <span className="text-ink-soft"> {a.type === 'question' ? 'asked' : 'answered'} · </span>
                <Link to={a.type === 'question' ? `/question/${a.id}` : `/question/${a.questionId ?? ''}`} className="font-medium hover:text-brand">
                  {a.title.length > 70 ? `${a.title.slice(0, 70)}…` : a.title}
                </Link>
              </div>
              <span className="text-[11.5px] text-ink-faint shrink-0">{timeAgo(a.createdAt)}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

// ---- Users --------------------------------------------------------------------------------

function UsersTab() {
  const queryClient = useQueryClient()
  const { user: me } = useAuth()
  const [query, setQuery] = useState('')
  const debounced = useDebounce(query, 350)
  const [confirming, setConfirming] = useState(null)

  const { data, isLoading } = useQuery({
    queryKey: ['admin-users', debounced],
    queryFn: () => adminUsers({ query: debounced }),
  })

  const toggle = useMutation({
    mutationFn: adminToggleAdmin,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-users'] }),
  })

  return (
    <div className="card p-5">
      <div className="relative mb-4">
        <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-ink-faint" />
        <input
          className="input pl-9"
          placeholder="Search by name, username or email…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
      </div>

      {isLoading ? (
        <div className="space-y-3">{[...Array(5)].map((_, i) => <div key={i} className="skeleton h-12" />)}</div>
      ) : (
        <div className="divide-y divide-line/70">
          {data?.items?.length === 0 && <p className="text-[13.5px] text-ink-faint py-4">No users match “{debounced}”.</p>}
          {data?.items?.map((u) => (
            <div key={u.id} className="py-3 flex items-center gap-3">
              <Avatar name={u.displayName} hue={u.avatarHue} size={36} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <Link to={`/profile/${u.id}`} className="text-[14px] font-semibold hover:underline">{u.displayName}</Link>
                  {u.isAdmin && <span className="chip bg-brand-50 text-brand gap-1"><Shield size={11} /> Admin</span>}
                </div>
                <div className="text-[12px] text-ink-faint truncate">
                  {u.email || u.sam} · {u.questionCount}Q / {u.answerCount}A · {compactNumber(u.reputation)} rep
                  {u.lastLoginAt ? ` · seen ${timeAgo(u.lastLoginAt)}` : ''}
                </div>
              </div>
              {u.id !== me?.id && (
                <button
                  className={`btn text-[12.5px] font-semibold px-3 h-8 gap-1.5 ${
                    u.isAdmin ? 'text-ink-soft border border-line hover:bg-brand-50 hover:text-brand' : 'text-brand bg-brand-50 hover:bg-brand-100'
                  }`}
                  onClick={() => setConfirming(u)}
                  disabled={toggle.isPending}
                >
                  {u.isAdmin ? <><ShieldOff size={13.5} /> Revoke admin</> : <><ShieldCheck size={13.5} /> Make admin</>}
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      <p className="text-[12px] text-ink-faint mt-4">
        Role changes apply the next time the user signs in (their session cookie keeps the old role until then).
      </p>

      <ConfirmDialog
        open={!!confirming}
        title={confirming?.isAdmin ? `Revoke admin — ${confirming?.displayName}?` : `Make ${confirming?.displayName} an admin?`}
        message={confirming?.isAdmin
          ? 'They will lose access to the admin panel on their next sign-in.'
          : 'They will be able to manage users, tags, content and email settings.'}
        confirmLabel={confirming?.isAdmin ? 'Revoke' : 'Promote'}
        onConfirm={() => toggle.mutate(confirming.id)}
        onClose={() => setConfirming(null)}
      />
    </div>
  )
}

// ---- Tags ---------------------------------------------------------------------------------

function TagsTab() {
  const queryClient = useQueryClient()
  const { data: tags = [], isLoading } = useQuery({ queryKey: ['admin-tags'], queryFn: adminTags })
  const [editing, setEditing] = useState(null) // { id, name, color, description }
  const [merging, setMerging] = useState(null) // tag
  const [mergeTarget, setMergeTarget] = useState('')
  const [deleting, setDeleting] = useState(null)
  const [error, setError] = useState('')

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['admin-tags'] })
    queryClient.invalidateQueries({ queryKey: ['all-tags'] })
    queryClient.invalidateQueries({ queryKey: ['popular-tags'] })
  }

  const save = useMutation({
    mutationFn: () => adminUpdateTag(editing.id, { name: editing.name, color: editing.color, description: editing.description }),
    onSuccess: () => { setEditing(null); invalidate() },
    onError: (e) => setError(errorMessage(e)),
  })
  const merge = useMutation({
    mutationFn: () => adminMergeTag(merging.id, Number(mergeTarget)),
    onSuccess: () => { setMerging(null); setMergeTarget(''); invalidate() },
    onError: (e) => setError(errorMessage(e)),
  })
  const remove = useMutation({
    mutationFn: () => adminDeleteTag(deleting.id),
    onSuccess: () => { setDeleting(null); invalidate() },
    onError: (e) => setError(errorMessage(e)),
  })

  return (
    <div className="card p-5">
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-bold text-[15px]">{tags.length} tags</h3>
        <p className="text-[12px] text-ink-faint">Edit, merge duplicates, or remove unused tags.</p>
      </div>
      {error && <div className="mb-3 text-[13px] text-brand bg-brand-50 rounded-lg px-3 py-2">{error}</div>}

      {isLoading ? (
        <div className="space-y-3">{[...Array(6)].map((_, i) => <div key={i} className="skeleton h-12" />)}</div>
      ) : (
        <div className="divide-y divide-line/70">
          {tags.map((t) => (
            <div key={t.id} className="py-3 flex items-center gap-3">
              <input
                type="color"
                value={t.color}
                disabled={editing?.id !== t.id}
                onChange={(e) => setEditing({ ...editing, color: e.target.value })}
                className="w-8 h-8 rounded-lg border border-line cursor-pointer disabled:cursor-default bg-surface shrink-0"
                aria-label={`${t.name} color`}
              />
              <div className="min-w-0 flex-1">
                {editing?.id === t.id ? (
                  <div className="flex flex-wrap gap-2">
                    <input
                      className="input !py-1.5 !text-[13.5px] w-36"
                      value={editing.name}
                      maxLength={30}
                      onChange={(e) => setEditing({ ...editing, name: e.target.value })}
                    />
                    <input
                      className="input !py-1.5 !text-[13.5px] flex-1 min-w-[160px]"
                      placeholder="Description (optional)"
                      value={editing.description ?? ''}
                      maxLength={200}
                      onChange={(e) => setEditing({ ...editing, description: e.target.value })}
                    />
                  </div>
                ) : (
                  <>
                    <Link to={`/tag/${t.slug}`} className="text-[14px] font-semibold hover:underline">{t.name}</Link>
                    <div className="text-[12px] text-ink-faint truncate">
                      {t.questionCount} questions{t.description ? ` · ${t.description}` : ''}
                    </div>
                  </>
                )}
              </div>
              {editing?.id === t.id ? (
                <div className="flex gap-1.5">
                  <button className="btn-secondary !h-8 !text-[12.5px]" onClick={() => setEditing(null)}>Cancel</button>
                  <button className="btn-primary !h-8 !text-[12.5px]" onClick={() => save.mutate()} disabled={save.isPending}>
                    {save.isPending && <Loader2 size={13} className="animate-spin" />} Save
                  </button>
                </div>
              ) : (
                <div className="flex gap-1">
                  <button className="btn-ghost !p-2" title="Edit tag" onClick={() => setEditing({ id: t.id, name: t.name, color: t.color, description: t.description ?? '' })}>
                    <Pencil size={14.5} />
                  </button>
                  <button className="btn-ghost !p-2" title="Merge into another tag" onClick={() => { setMerging(t); setMergeTarget(''); setError('') }}>
                    <GitMerge size={15} />
                  </button>
                  <button
                    className="btn-ghost !p-2 hover:!text-brand disabled:opacity-30"
                    title={t.questionCount > 0 ? 'Only tags without questions can be deleted — merge instead' : 'Delete tag'}
                    disabled={t.questionCount > 0}
                    onClick={() => setDeleting(t)}
                  >
                    <Trash2 size={14.5} />
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!deleting}
        title={`Delete tag “${deleting?.name}”?`}
        message="The tag has no questions attached and will be removed permanently."
        onConfirm={() => remove.mutate()}
        onClose={() => setDeleting(null)}
      />

      {merging && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4" onClick={() => setMerging(null)}>
          <div className="card pop-in w-full max-w-sm p-6" onClick={(e) => e.stopPropagation()}>
            <h3 className="font-bold text-[16px] mb-1.5">Merge “{merging.name}”</h3>
            <p className="text-[13.5px] text-ink-soft mb-4">
              All {merging.questionCount} questions with this tag move to the target, then “{merging.name}” is deleted.
            </p>
            <select className="input mb-4" value={mergeTarget} onChange={(e) => setMergeTarget(e.target.value)}>
              <option value="">Choose target tag…</option>
              {tags.filter((t) => t.id !== merging.id).map((t) => (
                <option key={t.id} value={t.id}>{t.name} ({t.questionCount})</option>
              ))}
            </select>
            <div className="flex justify-end gap-2.5">
              <button className="btn-secondary" onClick={() => setMerging(null)}>Cancel</button>
              <button className="btn-primary" disabled={!mergeTarget || merge.isPending} onClick={() => merge.mutate()}>
                {merge.isPending && <Loader2 size={14} className="animate-spin" />} Merge
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ---- Content ------------------------------------------------------------------------------

function ContentTab() {
  const queryClient = useQueryClient()
  const [type, setType] = useState('question')
  const [query, setQuery] = useState('')
  const debounced = useDebounce(query, 350)
  const [page, setPage] = useState(1)
  const [deleting, setDeleting] = useState(null)

  const { data, isLoading } = useQuery({
    queryKey: ['admin-content', type, debounced, page],
    queryFn: () => adminContent({ type, query: debounced, page }),
  })

  const remove = useMutation({
    mutationFn: () => (type === 'question' ? deleteQuestion(deleting.id) : deleteAnswer(deleting.id)),
    onSuccess: () => {
      setDeleting(null)
      queryClient.invalidateQueries({ queryKey: ['admin-content'] })
      queryClient.invalidateQueries({ queryKey: ['admin-stats'] })
    },
  })

  return (
    <div className="card p-5">
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="flex rounded-full bg-ink/[0.05] p-0.5">
          {['question', 'answer'].map((t) => (
            <button
              key={t}
              onClick={() => { setType(t); setPage(1) }}
              className={`px-3.5 py-1.5 rounded-full text-[13px] font-semibold capitalize transition-colors ${
                type === t ? 'bg-surface text-ink shadow-sm' : 'text-ink-soft'
              }`}
            >
              {t}s
            </button>
          ))}
        </div>
        <div className="relative flex-1 min-w-[180px]">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-ink-faint" />
          <input className="input pl-9 !py-2" placeholder={`Search ${type}s…`} value={query} onChange={(e) => { setQuery(e.target.value); setPage(1) }} />
        </div>
        {data && <span className="text-[12.5px] text-ink-faint">{data.total} total</span>}
      </div>

      {isLoading ? (
        <div className="space-y-3">{[...Array(5)].map((_, i) => <div key={i} className="skeleton h-14" />)}</div>
      ) : (
        <div className="divide-y divide-line/70">
          {data?.items?.length === 0 && <p className="text-[13.5px] text-ink-faint py-4">Nothing matches.</p>}
          {data?.items?.map((row) => (
            <div key={row.id} className="py-3 flex items-center gap-3 group">
              <div className="min-w-0 flex-1">
                <Link
                  to={type === 'question' ? `/question/${row.id}` : `/question/${row.questionId}`}
                  className="text-[14px] font-semibold hover:text-brand line-clamp-1"
                >
                  {row.title}
                </Link>
                <div className="text-[12px] text-ink-faint truncate">
                  {row.authorName} · {row.score} {type === 'question' ? 'answers' : 'upvotes'} · {timeAgo(row.createdAt)}
                  {row.excerpt ? ` · ${row.excerpt.slice(0, 80)}` : ''}
                </div>
              </div>
              <button
                className="btn-ghost !p-2 opacity-0 group-hover:opacity-100 hover:!text-brand transition-opacity"
                title={`Delete ${type}`}
                onClick={() => setDeleting(row)}
              >
                <Trash2 size={15} />
              </button>
            </div>
          ))}
        </div>
      )}

      {data?.totalPages > 1 && (
        <div className="flex items-center justify-center gap-3 mt-4 text-[13px]">
          <button className="btn-secondary !h-8 !text-[12.5px]" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
          <span className="text-ink-faint">Page {data.page} of {data.totalPages}</span>
          <button className="btn-secondary !h-8 !text-[12.5px]" disabled={!data.hasMore} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>
      )}

      <ConfirmDialog
        open={!!deleting}
        title={`Delete this ${type}?`}
        message={type === 'question'
          ? 'The question and all its answers, comments and votes will be permanently removed.'
          : 'The answer, its comments and votes will be permanently removed.'}
        onConfirm={() => remove.mutate()}
        onClose={() => setDeleting(null)}
      />
    </div>
  )
}

// ---- Email --------------------------------------------------------------------------------

function EmailTab() {
  const queryClient = useQueryClient()
  const { data, isLoading } = useQuery({ queryKey: ['admin-email'], queryFn: getEmailSettings })
  const [form, setForm] = useState(null)
  const [error, setError] = useState('')
  const [testResult, setTestResult] = useState(null)

  // hydrate the form once settings arrive
  const [hydratedFor, setHydratedFor] = useState(null)
  if (data && hydratedFor !== data) {
    setHydratedFor(data)
    setForm({
      enabled: data.enabled, host: data.host, port: data.port || 25, username: data.username || '',
      password: '', useSsl: data.useSsl, fromAddress: data.fromAddress || 'askfix@localhost',
      fromName: data.fromName || 'AskFix', baseUrl: data.baseUrl || window.location.origin,
    })
  }

  const save = useMutation({
    mutationFn: () => saveEmailSettings(form),
    onSuccess: () => {
      setError('')
      queryClient.invalidateQueries({ queryKey: ['admin-email'] })
    },
    onError: (e) => setError(errorMessage(e)),
  })
  const test = useMutation({
    mutationFn: testEmailSettings,
    onSuccess: (r) => setTestResult({ ok: true, text: `Test email sent to ${r.to}.` }),
    onError: (e) => setTestResult({ ok: false, text: errorMessage(e) }),
  })

  if (isLoading || !form) return <div className="skeleton h-64" />
  const set = (k) => (e) => setForm({ ...form, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value })

  return (
    <div className="card p-6 max-w-2xl">
      <div className="flex items-start justify-between gap-4 mb-5">
        <div>
          <h3 className="font-bold text-[15px]">Email notifications</h3>
          <p className="text-[13px] text-ink-soft mt-0.5">
            Users get email when someone answers their question, comments on their answer, or their answer is marked as the fix — according to their Settings page.
          </p>
        </div>
        <label className="flex items-center gap-2 cursor-pointer shrink-0 pt-1">
          <input type="checkbox" checked={form.enabled} onChange={set('enabled')} className="accent-[#5457D6] w-4 h-4" />
          <span className="text-[13.5px] font-semibold">Enabled</span>
        </label>
      </div>

      <div className="grid sm:grid-cols-2 gap-4">
        <div className="sm:col-span-2">
          <label className="block text-[13px] font-semibold mb-1.5">SMTP host</label>
          <input className="input" placeholder="smtp.corp.example" value={form.host} onChange={set('host')} />
        </div>
        <div>
          <label className="block text-[13px] font-semibold mb-1.5">Port</label>
          <input className="input" type="number" min={1} max={65535} value={form.port} onChange={set('port')} />
        </div>
        <div>
          <label className="block text-[13px] font-semibold mb-1.5">Security</label>
          <label className="flex items-center gap-2 h-[42px] px-3 rounded-lg border border-line text-[13.5px] cursor-pointer">
            <input type="checkbox" checked={form.useSsl} onChange={set('useSsl')} className="accent-[#5457D6] w-4 h-4" />
            Use SSL/TLS (port 587/465)
          </label>
        </div>
        <div>
          <label className="block text-[13px] font-semibold mb-1.5">Username <span className="text-ink-faint font-normal">(optional)</span></label>
          <input className="input" autoComplete="off" value={form.username} onChange={set('username')} />
        </div>
        <div>
          <label className="block text-[13px] font-semibold mb-1.5">
            Password {data.hasPassword && <span className="text-ink-faint font-normal">(saved — leave empty to keep)</span>}
          </label>
          <input className="input" type="password" autoComplete="new-password" placeholder={data.hasPassword ? '••••••••' : ''} value={form.password} onChange={set('password')} />
        </div>
        <div>
          <label className="block text-[13px] font-semibold mb-1.5">From address</label>
          <input className="input" placeholder="askfix@corp.example" value={form.fromAddress} onChange={set('fromAddress')} />
        </div>
        <div>
          <label className="block text-[13px] font-semibold mb-1.5">From name</label>
          <input className="input" value={form.fromName} onChange={set('fromName')} />
        </div>
        <div className="sm:col-span-2">
          <label className="block text-[13px] font-semibold mb-1.5">Site URL for email links</label>
          <input className="input" placeholder="http://askfix.corp.example:8080" value={form.baseUrl} onChange={set('baseUrl')} />
          <p className="text-[12px] text-ink-faint mt-1.5">Used to build question links inside the emails.</p>
        </div>
      </div>

      {error && <div className="mt-4 text-[13.5px] text-brand-dark bg-brand-50 border border-brand/25 rounded-lg px-3.5 py-2.5">{error}</div>}
      {testResult && (
        <div className={`mt-4 text-[13.5px] rounded-lg px-3.5 py-2.5 border ${
          testResult.ok ? 'text-emerald-700 bg-emerald-50 border-emerald-200 dark:bg-emerald-500/10 dark:border-emerald-500/30 dark:text-emerald-300' : 'text-brand-dark bg-brand-50 border-brand/25'
        }`}>
          {testResult.text}
        </div>
      )}

      <div className="flex items-center gap-2.5 mt-6 pt-5 border-t border-line/70">
        <button className="btn-primary" onClick={() => save.mutate()} disabled={save.isPending}>
          {save.isPending && <Loader2 size={15} className="animate-spin" />} Save settings
        </button>
        <button className="btn-secondary" onClick={() => test.mutate()} disabled={test.isPending || !data.enabled}>
          {test.isPending ? <Loader2 size={15} className="animate-spin" /> : <Mail size={15} />} Send test email
        </button>
        <span className="text-[12px] text-ink-faint ml-auto">The SMTP password is encrypted on the server.</span>
      </div>
    </div>
  )
}
