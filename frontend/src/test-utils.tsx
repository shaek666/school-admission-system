import { MantineProvider } from '@mantine/core'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { mantineTheme } from './mantineTheme.ts'

export function renderWithMantine(ui: ReactElement) {
  return render(<MantineProvider theme={mantineTheme}>{ui}</MantineProvider>)
}

// A fresh QueryClient per call so no cache leaks between tests; retries off for speed.
export function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <MantineProvider theme={mantineTheme}>
      <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
    </MantineProvider>,
  )
}
