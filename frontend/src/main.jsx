import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import SitesPage from './pages/SitesPage.jsx'
import SitePage from './pages/SitePage.jsx'
import './styles.css'

function App() {
  return (
    <>
      <header className="topbar">
        <Link to="/" className="brand">
          <span className="brand-mark" aria-hidden="true">⬢</span> ScanVault
        </Link>
        <nav>
          <a href="/swagger" target="_blank" rel="noreferrer">API docs</a>
        </nav>
      </header>
      <main className="container">
        <Routes>
          <Route path="/" element={<SitesPage />} />
          <Route path="/sites/:siteId" element={<SitePage />} />
          <Route path="*" element={<p>Page not found. <Link to="/">Go to sites</Link></p>} />
        </Routes>
      </main>
    </>
  )
}

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>
)
