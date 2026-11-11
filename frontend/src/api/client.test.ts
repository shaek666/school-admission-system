import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiFetch, errorCode, STUDENT_HEADER } from './client'
import { resetClock, serverNow } from './clock'

const fetchMock = vi.fn()

function sentHeaders(call: number): Headers {
  const init = fetchMock.mock.calls[call]?.[1] as RequestInit
  return new Headers(init.headers)
}

function json(body: unknown, init: ResponseInit = {}): Response {
  return new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })
}

describe('apiFetch', () => {
  beforeEach(() => {
    window.APP_CONFIG = { apiBaseUrl: 'http://api.test' }
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
    vi.useRealTimers()
    resetClock()
    delete window.APP_CONFIG
  })

  it('returns the parsed success body', async () => {
    fetchMock.mockResolvedValue(json({ id: '1' }))
    await expect(apiFetch('/classes')).resolves.toEqual({ id: '1' })
    expect(fetchMock.mock.calls[0]?.[0]).toBe('http://api.test/classes')
  })

  it('turns an error body into ApiError', async () => {
    fetchMock.mockResolvedValue(
      json({ error: { code: 'NO_SEATS_AVAILABLE', message: 'Full.' } }, { status: 409 }),
    )
    const error = await apiFetch('/x').catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ code: 'NO_SEATS_AVAILABLE', status: 409, message: 'Full.' })
  })

  it('maps a rejected fetch to NETWORK with status 0', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))
    await expect(apiFetch('/x')).rejects.toMatchObject({ code: 'NETWORK', status: 0 })
  })

  it('rethrows an abort instead of mapping it to NETWORK', async () => {
    const controller = new AbortController()
    controller.abort()
    const abort = new DOMException('Aborted', 'AbortError')
    fetchMock.mockRejectedValue(abort)
    await expect(apiFetch('/x', { signal: controller.signal })).rejects.toBe(abort)
  })

  it('resolves 204 to undefined', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))
    await expect(apiFetch('/x', { method: 'POST' })).resolves.toBeUndefined()
  })

  it('maps a non-JSON error response to REQUEST_ERROR and keeps the status', async () => {
    fetchMock.mockResolvedValue(new Response('<html>bad gateway</html>', { status: 502 }))
    await expect(apiFetch('/x')).rejects.toMatchObject({ code: 'REQUEST_ERROR', status: 502 })
  })

  it('maps an envelope-less JSON error to REQUEST_ERROR', async () => {
    fetchMock.mockResolvedValue(json({ oops: true }, { status: 500 }))
    await expect(apiFetch('/x')).rejects.toMatchObject({ code: 'REQUEST_ERROR', status: 500 })
  })

  it('sends the student header and records the Date header', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))
    fetchMock.mockResolvedValue(json({}, { headers: { Date: 'Thu, 01 Jan 2026 00:00:30 GMT' } }))
    await apiFetch('/holds', { studentId: '12345678901234567' })
    expect(sentHeaders(0).get(STUDENT_HEADER)).toBe('12345678901234567')
    expect(serverNow()).toBe(Date.parse('2026-01-01T00:00:30Z') + 500)
  })

  it('records the Date header on error responses too', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))
    fetchMock.mockResolvedValue(
      json({ error: { code: 'BAD_REQUEST', message: 'm' } }, {
        status: 400,
        headers: { Date: 'Thu, 01 Jan 2026 00:00:30 GMT' },
      }),
    )
    await apiFetch('/x').catch(() => undefined)
    expect(serverNow()).toBe(Date.parse('2026-01-01T00:00:30Z') + 500)
  })

  it('sets Content-Type only with a body and does not mutate the caller init', async () => {
    fetchMock.mockImplementation(() => Promise.resolve(json({})))
    const headers = { Accept: 'application/json' }
    const init = { method: 'POST', body: '{}', headers, studentId: '1' }
    await apiFetch('/x', init)
    const sent = sentHeaders(0)
    expect(sent.get('Content-Type')).toBe('application/json')
    expect(sent.get('Accept')).toBe('application/json')
    expect(headers).toEqual({ Accept: 'application/json' })
    expect(init.studentId).toBe('1')

    await apiFetch('/x')
    const bare = sentHeaders(1)
    expect(bare.has('Content-Type')).toBe(false)
    expect(bare.has(STUDENT_HEADER)).toBe(false)
  })

  it('rejects a bad header value without calling it a network failure', async () => {
    const result = apiFetch('/x', { studentId: 'bad\nvalue' })
    await expect(result).rejects.not.toMatchObject({ code: 'NETWORK' })
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('maps an unreadable 2xx body to REQUEST_ERROR', async () => {
    fetchMock.mockResolvedValue(new Response('<!doctype html>', { status: 200 }))
    await expect(apiFetch('/x')).rejects.toMatchObject({ code: 'REQUEST_ERROR', status: 200 })
  })

  it('maps a connection lost mid-body to NETWORK', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers(),
      text: () => Promise.reject(new TypeError('terminated')),
    })
    await expect(apiFetch('/x')).rejects.toMatchObject({ code: 'NETWORK', status: 0 })
  })

  it('keeps a Content-Type the caller set', async () => {
    fetchMock.mockResolvedValue(json({}))
    await apiFetch('/x', { method: 'POST', body: 'x', headers: { 'Content-Type': 'text/plain' } })
    expect(sentHeaders(0).get('Content-Type')).toBe('text/plain')
  })
})

describe('errorCode', () => {
  it('returns the code of an ApiError', () => {
    expect(errorCode(new ApiError('NO_SEATS_AVAILABLE', 409, 'full'))).toBe('NO_SEATS_AVAILABLE')
  })

  it('falls back to REQUEST_ERROR for anything else', () => {
    expect(errorCode(new Error('boom'))).toBe('REQUEST_ERROR')
    expect(errorCode('boom')).toBe('REQUEST_ERROR')
    expect(errorCode(undefined)).toBe('REQUEST_ERROR')
  })
})
