import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { KeyRound, Loader2, ShieldCheck, MessageSquareText, ArrowBigUp, Lock } from 'lucide-react'
import { apiInfo, login, errorMessage } from '../lib/api'
import { useAuth } from '../store/auth'

const DEMO_USERS = [
  { sam: 'corp\\mahesh', label: 'Mahesh · Developer' },
  { sam: 'corp\\priya.s', label: 'Priya · IT Support' },
  { sam: 'corp\\arjun.p', label: 'Arjun · QA' },
]

export default function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [busy, setBusy] = useState(false)
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const queryClient = useQueryClient()
  const { user, isLoading } = useAuth()
  const { data: appInfo } = useQuery({ queryKey: ['app-info'], queryFn: apiInfo, staleTime: Infinity })

  useEffect(() => {
    if (!isLoading && user) navigate('/', { replace: true })
  }, [user, isLoading, navigate])

  const submit = async (e) => {
    e.preventDefault()
    if (!username.trim() || !password) return
    setBusy(true)
    setError('')
    try {
      await login(username.trim(), password)
      await queryClient.invalidateQueries({ queryKey: ['me'] })
      navigate(params.get('next') || '/', { replace: true })
    } catch (err) {
      setError(errorMessage(err, 'Invalid username or password.'))
      setBusy(false)
    }
  }

  return (
    <div className="min-h-screen flex flex-col lg:flex-row bg-canvas">
      {/* brand panel */}
      <div className="lg:w-[46%] bg-gradient-to-br from-brand-dark via-brand to-brand-violet text-white px-8 py-12 lg:py-0 flex flex-col justify-center relative overflow-hidden">
        <div
          className="absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage: 'radial-gradient(circle at 1px 1px, #fff 1px, transparent 0)',
            backgroundSize: '26px 26px',
          }}
        />
        <div className="relative max-w-md mx-auto lg:mx-0 lg:ml-auto lg:mr-16">
          <span className="flex items-center gap-2.5 mb-8">
            <span className="w-11 h-11 rounded-xl bg-white flex items-center justify-center shadow-lg">              <svg viewBox="0 0 24 24" className="w-7 h-7" fill="#5457D6" aria-hidden="true">
                <path d="M11.2 5.2 16.8 18.4h-2.4l-1.2-3h-4.4l-1.2 3H5.2L10.8 5.2Zm-.5 7.9h2.6L12 9.7Z" />
                <circle cx="17.2" cy="6.6" r="1.55" />
              </svg>
            </span>
            <span className="text-2xl font-extrabold tracking-tight">AskFix</span>
          </span>
          <h1 className="text-[28px] lg:text-[34px] font-extrabold leading-[1.18] mb-4">
            Stuck on a tool?
            <br />
            Someone here has fixed it.
          </h1>
          <p className="text-white/85 text-[15.5px] leading-relaxed mb-8">
            The internal Q&amp;A for setup problems, build issues and tool quirks. Ask the whole company, get answers from
            people who hit the same wall — and upvote what actually worked.
          </p>
          <ul className="space-y-3.5 text-[14.5px]">
            <li className="flex items-center gap-3">
              <span className="w-8 h-8 rounded-lg bg-white/15 flex items-center justify-center shrink-0">
                <MessageSquareText size={16} />
              </span>
              Ask about any tool, project or setup issue
            </li>
            <li className="flex items-center gap-3">
              <span className="w-8 h-8 rounded-lg bg-white/15 flex items-center justify-center shrink-0">
                <ArrowBigUp size={17} />
              </span>
              Upvote the answers that actually fix it
            </li>
            <li className="flex items-center gap-3">
              <span className="w-8 h-8 rounded-lg bg-white/15 flex items-center justify-center shrink-0">
                <ShieldCheck size={16} />
              </span>
              Sign in with your Windows account — internal only
            </li>
          </ul>
        </div>
      </div>

      {/* form panel */}
      <div className="flex-1 flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm">
          <h2 className="text-[22px] font-extrabold text-ink">Welcome back</h2>
          <p className="text-[14.5px] text-ink-soft mt-1.5 mb-7">Sign in with your Windows / domain account.</p>

          {params.get('expired') === '1' && !error && (
            <div className="mb-4 text-[13.5px] text-amber-800 bg-amber-50 border border-amber-200 rounded-lg px-3.5 py-2.5">
              Your session expired. Please sign in again.
            </div>
          )}
          {error && (
            <div className="mb-4 text-[13.5px] text-brand-dark bg-brand-50 border border-brand/25 rounded-lg px-3.5 py-2.5" role="alert">
              {error}
            </div>
          )}

          <form onSubmit={submit} className="space-y-4">
            <div>
              <label htmlFor="username" className="block text-[13px] font-semibold text-ink mb-1.5">
                Domain username
              </label>
              <input
                id="username"
                className="input"
                placeholder="CORP\jdoe or jdoe@corp.example"
                autoComplete="username"
                autoFocus
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                disabled={busy}
              />
            </div>
            <div>
              <label htmlFor="password" className="block text-[13px] font-semibold text-ink mb-1.5">
                Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  className="input pr-11"
                  placeholder="Your Windows password"
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={busy}
                />
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-ink-faint hover:text-ink"
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  <Lock size={15} className={showPassword ? 'text-brand' : ''} />
                </button>
              </div>
            </div>
            <button className="btn-primary w-full h-11 text-[15px]" disabled={busy || !username.trim() || !password}>
              {busy ? <Loader2 size={17} className="animate-spin" /> : <KeyRound size={16} />}
              {busy ? 'Signing in…' : 'Sign in'}
            </button>
          </form>

          {appInfo?.devMode && (
            <div className="mt-6 rounded-xl border border-dashed border-line bg-white p-4">
              <div className="text-[12px] font-bold text-ink-soft uppercase tracking-wide mb-2">
                Dev mode — demo accounts (password: <code className="font-mono text-brand">AskFix!123</code>)
              </div>
              <div className="flex flex-wrap gap-1.5">
                {DEMO_USERS.map((u) => (
                  <button
                    key={u.sam}
                    type="button"
                    className="chip border border-line bg-white hover:border-brand/50 hover:text-brand transition-colors"
                    onClick={() => {
                      setUsername(u.sam)
                      setPassword('AskFix!123')
                    }}
                  >
                    {u.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          <p className="mt-8 text-[12.5px] text-ink-faint leading-relaxed">
            AskFix is for internal use. Your credentials are verified against the company domain and never stored.
          </p>
        </div>
      </div>
    </div>
  )
}
