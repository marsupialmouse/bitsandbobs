import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { Provider } from 'react-redux'
import { setupStore } from './stores/store.ts'

const container = document.getElementById('root')
if (!container)
  throw new Error('Failed to init the app. The Root is missing from the page')

createRoot(container).render(
  <StrictMode>
    <Provider store={setupStore()}>
      <App />
    </Provider>
  </StrictMode>
)
