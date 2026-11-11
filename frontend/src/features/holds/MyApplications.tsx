import { Text } from '@mantine/core'
import { useMyHolds } from '../../api/queries.ts'
import { QuerySection } from '../../components/QuerySection.tsx'
import { useStudentId } from '../student/useStudentId.ts'
import { groupHolds } from './groupHolds.ts'
import { HoldCard } from './HoldCard.tsx'
import { useHighlightedClassId } from './highlight.ts'

export const NO_STUDENT_APPLICATIONS_HINT = "Enter the student's 17-digit number above to see applications."

const ACTIVE_STATUSES: ReadonlySet<string> = new Set(['HELD', 'CONFIRMED'])

export function MyApplications() {
  const [studentId] = useStudentId()
  const holds = useMyHolds(studentId)
  const highlightedClassId = useHighlightedClassId()

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
      items={holds.data && groupHolds(holds.data)}
      renderItem={(hold) =>
        // Without a student the query is disabled and there are no items; the check narrows the type.
        studentId !== null && (
          <HoldCard
            key={hold.holdId}
            hold={hold}
            studentId={studentId}
            highlighted={hold.classId === highlightedClassId && ACTIVE_STATUSES.has(hold.status)}
          />
        )
      }
    />
  )
}
