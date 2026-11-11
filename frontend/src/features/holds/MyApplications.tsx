import { Text } from '@mantine/core'
import { serverNow } from '../../api/clock.ts'
import { useMyHolds, type MyHoldResponse } from '../../api/queries.ts'
import { QuerySection } from '../../components/QuerySection.tsx'
import { useStudentId } from '../student/useStudentId.ts'
import { remainingMs } from './countdown.ts'
import { groupHolds } from './groupHolds.ts'
import { HoldCard } from './HoldCard.tsx'
import { useHighlightedClassId } from './highlight.ts'
import { useTickingNow } from './ticker.ts'

export const NO_STUDENT_APPLICATIONS_HINT = "Enter the student's 17-digit number above to see applications."

const ACTIVE_STATUSES: ReadonlySet<string> = new Set(['HELD', 'CONFIRMED'])

// A HELD hold past its expiry is shown (and grouped) as EXPIRED before the server says so.
const withLocalExpiry = (hold: MyHoldResponse): MyHoldResponse =>
  hold.status === 'HELD' && remainingMs(hold.expiresAt, serverNow()) === 0 ? { ...hold, status: 'EXPIRED' } : hold

export function MyApplications() {
  const [studentId] = useStudentId()
  const holds = useMyHolds(studentId)
  const highlightedClassId = useHighlightedClassId()
  // Re-render every second while a hold is counting down so expiry moves it between groups.
  useTickingNow(holds.data?.some((hold) => hold.status === 'HELD') ?? false)
  // Cards keep the server's status (they detect and report their own expiry); only grouping and highlight use the local one.
  const originals = new Map(holds.data?.map((hold) => [hold.holdId, hold]))
  const items =
    studentId !== null && holds.data
      ? groupHolds(holds.data.map(withLocalExpiry)).map((shown) => ({
          shown,
          hold: originals.get(shown.holdId) ?? shown,
          studentId,
        }))
      : undefined

  return (
    <QuerySection
      titleId="applications-title"
      title="Guardian: my applications"
      hint={studentId === null && <Text size="sm" c="dimmed">{NO_STUDENT_APPLICATIONS_HINT}</Text>}
      loading={holds.isLoading}
      loadingLabel="Loading applications"
      errorTitle="Could not load applications"
      emptyText="No applications yet."
      query={holds}
      items={items}
      renderItem={({ shown, hold, studentId: id }) => (
        <HoldCard
          key={hold.holdId}
          hold={hold}
          studentId={id}
          highlighted={shown.classId === highlightedClassId && ACTIVE_STATUSES.has(shown.status)}
        />
      )}
    />
  )
}
