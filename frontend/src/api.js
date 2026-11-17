// Small wrapper around fetch for every call to the ScanVault REST API.
// Errors come back from the API as ProblemDetails JSON; we turn them into readable messages.

async function request(path, options = {}) {
  const response = await fetch(path, options)
  if (response.status === 204) return null

  const body = await response.json().catch(() => null)
  if (!response.ok) {
    const validation = body?.errors ? Object.values(body.errors).flat().join(' ') : null
    throw new Error(validation || body?.detail || body?.title || `Request failed (${response.status})`)
  }
  return body
}

const json = (method, data) => ({
  method,
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(data)
})

export const api = {
  getSites: () => request('/api/sites'),
  getSite: (id) => request(`/api/sites/${id}`),
  createSite: (site) => request('/api/sites', json('POST', site)),
  updateSite: (id, site) => request(`/api/sites/${id}`, json('PUT', site)),
  deleteSite: (id) => request(`/api/sites/${id}`, { method: 'DELETE' }),

  // Files go up as multipart/form-data, the same format an HTML form uses.
  uploadScan: (siteId, { file, title, notes, capturedAt }) => {
    const form = new FormData()
    form.append('File', file)
    form.append('Title', title)
    if (notes) form.append('Notes', notes)
    if (capturedAt) form.append('CapturedAt', new Date(capturedAt).toISOString())
    return request(`/api/sites/${siteId}/scans`, { method: 'POST', body: form })
  },
  deleteScan: (id) => request(`/api/scans/${id}`, { method: 'DELETE' }),

  getAnnotations: (scanId) => request(`/api/scans/${scanId}/annotations`),
  createAnnotation: (scanId, annotation) => request(`/api/scans/${scanId}/annotations`, json('POST', annotation)),
  deleteAnnotation: (id) => request(`/api/annotations/${id}`, { method: 'DELETE' })
}

export function formatBytes(bytes) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

export function formatDate(value) {
  return value ? new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' }) : '—'
}
