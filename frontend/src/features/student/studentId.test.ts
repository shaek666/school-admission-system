import { describe, expect, it } from 'vitest'
import { isValidStudentId } from './studentId.ts'

describe('isValidStudentId', () => {
  it('accepts 17 digits', () => {
    expect(isValidStudentId('12345678901234567')).toBe(true)
  })

  it('rejects 16 digits and 18 digits', () => {
    expect(isValidStudentId('1234567890123456')).toBe(false)
    expect(isValidStudentId('123456789012345678')).toBe(false)
  })

  it('rejects letters and punctuation', () => {
    expect(isValidStudentId('1234567890123456a')).toBe(false)
    expect(isValidStudentId('1234567-901234567')).toBe(false)
  })

  it('trims surrounding spaces', () => {
    expect(isValidStudentId('  12345678901234567 ')).toBe(true)
  })

  it('rejects inner spaces and empty input', () => {
    expect(isValidStudentId('1234567 8901234567')).toBe(false)
    expect(isValidStudentId('')).toBe(false)
  })

  it('accepts ASCII digits only', () => {
    expect(isValidStudentId('١٢٣٤٥٦٧٨٩٠١٢٣٤٥٦٧')).toBe(false)
  })
})
