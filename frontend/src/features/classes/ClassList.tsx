import { Button, Group, Paper, Progress, Stack, Text } from '@mantine/core'
import { useQueryClient } from '@tanstack/react-query'
import { errorCode } from '../../api/client.ts'
import { myHoldsKey, useApply, useClasses, type ClassResponse, type MyHoldResponse } from '../../api/queries.ts'
import { QuerySection } from '../../components/QuerySection.tsx'
import { messageFor } from '../../messages.ts'
import { notifyError, notifyInfo } from '../../notify.ts'
import { highlightClass } from '../holds/highlight.ts'
import { useStudentId } from '../student/useStudentId.ts'
import { applyOutcome } from './applyOutcome.ts'

const ACTIVE_STATUSES: ReadonlySet<string> = new Set(['HELD', 'CONFIRMED'])

const NO_STUDENT_HINT_ID = 'no-student-hint'
export const NO_STUDENT_HINT = "Enter the student's 17-digit number above to apply."

function percent(part: number, total: number): number {
  return total > 0 ? Math.min(100, (part / total) * 100) : 0
}

function ClassCard({ item, canApply, applyingId, isApplying, onApply }: {
  item: ClassResponse
  canApply: boolean
  applyingId: string | null
  isApplying: boolean
  onApply: (classId: string) => void
}) {
  const full = item.availableSeats <= 0
  return (
    <Paper withBorder p="md" component="li" style={{ listStyle: 'none' }}>
      <Stack gap="xs">
        <Group justify="space-between" wrap="nowrap">
          <Text fw={600}>{item.name}</Text>
          {full ? (
            <Button disabled>Full</Button>
          ) : (
            <Button
              disabled={!canApply || isApplying}
              aria-describedby={canApply ? undefined : NO_STUDENT_HINT_ID}
              loading={applyingId === item.id}
              onClick={() => onApply(item.id)}
              aria-label={`Apply to ${item.name}`}
            >
              Apply
            </Button>
          )}
        </Group>
        <Progress.Root size="md">
          <Progress.Section
            value={percent(item.confirmedSeats, item.totalSeats)}
            color="accent.8"
            aria-label={`Confirmed seats in ${item.name}`}
          />
          <Progress.Section
            value={percent(item.heldSeats, item.totalSeats)}
            color="accent.3"
            aria-label={`Seats on hold in ${item.name}`}
          />
        </Progress.Root>
        <Text size="sm" c="dimmed">
          {item.availableSeats} of {item.totalSeats} seats available ({item.confirmedSeats} confirmed, {item.heldSeats} on hold)
        </Text>
      </Stack>
    </Paper>
  )
}

export function ClassList() {
  const [studentId] = useStudentId()
  const classes = useClasses()
  const apply = useApply()
  const queryClient = useQueryClient()

  const onApply = (classId: string) => {
    if (studentId === null || apply.isPending) return
    apply.mutate(
      { classId, studentId },
      {
        onError: (error) => {
          const code = errorCode(error)
          const message = messageFor(code)
          const outcome = applyOutcome(code)
          if (outcome === 'highlight-existing') {
            highlightClass(classId)
            // The highlighted card explains itself; only tell the guardian if no such card is cached.
            const cached = queryClient.getQueryData<MyHoldResponse[]>(myHoldsKey(studentId))
            const cardShown = cached?.some((hold) => hold.classId === classId && ACTIVE_STATUSES.has(hold.status))
            if (!cardShown) notifyInfo('Already applied', message)
          }
          else if (outcome === 'refresh-classes') notifyInfo('Class is full', message)
          else notifyError('Could not apply', message)
        },
      },
    )
  }

  return (
    <QuerySection
      titleId="guardian-title"
      title="Guardian: classes"
      hint={studentId === null && <Text size="sm" c="dimmed" id={NO_STUDENT_HINT_ID}>{NO_STUDENT_HINT}</Text>}
      loading={classes.isPending}
      loadingLabel="Loading classes"
      errorTitle="Could not load classes"
      emptyText="No classes yet."
      query={classes}
      items={classes.data}
      renderItem={(item) => (
        <ClassCard
          key={item.id}
          item={item}
          canApply={studentId !== null}
          applyingId={apply.isPending ? apply.variables.classId : null}
          isApplying={apply.isPending}
          onApply={onApply}
        />
      )}
    />
  )
}
