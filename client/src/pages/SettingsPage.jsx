import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Bell, Loader2, BellRing, CheckCircle2, Info, Sun, Moon, Monitor } from 'lucide-react'
import { getNotifPrefs, saveNotifPrefs } from '../lib/api'
import * as desktop from '../lib/desktopNotifications'
import * as theme from '../lib/theme'
import { useAuth } from '../store/auth'

function Toggle({ checked, onChange, disabled = false, label, hint }) {
  return (
    <label className={`flex items-start gap-3 py-3 ${disabled ? 'opacity-60' : 'cursor-pointer'}`}>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => onChange(!checked)}
        className={`w-[38px] h-[22px] rounded-full transition-colors shrink-0 mt-0.5 relative ${
          checked ? 'bg-brand' : 'bg-ink/20'
        }`}
      >
        <span className={`absolute top-[2px] w-[18px] h-[18px] rounded-full bg-surface shadow transition-all ${checked ? 'left-[18px]' : 'left-[2px]'}`} />
      </button>
      <span className="min-w-0">
        <span className="block text-[14px] font-semibold leading-tight">{label}</span>
        {hint && <span className="block text-[12.5px] text-ink-soft mt-0.5 leading-snug">{hint}</span>}
      </span>
    </label>
  )
}

function EmailSection() {
  const queryClient = useQueryClient()
  const { data, isLoading } = useQuery({ queryKey: ['notif-prefs'], queryFn: getNotifPrefs })
  const [prefs, setPrefs] = useState(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (data && !prefs) setPrefs(data)
  }, [data, prefs])

  const save = useMutation({
    mutationFn: (p) => saveNotifPrefs({
      emailOnAnswer: p.emailOnAnswer,
      emailOnComment: p.emailOnComment,
      emailOnAccepted: p.emailOnAccepted,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notif-prefs'] })
      setSaved(true)
      setTimeout(() => setSaved(false), 1800)
    },
  })

  if (isLoading || !prefs) return <div className="skeleton h-48" />

  const change = (key) => (on) => {
    const next = { ...prefs, [key]: on }
    setPrefs(next)
    save.mutate(next)
  }

  return (
    <div className="card p-5">
      <div className="flex items-center gap-2 mb-1">
        <svg viewBox="0 0 24 24" className="w-4 h-4 text-brand" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
          <rect x="2" y="4" width="20" height="16" rx="2" /><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
        </svg>
        <h3 className="font-bold text-[15px]">Email notifications</h3>
        <span className="ml-auto flex items-center gap-1.5">
          {save.isPending && <Loader2 size={14} className="animate-spin text-ink-faint" />}
          {saved && <span className="text-[12.5px] font-semibold text-emerald-600 dark:text-emerald-400 flex items-center gap-1"><CheckCircle2 size={13} /> Saved</span>}
        </span>
      </div>
      <p className="text-[13px] text-ink-soft mb-2">
        Sent to <span className="font-semibold">{data.email || 'your account email (not set)'}</span>.
      </p>

      <div className="divide-y divide-line/70">
        <Toggle
          label="Someone answers my question (or one I follow)"
          checked={prefs.emailOnAnswer}
          onChange={change('emailOnAnswer')}
          hint="New answers as they arrive."
        />
        <Toggle
          label="Someone comments on my answer"
          checked={prefs.emailOnComment}
          onChange={change('emailOnComment')}
          hint="Replies and follow-ups on answers you wrote."
        />
        <Toggle
          label="My answer is marked as the fix"
          checked={prefs.emailOnAccepted}
          onChange={change('emailOnAccepted')}
          hint="When the asker marks your answer with “This worked”."
        />
      </div>

      <p className="text-[12px] text-ink-faint mt-3">
        Upvotes and follows never send email — find them under the bell icon.
      </p>
    </div>
  )
}

