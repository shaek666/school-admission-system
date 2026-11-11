import { describe, expect, it } from 'vitest'
import type { MyHoldResponse } from '../../api/queries.ts'
import { groupHolds } from './groupHolds.ts'

const hold = (holdId: string, status: string): MyHoldResponse => ({
  holdId,
  classId: `class-${holdId}`,
  className: `Class ${holdId}`,
  status,
  expiresAt: '2026-01-01T00:00:00Z',
})

describe('groupHolds', () => {
  it('orders HELD, CONFIRMED, then EXPIRED and CANCELLED together', () => {
    const input = [hold('a', 'CANCELLED'), hold('b', 'CONFIRMED'), hold('c', 'EXPIRED'), hold('d', 'HELD')]
    expect(groupHolds(input).map((h) => h.holdId)).toEqual(['d', 'b', 'a', 'c'])
  })

  it('keeps the incoming newest-first order inside each group', () => {
    const input = [hold('1', 'EXPIRED'), hold('2', 'HELD'), hold('3', 'CANCELLED'), hold('4', 'HELD'), hold('5', 'CONFIRMED')]
    expect(groupHolds(input).map((h) => h.holdId)).toEqual(['2', '4', '5', '1', '3'])
  })

  it('does not mutate the input and handles empty and unknown statuses', () => {
    const input = [hold('x', 'EXPIRED'), hold('y', 'HELD')]
    const copy = [...input]
    groupHolds(input)
    expect(input).toEqual(copy)
    expect(groupHolds([])).toEqual([])
    expect(groupHolds([hold('u', 'WEIRD'), hold('h', 'HELD')]).map((h) => h.holdId)).toEqual(['h', 'u'])
  })
})
