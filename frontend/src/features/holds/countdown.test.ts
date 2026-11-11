import { afterEach, describe, expect, it, vi } from 'vitest'
import { recordServerDate, resetClock, serverNow } from '../../api/clock'
import { formatMmSs, remainingMs } from './countdown'

describe('formatMmSs', () => {
  it.each([
    [600000, '10:00'],
    [59999, '01:00'],
    [0, '00:00'],
    [1, '00:01'],
    [61000, '01:01'],
    [3600000, '60:00'],
    [-5, '00:00'],
  ])('formats %i ms as %s', (ms, expected) => {
    expect(formatMmSs(ms)).toBe(expected)
  })
})

describe('remainingMs', () => {
  afterEach(() => {
    resetClock()
    vi.useRealTimers()
  })

  it('returns the difference to expiresAt', () => {
    expect(remainingMs('2026-11-11T10:10:00Z', Date.parse('2026-11-11T10:00:00Z'))).toBe(600000)
  })

  it('never goes below zero', () => {
    expect(remainingMs('2026-11-11T10:00:00Z', Date.parse('2026-11-11T10:05:00Z'))).toBe(0)
  })

  it('returns 0 for an unparsable expiresAt', () => {
    expect(remainingMs('not a date', 0)).toBe(0)
  })

  it('gives the server-side remainder when the client clock is 5 minutes fast', () => {
    vi.useFakeTimers()
    vi.setSystemTime(Date.parse('2026-11-11T10:05:00Z')) // client: server time + 5 min
    recordServerDate('Wed, 11 Nov 2026 10:00:00 GMT')
    // recordServerDate adds 500 ms (midpoint of the 1 s Date header), so the
    // corrected server time is 10:00:00.500 and 10 min minus 500 ms remain.
    expect(remainingMs('2026-11-11T10:10:00Z', serverNow())).toBe(599500)
    vi.advanceTimersByTime(1000)
    expect(remainingMs('2026-11-11T10:10:00Z', serverNow())).toBe(598500)
  })
})
