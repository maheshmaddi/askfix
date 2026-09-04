export default function EmptyState({ icon: Icon, title, subtitle, action, compact = false }) {
  return (
    <div className={`card flex flex-col items-center text-center ${compact ? 'px-6 py-10' : 'px-6 py-16'}`}>
      {Icon && (
        <div className="w-14 h-14 rounded-2xl bg-brand-50 text-brand flex items-center justify-center mb-4">
          <Icon size={26} strokeWidth={1.8} />
        </div>
      )}
      <h3 className="text-[16.5px] font-bold text-ink mb-1.5">{title}</h3>
      {subtitle && <p className="text-[14px] text-ink-soft max-w-sm leading-relaxed">{subtitle}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}
