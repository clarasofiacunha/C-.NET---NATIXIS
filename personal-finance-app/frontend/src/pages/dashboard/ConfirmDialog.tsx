import { useEffect, useRef } from 'react'

// Confirmacao dentro da app (substitui window.confirm, que alguns browsers bloqueiam).
export default function ConfirmDialog({
  title,
  message,
  confirmLabel,
  busy = false,
  onConfirm,
  onCancel,
}: {
  title: string
  message: string
  confirmLabel: string
  busy?: boolean
  onConfirm: () => void
  onCancel: () => void
}) {
  const cancelButton = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    cancelButton.current?.focus()
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !busy) onCancel()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [busy, onCancel])

  return (
    <div className="account-modal confirm-dialog" role="presentation">
      <div className="account-modal__dialog confirm-dialog__box" role="alertdialog" aria-modal="true" aria-labelledby="confirm-dialog-title" aria-describedby="confirm-dialog-message">
        <div className="account-modal__header">
          <div>
            <h3 id="confirm-dialog-title">{title}</h3>
            <p id="confirm-dialog-message">{message}</p>
          </div>
        </div>
        <div className="account-modal__footer confirm-dialog__footer">
          <button ref={cancelButton} type="button" className="account-modal__cancel" onClick={onCancel} disabled={busy}>Cancel</button>
          <button type="button" className="submit account-form__submit confirm-dialog__danger" onClick={onConfirm} disabled={busy}>
            {busy ? 'Deleting...' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
