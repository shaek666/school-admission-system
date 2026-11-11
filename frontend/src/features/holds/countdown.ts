const MS_PER_SECOND = 1000
const SECONDS_PER_MINUTE = 60

/** Milliseconds until `expiresAt` (ISO UTC) on the server clock; 0 if expired or unparsable. */
export function remainingMs(expiresAt: string, serverNowMs: number): number {
  const expiresMs = Date.parse(expiresAt)
  if (Number.isNaN(expiresMs)) return 0
  return Math.max(0, expiresMs - serverNowMs)
}

/** Formats a duration as MM:SS, rounding up to whole seconds. Minutes may exceed 59. */
export function formatMmSs(ms: number): string {
  const totalSeconds = Math.ceil(Math.max(0, ms) / MS_PER_SECOND)
  const minutes = Math.floor(totalSeconds / SECONDS_PER_MINUTE)
  const seconds = totalSeconds % SECONDS_PER_MINUTE
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}
