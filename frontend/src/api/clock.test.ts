import { afterEach, describe, expect, it, vi } from 'vitest'
import { recordServerDate, resetClock, serverNow } from './clock'

describe('server clock', () => {
  afterEach(() => {
    resetClock()
    vi.useRealTimers()
  })

  it('applies the offset between server and client time', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))
    recordServerDate('Thu, 01 Jan 2026 00:00:10 GMT', Date.now())
    expect(serverNow()).toBe(Date.parse('2026-01-01T00:00:10Z') + 500)
    vi.advanceTimersByTime(5000)
    expect(serverNow()).toBe(Date.parse('2026-01-01T00:00:15Z') + 500)
  })

  it('ignores null and unparsable headers', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))
    recordServerDate('Thu, 01 Jan 2026 00:01:00 GMT', Date.now())
    recordServerDate(null)
    recordServerDate('not a date')
    expect(serverNow()).toBe(Date.parse('2026-01-01T00:01:00Z') + 500)
  })

  it('has no offset before any header is recorded', () => {
    vi.useFakeTimers()
    vi.setSystemTime(1_000_000)
    expect(serverNow()).toBe(1_000_000)
  })
})
