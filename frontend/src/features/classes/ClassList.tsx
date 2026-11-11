import { Alert, Button, Group, Loader, Paper, Progress, Stack, Text, Title } from '@mantine/core'
import { errorCode } from '../../api/client.ts'
import { useApply, useClasses, type ClassResponse } from '../../api/queries.ts'
import { messageFor } from '../../messages.ts'
import { notifyError, notifyInfo } from '../../notify.ts'
import { highlightClass } from '../holds/highlight.ts'
import { useStudentId } from '../student/useStudentId.ts'
import { applyOutcome } from './applyOutcome.ts'

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

  const onApply = (classId: string) => {
    if (studentId === null || apply.isPending) return
    apply.mutate(
      { classId, studentId },
      {
        onError: (error) => {
          const code = errorCode(error)
          const message = messageFor(code)
          const outcome = applyOutcome(code)
          if (outcome === 'highlight-existing') highlightClass(classId)
          else if (outcome === 'refresh-classes') notifyInfo('Class is full', message)
          else notifyError('Could not apply', message)
        },
      },
    )
  }

  return (
    <Paper withBorder p="lg" component="section" aria-labelledby="guardian-title">
      <Stack>
        <Title order={2} size="h3" id="guardian-title">Guardian: classes</Title>
        {studentId === null && <Text size="sm" c="dimmed" id={NO_STUDENT_HINT_ID}>{NO_STUDENT_HINT}</Text>}
        {classes.isPending && <Loader size="sm" aria-label="Loading classes" />}
        {classes.isError && (
          <Alert color="red" title="Could not load classes">
            <Stack gap="xs" align="flex-start">
              <Text size="sm">
                {messageFor(errorCode(classes.error))}
              </Text>
              <Button size="xs" variant="default" loading={classes.isFetching} onClick={() => void classes.refetch()}>
                Retry
              </Button>
            </Stack>
          </Alert>
        )}
        {classes.data?.length === 0 && <Text>No classes yet.</Text>}
        {classes.data && classes.data.length > 0 && (
          <Stack component="ul" p={0} m={0}>
            {classes.data.map((item) => (
              <ClassCard
                key={item.id}
                item={item}
                canApply={studentId !== null}
                applyingId={apply.isPending ? apply.variables.classId : null}
                isApplying={apply.isPending}
                onApply={onApply}
              />
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
