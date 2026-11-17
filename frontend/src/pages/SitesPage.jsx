import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, formatDate } from '../api.js'

// Home page: every site with its scan count, plus a form to add a new site.
export default function SitesPage() {
  const [sites, setSites] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [form, setForm] = useState({ name: '', location: '', description: '' })
  const [saving, setSaving] = useState(false)

  const load = () =>
    api.getSites()
      .then(setSites)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))

  useEffect(() => { load() }, [])

  async function handleCreate(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await api.createSite(form)
      setForm({ name: '', location: '', description: '' })
      await load()
    } catch (e) {
      setError(e.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="layout-two">
      <section>
        <h1>Sites</h1>
        <p className="muted">Each site keeps its scans in date order, so you can see how it changes over time.</p>
        {error && <p className="error">{error}</p>}
        {loading ? (
          <p className="muted">Loading…</p>
        ) : sites.length === 0 ? (
          <div className="empty">No sites yet. Create your first one to start uploading scans.</div>
        ) : (
          <ul className="card-grid">
            {sites.map((site) => (
              <li key={site.id}>
                <Link to={`/sites/${site.id}`} className="card site-card">
                  <h3>{site.name}</h3>
                  <p className="muted">{site.location || 'No location'}</p>
                  <div className="stats">
                    <span><strong>{site.scanCount}</strong> scans</span>
                    <span>Last capture: {formatDate(site.lastCapturedAt)}</span>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>

      <aside className="card">
        <h2>New site</h2>
        <form onSubmit={handleCreate} className="form">
          <label>
            Name
            <input required minLength={2} maxLength={120} value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="Block A – Floor 2" />
          </label>
          <label>
            Location
            <input maxLength={200} value={form.location}
              onChange={(e) => setForm({ ...form, location: e.target.value })} placeholder="Hyderabad" />
          </label>
          <label>
            Description
            <textarea maxLength={1000} rows={3} value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </label>
          <button type="submit" disabled={saving}>{saving ? 'Creating…' : 'Create site'}</button>
        </form>
      </aside>
    </div>
  )
}
