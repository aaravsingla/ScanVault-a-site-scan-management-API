import { useRef } from 'react'
import '@google/model-viewer'

// Shows one scan: a rotatable 3D model (.glb) or a photo, with its annotations pinned on top.
// When `picking` is true, clicking the scan reports the clicked position through onPick({ x, y, z }).
export default function ScanViewer({ scan, annotations, picking, pendingPoint, onPick }) {
  const viewerRef = useRef(null)

  if (scan.format === 'Model3D') {
    function handleModelClick(event) {
      if (!picking || !viewerRef.current) return
      // Ask model-viewer which point on the 3D surface sits under the mouse.
      const hit = viewerRef.current.positionAndNormalFromPoint(event.clientX, event.clientY)
      if (hit) onPick({ x: round(hit.position.x), y: round(hit.position.y), z: round(hit.position.z) })
    }

    return (
      <model-viewer
        ref={viewerRef}
        src={scan.fileUrl}
        alt={scan.title}
        camera-controls=""
        camera-orbit="25deg 65deg auto"
        interaction-prompt="none"
        shadow-intensity="0.6"
        exposure="1"
        class={`viewer ${picking ? 'picking' : ''}`}
        onClick={handleModelClick}
      >
        {annotations.map((a, i) => (
          <div key={a.id} slot={`hotspot-${a.id}`} className={`pin pin-${a.category.toLowerCase()}`}
            data-position={`${a.x}m ${a.y}m ${a.z}m`} data-visibility-attribute="visible" title={a.text}>
            {i + 1}
          </div>
        ))}
        {pendingPoint && (
          <div slot="hotspot-pending" className="pin pin-pending"
            data-position={`${pendingPoint.x}m ${pendingPoint.y}m ${pendingPoint.z}m`}>+</div>
        )}
      </model-viewer>
    )
  }

  // Photos: positions are stored as fractions of the width and height (0 to 1).
  function handleImageClick(event) {
    if (!picking) return
    const box = event.currentTarget.getBoundingClientRect()
    onPick({
      x: round((event.clientX - box.left) / box.width),
      y: round((event.clientY - box.top) / box.height),
      z: 0
    })
  }

  return (
    <div className={`viewer image-viewer ${picking ? 'picking' : ''}`}>
      <div className="image-frame" onClick={handleImageClick}>
        <img src={scan.fileUrl} alt={scan.title} draggable="false" />
        {annotations.map((a, i) => (
          <div key={a.id} className={`pin pin-${a.category.toLowerCase()} pin-abs`}
            style={{ left: `${a.x * 100}%`, top: `${a.y * 100}%` }} title={a.text}>
            {i + 1}
          </div>
        ))}
        {pendingPoint && (
          <div className="pin pin-pending pin-abs"
            style={{ left: `${pendingPoint.x * 100}%`, top: `${pendingPoint.y * 100}%` }}>+</div>
        )}
      </div>
    </div>
  )
}

const round = (n) => Math.round(n * 1000) / 1000
