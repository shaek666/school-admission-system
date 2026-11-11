import type { MyHoldResponse } from '../../api/queries.ts'

const RANK: Readonly<Record<string, number>> = { HELD: 0, CONFIRMED: 1, EXPIRED: 2, CANCELLED: 2 }
const UNKNOWN_RANK = 3

const rankOf = (status: string): number => RANK[status] ?? UNKNOWN_RANK

/** Active holds first, then paid, then history. The API order (newest first) is kept inside each group. */
export function groupHolds(holds: readonly MyHoldResponse[]): MyHoldResponse[] {
  return [...holds].sort((a, b) => rankOf(a.status) - rankOf(b.status))
}
