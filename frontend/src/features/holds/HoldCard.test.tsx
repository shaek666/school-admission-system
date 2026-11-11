import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { recordServerDate, resetClock } from '../../api/clock.ts'
import { renderWithProviders } from '../../test-utils.tsx'
import { HoldCard } from './HoldCard.tsx'

const NOW = '2026-01-01T00:00:00Z'
const STUDENT_ID = '12345678901234567'

const held = (expiresAt: string) => ({
  holdId: 'h1',
  classId: 'c1',
  className: 'Class One',
  status: 'HELD',
  expiresAt,
})

describe('HoldCard', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(NOW))
    recordServerDate('Thu, 01 Jan 2026 00:00:00 GMT')
  })

  afterEach(() => {
    vi.useRealTimers()
    resetClock()
  })

  it('shows EXPIRED and a disabled Pay button when the expiry is in the past', () => {
    renderWithProviders(<HoldCard hold={held('2025-12-31T23:59:00Z')} studentId={STUDENT_ID} highlighted={false} />)

    expect(screen.getByText('EXPIRED')).toBeInTheDocument()
    expect(screen.queryByText('HELD')).toBeNull()
    expect(screen.getByRole('button', { name: 'Pay admission fee for Class One' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel application for Class One' })).toBeDisabled()
  })

  it('shows HELD and an enabled Pay button while the hold is live', () => {
    renderWithProviders(<HoldCard hold={held('2026-01-01T00:10:00Z')} studentId={STUDENT_ID} highlighted={false} />)

    expect(screen.getByText('HELD')).toBeInTheDocument()
    expect(screen.queryByText('EXPIRED')).toBeNull()
    expect(screen.getByRole('button', { name: 'Pay admission fee for Class One' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Cancel application for Class One' })).toBeEnabled()
  })
})
