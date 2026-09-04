import { useEffect } from 'react'
import { AlertTriangle } from 'lucide-react'

export default function ConfirmDialog({ open, title, message, confirmLabel = 'Delete', onConfirm, onClose }) {
  useEffect(() => {
    if (!open) return
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  if (!open) return null
  return (
    <div
      className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      onClick={onClose}
    >
      <div className="card pop-in w-full max-w-sm p-6" onClick={(e) => e.stopPropagation()}>
        <div className="w-11 h-11 rounded-full bg-brand-50 text-brand flex items-center justify-center mb-4">
          <AlertTriangle size={22} />
        </div>
        <h3 className="text-[16.5px] font-bold mb-1.5">{title}</h3>
        <p className="text-[14px] text-ink-soft leading-relaxed mb-5">{message}</p>
        <div className="flex justify-end gap-2.5">
          <button className="btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button
            className="btn bg-brand text-white hover:bg-brand-dark px-4 h-9 rounded-full text-[14px] font-semibold"
            onClick={() => {
              onConfirm()
              onClose()
            }}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
