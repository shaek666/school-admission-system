import { describe, expect, it } from 'vitest'
import { applyOutcome } from './applyOutcome.ts'

describe('applyOutcome', () => {
  it('highlights the existing application when already applied', () => {
    expect(applyOutcome('ALREADY_APPLIED')).toBe('highlight-existing')
  })

  it('refreshes classes when no seats are left', () => {
    expect(applyOutcome('NO_SEATS_AVAILABLE')).toBe('refresh-classes')
  })

  it.each(['SERVICE_BUSY', 'NETWORK', 'VALIDATION_ERROR', 'UNKNOWN', ''])('notifies for %j', (code) => {
    expect(applyOutcome(code)).toBe('notify')
  })
})
