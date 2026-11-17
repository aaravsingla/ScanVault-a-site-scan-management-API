import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api, formatBytes, formatDate } from '../api.js'
import ScanViewer from '../components/ScanViewer.jsx'
import UploadScanForm from '../components/UploadScanForm.jsx'
import AnnotationPanel from '../components/AnnotationPanel.jsx'

// One site: its scan history on the left, the selected scan in the viewer, and annotations.
export default function SitePage() {
  const { siteId } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const [site, setSite] = useState(null)
  const [annotations, setAnnotations] = useState([])
  const [picking, setPicking] = useState(false)
  const [pendingPoint, setPendingPoint] = useState(null)
  const [error, setError] = useState('')

  const selectedId = Number(searchParams.get('scan')) || site?.scans[0]?.id
  const selectedScan = site?.scans.find((s) => s.id === selectedId)

  const loadSite = useCallback(
    () => api.getSite(siteId).then(setSite).catch((e) => setError(e.message)),
    [siteId]
  )

  useEffect(() => { loadSite() }, [loadSite])

  useEffect(() => {
    setPicking(false)
    setPendingPoint(null)
    if (!selectedScan) return setAnnotations([])
    api.getAnnotations(selectedScan.id).then(setAnnotations).catch((e) => setError(e.message))
  }, [selectedScan?.id])

  const selectScan = (id) => setSearchParams({ scan: String(id) })

  async function handleUploaded(scan) {
    await loadSite()
    selectScan(scan.id)
  }

  async function handleDeleteScan(scan) {
    if (!confirm(`Delete "${scan.title}" and its annotations?`)) return
    await api.deleteScan(scan.id).catch((e) => setError(e.message))
    setSearchParams({})
    loadSite()
  }

  async function handleDeleteSite() {
    if (!confirm(`Delete site "${site.name}" with all ${site.scans.length} scans?`)) return
    await api.deleteSite(site.id)
    navigate('/')
  }

  async function handleSaveAnnotation(annotation) {
    const created = await api.createAnnotation(selectedScan.id, annotation)
    setAnnotations((list) => [...list, created])
    setPicking(false)
    setPendingPoint(null)
    loadSite()
  }

  async function handleDeleteAnnotation(id) {
    await api.deleteAnnotation(id).catch((e) => setError(e.message))
    setAnnotations((list) => list.filter((a) => a.id !== id))
    loadSite()
  }

  if (error && !site) return <p className="error">{error} <Link to="/">Back to sites</Link></p>
  if (!site) return <p className="muted">Loading…</p>

  return (
    <div>
      <div className="row-between page-head">
        <div>
          <Link to="/" className="muted small">← All sites</Link>
          <h1>{site.name}</h1>
          <p className="muted">{[site.location, site.description].filter(Boolean).join(' · ') || 'No details'}</p>
        </div>
        <button className="secondary danger" onClick={handleDeleteSite}>Delete site</button>
      </div>
      {error && <p className="error">{error}</p>}

      <div className="layout-three">
        <aside className="card">
          <h2>Scan history</h2>
          {site.scans.length === 0 ? (
            <p className="muted small">No scans yet. Upload one below.</p>
          ) : (
            <ol className="timeline">
              {site.scans.map((scan) => (
                <li key={scan.id}>
                  <button className={`timeline-item ${scan.id === selectedId ? 'active' : ''}`} onClick={() => selectScan(scan.id)}>
                    <span className="timeline-date">{formatDate(scan.capturedAt)}</span>
                    <span className="timeline-title">{scan.title}</span>
                    <span className="muted small">
                      {scan.format === 'Model3D' ? '3D model' : 'Photo'} · {formatBytes(scan.sizeBytes)} · {scan.annotationCount} notes
                    </span>
                  </button>
                </li>
              ))}
            </ol>
          )}
          <h2 className="spaced">Upload scan</h2>
          <UploadScanForm siteId={site.id} onUploaded={handleUploaded} />
        </aside>

        <section className="card viewer-card">
          {selectedScan ? (
            <>
              <div className="row-between">
                <div>
                  <h2>{selectedScan.title}</h2>
                  <p className="muted small">
                    Captured {formatDate(selectedScan.capturedAt)} · {selectedScan.originalFileName}
                    {selectedScan.notes ? ` · ${selectedScan.notes}` : ''}
                  </p>
                </div>
                <div className="actions">
                  <a className="button secondary" href={selectedScan.fileUrl} download={selectedScan.originalFileName}>Download</a>
                  <button className="secondary danger" onClick={() => handleDeleteScan(selectedScan)}>Delete</button>
                </div>
              </div>
              <ScanViewer scan={selectedScan} annotations={annotations} picking={picking}
                pendingPoint={pendingPoint} onPick={setPendingPoint} />
              {selectedScan.format === 'Model3D' && (
                <p className="muted small">Drag to rotate · scroll to zoom</p>
              )}
            </>
          ) : (
            <div className="empty">Upload a .glb model or a photo to see it here.</div>
          )}
        </section>

        <aside className="card">
          {selectedScan ? (
            <AnnotationPanel
              annotations={annotations}
              picking={picking}
              pendingPoint={pendingPoint}
              onStartPicking={() => setPicking(true)}
              onCancel={() => { setPicking(false); setPendingPoint(null) }}
              onSave={handleSaveAnnotation}
              onDelete={handleDeleteAnnotation}
            />
          ) : (
            <p className="muted small">Select a scan to see its annotations.</p>
          )}
        </aside>
      </div>
    </div>
  )
}
