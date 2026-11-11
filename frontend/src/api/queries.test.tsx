import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useApply, useClasses } from './queries.ts'

describe('mutation settling', () => {
  const fetchMock = vi.fn()
  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })
  afterEach(() => vi.unstubAllGlobals())

  it('runs mutate onError promptly even when the follow-up refetch never resolves', async () => {
    let classesCalls = 0
    fetchMock.mockImplementation((url: string, init?: RequestInit) => {
      if (init?.method === 'POST') return Promise.reject(new TypeError('failed'))
      if (url.endsWith('/classes') && ++classesCalls > 1) return new Promise(() => {})
      return Promise.resolve(new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }))
    })
    const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    )
    const { result } = renderHook(() => ({ classes: useClasses(), apply: useApply() }), { wrapper })
    await waitFor(() => expect(result.current.classes.isSuccess).toBe(true))

    const onError = vi.fn()
    result.current.apply.mutate({ classId: 'c1', studentId: '12345678901234567' }, { onError })

    await waitFor(() => expect(onError).toHaveBeenCalled(), { timeout: 1000 })
  })
})
