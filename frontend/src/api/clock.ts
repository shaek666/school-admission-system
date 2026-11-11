// A Date header is truncated to whole seconds, so the true server time lies
// in [serverMs, serverMs + 1000). Use the midpoint to halve the worst error.
const HALF_SECOND_MS = 500

let offsetMs = 0

/** Records the server/client clock offset from an HTTP `Date` header (1 s resolution). */
export function recordServerDate(header: string | null, clientNow: number = Date.now()): void {
  if (header === null) return
  const serverMs = Date.parse(header)
  if (Number.isNaN(serverMs)) return
  offsetMs = serverMs + HALF_SECOND_MS - clientNow
}

export function serverNow(): number {
  return Date.now() + offsetMs
}

/** Test-only: forget the recorded offset. */
export function resetClock(): void {
  offsetMs = 0
}
