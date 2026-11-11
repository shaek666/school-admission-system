import { Badge, Button, Group, Paper, Stack, Text } from '@mantine/core'
import { useEffect, useRef, useState } from 'react'
import { serverNow } from '../../api/clock.ts'
import { errorCode } from '../../api/client.ts'
import { useCancel, useConfirm, type MyHoldResponse } from '../../api/queries.ts'
import { messageFor } from '../../messages.ts'
import { notifyError, notifySuccess } from '../../notify.ts'
import { formatMmSs, remainingMs } from './countdown.ts'

const TICK_MS = 1000
export const ALREADY_APPLIED_NOTE = 'You already applied to this class. Your application is below.'

const BADGE_COLORS: Readonly<Record<string, string>> = {
  HELD: 'yellow',
  CONFIRMED: 'green',
  EXPIRED: 'gray',
  CANCELLED: 'red',
}

function useServerNow(active: boolean): number {
  const [now, setNow] = useState(serverNow)
  useEffect(() => {
    if (!active) return
    const id = setInterval(() => setNow(serverNow()), TICK_MS)
    return () => clearInterval(id)
  }, [active])
  return now
}

export function HoldCard({ hold, studentId, highlighted }: {
  hold: MyHoldResponse
  studentId: string
  highlighted: boolean
}) {
  const confirm = useConfirm()
  const cancel = useCancel()
  const ref = useRef<HTMLLIElement>(null)
  const isHeld = hold.status === 'HELD'
  const now = useServerNow(isHeld)
  const remaining = remainingMs(hold.expiresAt, now)
  const timedOut = isHeld && remaining <= 0
  const status = timedOut ? 'EXPIRED' : hold.status
  const disabled = timedOut || confirm.isPending || cancel.isPending

  useEffect(() => {
    if (highlighted) ref.current?.scrollIntoView?.({ block: 'nearest', behavior: 'smooth' })
  }, [highlighted])

  const onPay = () =>
    confirm.mutate(
      { holdId: hold.holdId, studentId },
      {
        onSuccess: () => notifySuccess('Payment received', `Your seat in ${hold.className} is confirmed.`),
        onError: (error) => notifyError('Could not pay', messageFor(errorCode(error))),
      },
    )

  const onCancel = () =>
    cancel.mutate(
      { holdId: hold.holdId, studentId },
      { onError: (error) => notifyError('Could not cancel', messageFor(errorCode(error))) },
    )

  return (
    <Paper
      withBorder
      p="md"
      component="li"
      ref={ref}
      data-highlighted={highlighted || undefined}
      style={{ listStyle: 'none', ...(highlighted ? { borderColor: 'var(--mantine-color-accent-6)', borderWidth: 2 } : {}) }}
    >
      <Stack gap="xs">
        <Group justify="space-between" wrap="nowrap">
          <Text fw={600}>{hold.className}</Text>
          <Badge color={BADGE_COLORS[status] ?? 'gray'} variant="light">{status}</Badge>
        </Group>
        {highlighted && <Text size="sm" role="status">{ALREADY_APPLIED_NOTE}</Text>}
        {isHeld && (
          <>
            <Text size="sm" c="dimmed">
              Time left: <Text span fw={600} c="inherit" aria-label="Time left">{formatMmSs(remaining)}</Text>
            </Text>
            <Group gap="xs">
              <Button
                loading={confirm.isPending}
                disabled={disabled}
                onClick={onPay}
                aria-label={`Pay admission fee for ${hold.className}`}
              >
                Pay admission fee
              </Button>
              <Button
                variant="default"
                loading={cancel.isPending}
                disabled={disabled}
                onClick={onCancel}
                aria-label={`Cancel application for ${hold.className}`}
              >
                Cancel
              </Button>
            </Group>
          </>
        )}
      </Stack>
    </Paper>
  )
}
