import { afterEach, beforeEach, vi } from 'vitest'

export function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

/** Registers hooks that stub global fetch with a fresh mock per test; returns the mock. */
export function useFetchStub() {
  const fetchMock = vi.fn()
  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })
  afterEach(() => {
    vi.unstubAllGlobals()
  })
  return fetchMock
}
