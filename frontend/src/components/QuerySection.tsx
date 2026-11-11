import { Alert, Button, Loader, Paper, Stack, Text, Title } from '@mantine/core'
import type { UseQueryResult } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { errorCode } from '../api/client.ts'
import { messageFor } from '../messages.ts'

type Props<T> = {
  titleId: string
  title: string
  hint?: ReactNode
  loading: boolean
  loadingLabel: string
  errorTitle: string
  emptyText: string
  query: Pick<UseQueryResult<unknown>, 'isError' | 'error' | 'isFetching' | 'refetch'>
  /** Items to list; undefined until data exists. Stale items stay visible under an error alert. */
  items: readonly T[] | undefined
  renderItem: (item: T) => ReactNode
}

export function QuerySection<T>({
  titleId, title, hint, loading, loadingLabel, errorTitle, emptyText, query, items, renderItem,
}: Props<T>) {
  return (
    <Paper withBorder p="lg" component="section" aria-labelledby={titleId}>
      <Stack>
        <Title order={2} size="h3" id={titleId}>{title}</Title>
        {hint}
        {loading && <Loader size="sm" aria-label={loadingLabel} />}
        {query.isError && (
          <Alert color="red" title={errorTitle}>
            <Stack gap="xs" align="flex-start">
              <Text size="sm">{messageFor(errorCode(query.error))}</Text>
              <Button size="xs" variant="default" loading={query.isFetching} onClick={() => void query.refetch()}>
                Retry
              </Button>
            </Stack>
          </Alert>
        )}
        {items?.length === 0 && <Text>{emptyText}</Text>}
        {items && items.length > 0 && (
          <Stack component="ul" p={0} m={0}>
            {items.map(renderItem)}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
