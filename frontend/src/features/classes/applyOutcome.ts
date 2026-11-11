export type ApplyOutcome = 'highlight-existing' | 'refresh-classes' | 'notify'

export function applyOutcome(code: string): ApplyOutcome {
  if (code === 'ALREADY_APPLIED') return 'highlight-existing'
  if (code === 'NO_SEATS_AVAILABLE') return 'refresh-classes'
  return 'notify'
}
