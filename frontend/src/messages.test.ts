import { describe, expect, it } from 'vitest'
import { messageFor } from './messages'

const CODES = [
  'ALREADY_APPLIED', 'BAD_REQUEST', 'CLASS_NOT_FOUND', 'HOLD_CANCELLED', 'HOLD_CONFIRMED',
  'HOLD_EXPIRED', 'HOLD_NOT_FOUND', 'INTERNAL_ERROR', 'INVALID_JSON', 'INVALID_STUDENT_ID',
  'METHOD_NOT_ALLOWED', 'NO_SEATS_AVAILABLE', 'NOT_FOUND', 'NOT_HOLD_OWNER', 'PAYLOAD_TOO_LARGE',
  'REQUEST_ERROR', 'SERVICE_BUSY', 'UNSUPPORTED_MEDIA_TYPE', 'VALIDATION_ERROR', 'NETWORK',
]

describe('messageFor', () => {
  it.each(CODES)('has a sentence for %s', (code) => {
    expect(messageFor(code).trim()).not.toBe('')
  })

  it('gives each known code its own sentence', () => {
    expect(new Set(CODES.map(messageFor)).size).toBe(CODES.length)
  })

  it('falls back for unknown codes', () => {
    expect(messageFor('SOMETHING_NEW').trim()).not.toBe('')
    expect(CODES.map(messageFor)).not.toContain(messageFor('SOMETHING_NEW'))
  })

  it('uses the design sentences', () => {
    expect(messageFor('ALREADY_APPLIED')).toBe(
      'You already applied to this class. Your application is below.',
    )
    expect(messageFor('NO_SEATS_AVAILABLE')).toBe('This class just filled up.')
    expect(messageFor('SERVICE_BUSY')).toBe(
      'Too many guardians are applying right now. Please try again in a moment.',
    )
  })

  it('says the admission server cannot be reached for NETWORK', () => {
    expect(messageFor('NETWORK').toLowerCase()).toContain('admission server cannot be reached')
  })

  it('does not treat inherited object keys as codes', () => {
    expect(messageFor('constructor')).toBe(messageFor('SOMETHING_NEW'))
  })
})
