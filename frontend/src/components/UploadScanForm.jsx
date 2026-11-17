import { useState } from 'react'
import { api } from '../api.js'

const today = () => new Date().toISOString().slice(0, 10)

// Uploads a .glb model or a .jpg/.png photo to a site.
export default function UploadScanForm({ siteId, onUploaded }) {
  const [file, setFile] = useState(null)
  const [title, setTitle] = useState('')
  const [capturedAt, setCapturedAt] = useState(today())
  const [notes, setNotes] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [formKey, setFormKey] = useState(0) // resets the file input after upload

  async function handleSubmit(event) {
    event.preventDefault()
    if (!file) return setError('Choose a file first.')
    if (file.size > 50 * 1024 * 1024) return setError('The file is larger than 50 MB.')

    setBusy(true)
    setError('')
    try {
      const scan = await api.uploadScan(siteId, { file, title, notes, capturedAt })
      setFile(null); setTitle(''); setNotes(''); setCapturedAt(today()); setFormKey((k) => k + 1)
      onUploaded(scan)
    } catch (e) {
      setError(e.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form key={formKey} onSubmit={handleSubmit} className="form">
      <label>
        Scan file (.glb, .jpg, .png · max 50 MB)
        <input type="file" accept=".glb,.jpg,.jpeg,.png" required
          onChange={(e) => {
            const chosen = e.target.files?.[0] ?? null
            setFile(chosen)
            if (chosen && !title) setTitle(chosen.name.replace(/\.[^.]+$/, ''))
          }} />
      </label>
      <label>
        Title
        <input required minLength={2} maxLength={150} value={title} onChange={(e) => setTitle(e.target.value)} />
      </label>
      <label>
        Captured on
        <input type="date" value={capturedAt} onChange={(e) => setCapturedAt(e.target.value)} />
      </label>
      <label>
        Notes
        <textarea rows={2} maxLength={1000} value={notes} onChange={(e) => setNotes(e.target.value)} />
      </label>
      {error && <p className="error">{error}</p>}
      <button type="submit" disabled={busy}>{busy ? 'Uploading…' : 'Upload scan'}</button>
    </form>
  )
}
