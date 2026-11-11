export const MAX_NAME_LENGTH = 200
export const MIN_SEATS = 1
export const MAX_SEATS = 100000

export type ClassFormValues = { name: string; totalSeats: number | string }
export type ClassFormErrors = { name?: string; totalSeats?: string }

const DIGITS_ONLY = /^\d+$/

// Returns the seat count when the value is a whole number in range, otherwise null.
export function parseSeats(value: number | string): number | null {
  const seats = typeof value === 'number' ? value : DIGITS_ONLY.test(value.trim()) ? Number(value.trim()) : NaN
  return Number.isInteger(seats) && seats >= MIN_SEATS && seats <= MAX_SEATS ? seats : null
}

export function validateClassForm(values: ClassFormValues): ClassFormErrors {
  const errors: ClassFormErrors = {}
  const name = values.name.trim()
  if (name.length < 1 || name.length > MAX_NAME_LENGTH) {
    errors.name = `Enter a class name of 1 to ${MAX_NAME_LENGTH} characters.`
  }
  if (parseSeats(values.totalSeats) === null) {
    errors.totalSeats = `Enter a whole number of seats from ${MIN_SEATS} to ${MAX_SEATS}.`
  }
  return errors
}
