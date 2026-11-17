import { useState } from 'react'

// Lists a scan's annotations and lets the user pin a new one.
// Flow: click "Add annotation" → click a spot on the scan → type the note → Save.
export default function AnnotationPanel({ annotations, picking, pendingPoint, onStartPicking, onCancel, onSave, onDelete }) {
  const [text, setText] = useState('')
  const [category, setCategory] = useState('Note')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function handleSave(event) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      await onSave({ text, category, ...pendingPoint })
      setText('')
      setCategory('Note')
    } catch (e) {
      setError(e.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="annotations">
      <div className="row-between">
        <h3>Annotations ({annotations.length})</h3>
        {!picking && <button className="secondary" onClick={onStartPicking}>Add annotation</button>}
      </div>

      {picking && (
        <div className="picking-box">
          {!pendingPoint ? (
            <p>Click a spot on the scan to place the pin.</p>
          ) : (
            <form onSubmit={handleSave} className="form">
              <p className="muted small">
                Position: {pendingPoint.x}, {pendingPoint.y}, {pendingPoint.z}
              </p>
              <input required maxLength={500} autoFocus value={text}
                onChange={(e) => setText(e.target.value)} placeholder="e.g. Crack near the window frame" />
              <select value={category} onChange={(e) => setCategory(e.target.value)}>
                <option>Note</option>
                <option>Issue</option>
                <option>Progress</option>
              </select>
              {error && <p className="error">{error}</p>}
              <button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save annotation'}</button>
            </form>
          )}
          <button className="link" onClick={onCancel}>Cancel</button>
        </div>
      )}

      {annotations.length === 0 && !picking ? (
        <p className="muted small">No annotations on this scan yet.</p>
      ) : (
        <ol className="annotation-list">
          {annotations.map((a) => (
            <li key={a.id}>
              <span className={`tag tag-${a.category.toLowerCase()}`}>{a.category}</span>
              <span className="annotation-text">{a.text}</span>
              <button className="link danger" onClick={() => onDelete(a.id)} aria-label="Delete annotation">✕</button>
            </li>
          ))}
        </ol>
      )}
    </div>
  )
}