function BrowserSection() {
  const [perm, setPerm] = useState(desktop.permission())
  const [enabled, setEnabled] = useState(desktop.isEnabled())
  const supported = desktop.isSupported()

  const toggle = async (on) => {
    if (on && perm !== 'granted') {
      const result = await desktop.requestPermission()
      setPerm(desktop.permission())
      if (result !== 'granted') return
    }
    desktop.setEnabled(on)
    setEnabled(on && desktop.permission() === 'granted')
  }

  const test = () => desktop.showDesktop('AskFix', 'Desktop notifications are working! 🎉', '/')

  return (
    <div className="card p-5">
      <div className="flex items-center gap-2 mb-1">
        <BellRing size={16} className="text-brand" />
        <h3 className="font-bold text-[15px]">Browser notifications</h3>
        {enabled && <span className="ml-auto chip bg-emerald-50 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-300 font-bold">Active</span>}
      </div>
      <p className="text-[13px] text-ink-soft mb-2">
        A desktop popup while an AskFix tab is open — quick updates without watching the site.
      </p>

      <div className="divide-y divide-line/70">
        <Toggle
          label="Show desktop notifications"
          checked={enabled}
          disabled={!supported}
          onChange={toggle}
          hint={supported ? 'Your browser will ask for permission the first time.' : undefined}
        />
      </div>

      {(!supported || perm === 'denied') && (
        <div className="flex gap-2.5 text-[13px] text-ink-soft bg-ink/[0.03] rounded-lg px-3.5 py-3 mt-3">
          <Info size={16} className="shrink-0 mt-0.5" />
          <span>
            {!supported
              ? <>This browser doesn't support notifications here — desktop notifications need <strong>HTTPS or localhost</strong>. Everything else in AskFix keeps working.</>
              : <>Notifications are blocked for this site in your browser settings. Unblock AskFix there, then enable it again.</>}
          </span>
        </div>
      )}

      <div className="flex justify-end mt-3">
        <button className="btn-secondary" onClick={test} disabled={!enabled}>
          <Bell size={14.5} /> Send test notification
        </button>
      </div>
    </div>
  )
}

function AppearanceSection() {
  const [current, setCurrent] = useState(theme.effectiveTheme())

  const choose = (t) => {
    theme.setTheme(t)
    setCurrent(t)
  }

  const options = [
    { key: 'system', label: 'System', icon: Monitor, hint: 'Follows your PC' },
    { key: 'light', label: 'Light', icon: Sun },
    { key: 'dark', label: 'Dark', icon: Moon },
  ]

  return (
    <div className="card p-5">
      <div className="flex items-center gap-2 mb-1">
        {current === 'dark' ? <Moon size={16} className="text-brand" /> : <Sun size={16} className="text-brand" />}
        <h3 className="font-bold text-[15px]">Appearance</h3>
      </div>
      <p className="text-[13px] text-ink-soft mb-3.5">How AskFix looks on this device.</p>
      <div className="grid grid-cols-3 gap-2.5">
        {options.map(({ key, label, icon: Icon, hint }) => (
          <button
            key={key}
            onClick={() => choose(key)}
            className={`flex flex-col items-center gap-1.5 rounded-xl border px-3 py-3.5 transition-colors ${
              current === key
                ? 'border-brand bg-brand-50 text-brand'
                : 'border-line text-ink-soft hover:border-ink/25 hover:text-ink'
            }`}
          >
            <Icon size={18} />
            <span className="text-[13.5px] font-semibold">{label}</span>
            {hint && <span className="text-[11px] text-ink-faint">{hint}</span>}
          </button>
        ))}
      </div>
    </div>
  )
}

export default function SettingsPage() {
  const { user } = useAuth()
  return (
    <div className="max-w-[640px]">
      <h1 className="text-q-page mb-1">Settings</h1>
      <p className="text-[14px] text-ink-soft mb-6">Notification preferences for {user?.displayName}.</p>
      <div className="space-y-4">
        <AppearanceSection />
        <EmailSection />
        <BrowserSection />
      </div>
    </div>
  )
}
