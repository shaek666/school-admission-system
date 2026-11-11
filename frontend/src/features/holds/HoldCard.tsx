import { Badge, Button, Group, Paper, Stack, Text } from '@mantine/core'
import { useEffect, useRef, useState } from 'react'
import { errorCode } from '../../api/client.ts'
import { useCancel, useConfirm, useInvalidateAll, type MyHoldResponse } from '../../api/queries.ts'
import { serverNow } from '../../api/clock.ts'
import { messageFor } from '../../messages.ts'
import { notifyError, notifySuccess } from '../../notify.ts'
import { formatMmSs, remainingMs } from './countdown.ts'
import { clearHighlight } from './highlight.ts'
import { useTickingNow } from './ticker.ts'

const HIGHLIGHT_MS = 6000
const MAX_TIMEOUT_MS = 2 ** 31 - 1

const BADGE_COLORS: Readonly<Record<string, string>> = {
  HELD: 'yellow',
  CONFIRMED: 'green',
  EXPIRED: 'gray',
  CANCELLED: 'red',
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
  const now = useTickingNow(isHeld)
  const invalidateAll = useInvalidateAll()
  const expiryReported = useRef(false)
  const remaining = remainingMs(hold.expiresAt, now)
  // The 1 s ticker can lag the expiry; a one-off timer at the exact instant closes that gap.
  const expiryKey = `${hold.holdId}@${hold.expiresAt}`
  const [expiredKey, setExpiredKey] = useState<string | null>(null)
  const timedOut = isHeld && (remaining <= 0 || expiredKey === expiryKey)
  const status = timedOut ? 'EXPIRED' : hold.status
  const disabled = timedOut || confirm.isPending || cancel.isPending

  // Let the server's EXPIRED replace the local override; once per card.
  useEffect(() => {
    if (!timedOut || expiryReported.current) return
    expiryReported.current = true
    void invalidateAll()
  }, [timedOut, invalidateAll])

  useEffect(() => {
    if (!isHeld) return
    const delay = remainingMs(hold.expiresAt, serverNow())
    if (delay > MAX_TIMEOUT_MS) return
    const timer = setTimeout(() => setExpiredKey(expiryKey), delay)
    return () => clearTimeout(timer)
  }, [isHeld, hold.expiresAt, expiryKey])

  // The highlight lasts from when this card shows it, not from when the error arrived.
  useEffect(() => {
    if (!highlighted) return
    const timer = setTimeout(() => clearHighlight(hold.classId), HIGHLIGHT_MS)
    return () => clearTimeout(timer)
  }, [highlighted, hold.classId])

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
        {highlighted && <Text size="sm" role="status">{messageFor('ALREADY_APPLIED')}</Text>}
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
