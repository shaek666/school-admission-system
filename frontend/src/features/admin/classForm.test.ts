import { describe, expect, it } from 'vitest'
import { validateClassForm } from './classForm.ts'

describe('validateClassForm', () => {
  it('accepts a valid name and seat count', () => {
    expect(validateClassForm({ name: 'Class One', totalSeats: 30 })).toEqual({})
  })

  it('accepts seats as a numeric string', () => {
    expect(validateClassForm({ name: 'A', totalSeats: '30' })).toEqual({})
  })

  it('accepts the seat bounds', () => {
    expect(validateClassForm({ name: 'A', totalSeats: 1 })).toEqual({})
    expect(validateClassForm({ name: 'A', totalSeats: 100000 })).toEqual({})
  })

  it.each(['', '   '])('rejects a blank name %j', (name) => {
    expect(validateClassForm({ name, totalSeats: 5 }).name).toBeDefined()
  })

  it('trims the name before counting length', () => {
    expect(validateClassForm({ name: `  ${'a'.repeat(200)}  `, totalSeats: 5 })).toEqual({})
    expect(validateClassForm({ name: 'a'.repeat(201), totalSeats: 5 }).name).toBeDefined()
  })

  it.each([0, -1, 100001, 1.5, NaN, Infinity, '', '  ', 'abc', '1.5', '1e3', '0', '100001'])(
    'rejects seats %j',
    (totalSeats) => {
      expect(validateClassForm({ name: 'A', totalSeats }).totalSeats).toBeDefined()
    },
  )

  it('reports both errors at once', () => {
    const errors = validateClassForm({ name: '', totalSeats: 0 })
    expect(errors.name).toBeDefined()
    expect(errors.totalSeats).toBeDefined()
  })
})
