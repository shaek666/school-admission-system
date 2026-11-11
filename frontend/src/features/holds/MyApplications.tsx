import { Alert, Button, Loader, Paper, Stack, Text, Title } from '@mantine/core'
import { errorCode } from '../../api/client.ts'
import { useMyHolds } from '../../api/queries.ts'
import { messageFor } from '../../messages.ts'
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
  const items = holds.data ? groupHolds(holds.data) : []

  return (
    <Paper withBorder p="lg" component="section" aria-labelledby="applications-title">
      <Stack>
        <Title order={2} size="h3" id="applications-title">Guardian: my applications</Title>
        {studentId === null && <Text size="sm" c="dimmed">{NO_STUDENT_APPLICATIONS_HINT}</Text>}
        {studentId !== null && holds.isPending && <Loader size="sm" aria-label="Loading applications" />}
        {holds.isError && (
          <Alert color="red" title="Could not load applications">
            <Stack gap="xs" align="flex-start">
              <Text size="sm">{messageFor(errorCode(holds.error))}</Text>
              <Button size="xs" variant="default" loading={holds.isFetching} onClick={() => void holds.refetch()}>
                Retry
              </Button>
            </Stack>
          </Alert>
        )}
        {studentId !== null && holds.data?.length === 0 && <Text>No applications yet.</Text>}
        {studentId !== null && items.length > 0 && (
          <Stack component="ul" p={0} m={0}>
            {items.map((hold) => (
              <HoldCard
                key={hold.holdId}
                hold={hold}
                studentId={studentId}
                highlighted={hold.classId === highlightedClassId && ACTIVE_STATUSES.has(hold.status)}
              />
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
