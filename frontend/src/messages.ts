const MESSAGES: ReadonlyMap<string, string> = new Map(Object.entries({
  ALREADY_APPLIED: 'You already applied to this class. Your application is below.',
  BAD_REQUEST: 'That request was not valid. Please check what you entered and try again.',
  CLASS_NOT_FOUND: 'We could not find that class. It may have been removed.',
  HOLD_CANCELLED: 'This application was cancelled.',
  HOLD_CONFIRMED: 'This application is already paid and confirmed.',
  HOLD_EXPIRED: 'The time to pay for this seat has run out.',
  HOLD_NOT_FOUND: 'We could not find that application.',
  INTERNAL_ERROR: 'Something went wrong on our side. Please try again in a moment.',
  INVALID_JSON: 'We could not read that request. Please check what you entered and try again.',
  INVALID_STUDENT_ID: 'The student ID must be exactly 17 digits.',
  METHOD_NOT_ALLOWED: 'That action is not allowed here.',
  NO_SEATS_AVAILABLE: 'This class just filled up.',
  NOT_FOUND: 'We could not find what you asked for.',
  NOT_HOLD_OWNER: 'This application belongs to a different student ID.',
  PAYLOAD_TOO_LARGE: 'That request was too large. Please shorten what you entered.',
  REQUEST_ERROR: 'The request could not be completed. Please try again.',
  SERVICE_BUSY: 'Too many guardians are applying right now. Please try again in a moment.',
  UNSUPPORTED_MEDIA_TYPE: 'We could not accept that kind of request. Please try again.',
  VALIDATION_ERROR: 'Some of the details are not valid. Please check them and try again.',
  NETWORK: 'The admission server cannot be reached. Please check your connection and try again.',
}))

const FALLBACK = 'Something unexpected happened. Please try again.'

export function messageFor(code: string): string {
  return MESSAGES.get(code) ?? FALLBACK
}
