import { StrictMode } from 'react'
import { MantineProvider } from '@mantine/core'
import '@mantine/core/styles.css'
import { Notifications } from '@mantine/notifications'
import '@mantine/notifications/styles.css'
import { QueryClientProvider } from '@tanstack/react-query'
import { createRoot } from 'react-dom/client'
import './index.css'
import { createQueryClient } from './api/queries.ts'
import App from './App.tsx'
import { ErrorBoundary } from './ErrorBoundary.tsx'
import { mantineTheme } from './mantineTheme.ts'

const queryClient = createQueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider theme={mantineTheme}>
      <Notifications />
      <QueryClientProvider client={queryClient}>
        <ErrorBoundary>
          <App />
        </ErrorBoundary>
      </QueryClientProvider>
    </MantineProvider>
  </StrictMode>,
)
