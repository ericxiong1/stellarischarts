import { useEffect, useState } from 'react'
import type { Country } from './api'
import { CountryList } from './components/CountryList'
import { CountryDetail } from './components/CountryDetail'
import { GalacticScreen } from './components/GalacticScreen'
import { FileUpload } from './components/FileUpload'
import { Button } from '@/components/ui/button'
function App() {
  const [selectedCountry, setSelectedCountry] = useState<Country | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)
  const [view, setView] = useState<'empire' | 'galaxy'>('empire')
  const [empirePanel, setEmpirePanel] = useState<'docked' | 'hidden'>('docked')

  useEffect(() => {
    document.documentElement.classList.add('dark')
    document.body.classList.add('dark')
  }, [])

  const handleUploadSuccess = () => {
    // Refresh the country list
    setSelectedCountry(null)
    setRefreshKey(prev => prev + 1)
  }

  console.log('App component rendered')

  return (
    <div className="flex h-screen flex-col bg-background text-foreground">
      <header className="border-b border-border bg-card px-6 py-4">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold">Stellaris Charts</h1>
            <p className="text-sm text-muted-foreground">Save file parser and budget analyzer</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Button
              variant={view === 'empire' ? 'default' : 'outline'}
              onClick={() => setView('empire')}
            >
              Empire
            </Button>
            <Button
              variant={view === 'galaxy' ? 'default' : 'outline'}
              onClick={() => setView('galaxy')}
            >
              Galactic
            </Button>
            <FileUpload compact onUploadSuccess={handleUploadSuccess} />
          </div>
        </div>
      </header>
      
      <div className="relative flex flex-1 flex-col overflow-hidden md:flex-row">
        {view === 'empire' && (
          <>
            {empirePanel === 'docked' && (
              <aside className="relative flex w-full flex-col overflow-hidden border-b border-border bg-background md:w-[360px] md:border-b-0 md:border-r">
                <CountryList key={refreshKey} onSelectCountry={setSelectedCountry} />
              </aside>
            )}
            {empirePanel === 'docked' && (
              <button
                type="button"
                onClick={() => setEmpirePanel('hidden')}
                className="absolute top-6 z-10 hidden -translate-x-1/2 rounded-full border border-border bg-background px-3 py-2 text-base font-semibold text-muted-foreground shadow-sm transition hover:text-foreground md:inline-flex"
                style={{ left: '360px' }}
                aria-label="Hide empire list"
              >
                &lt;
              </button>
            )}
            {empirePanel === 'hidden' && (
              <button
                type="button"
                onClick={() => setEmpirePanel('docked')}
                className="absolute left-0 top-6 z-10 rounded-r-full border border-border bg-background px-3 py-2 text-base font-semibold text-muted-foreground shadow-sm transition hover:text-foreground"
                aria-label="Show empire list"
              >
                &gt;
              </button>
            )}
          </>
        )}
        
        <main className="flex flex-1 flex-col overflow-hidden">
          {view === 'empire' ? (
            <CountryDetail country={selectedCountry} />
          ) : (
            <GalacticScreen />
          )}
        </main>
      </div>
    </div>
  )
}

export default App
