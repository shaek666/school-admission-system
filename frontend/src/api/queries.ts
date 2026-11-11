import { QueryClient, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch, ApiError } from './client.ts'

export type ClassResponse = {
  id: string
  name: string
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
}

export type HoldCreatedResponse = {
  holdId: string
  classId: string
  studentId: string
  status: string
  expiresAt: string
}

export type MyHoldResponse = {
  holdId: string
  classId: string
  className: string
  status: string
  expiresAt: string
}

export const CLASSES_KEY = ['classes'] as const
export const myHoldsKey = (studentId: string | null) => ['myHolds', studentId] as const

const CLASSES_REFETCH_MS = 3000
const MY_HOLDS_REFETCH_MS = 5000
const MAX_RETRIES = 1

// A 4xx answer will not change on retry; network and 5xx failures might.
export function shouldRetry(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false
  return failureCount < MAX_RETRIES
}

export function createQueryClient(): QueryClient {
  return new QueryClient({ defaultOptions: { queries: { retry: shouldRetry } } })
}

export function useClasses() {
  return useQuery({
    queryKey: CLASSES_KEY,
    queryFn: ({ signal }) => apiFetch<ClassResponse[]>('/classes', { signal }),
    refetchInterval: CLASSES_REFETCH_MS,
  })
}

export function useMyHolds(studentId: string | null) {
  return useQuery({
    queryKey: myHoldsKey(studentId),
    queryFn: ({ signal }) => apiFetch<MyHoldResponse[]>('/me/holds', { signal, studentId: studentId ?? undefined }),
    enabled: studentId !== null,
    refetchInterval: MY_HOLDS_REFETCH_MS,
    refetchOnWindowFocus: true,
  })
}

export function useInvalidateAll() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: CLASSES_KEY }),
      queryClient.invalidateQueries({ queryKey: ['myHolds'] }),
    ])
}

// Invalidation is fire-and-forget: TanStack v5 awaits a returned onSettled promise,
// which would delay the caller's mutate callbacks until the refetch finishes.
function useInvalidatingMutation<TData, TVariables>(mutationFn: (variables: TVariables) => Promise<TData>) {
  const invalidateAll = useInvalidateAll()
  return useMutation({
    mutationFn,
    onSettled: () => {
      void invalidateAll()
    },
  })
}

export function useCreateClass() {
  return useInvalidatingMutation((body: { name: string; totalSeats: number }) =>
    apiFetch<ClassResponse>('/classes', { method: 'POST', body: JSON.stringify(body) }),
  )
}

export function useApply() {
  return useInvalidatingMutation(({ classId, studentId }: { classId: string; studentId: string }) =>
    apiFetch<HoldCreatedResponse>(`/classes/${encodeURIComponent(classId)}/holds`, { method: 'POST', studentId }),
  )
}

type ConfirmResponse = { holdId: string; status: string }

type HoldAction = { holdId: string; studentId: string }

export function useConfirm() {
  return useInvalidatingMutation(({ holdId, studentId }: HoldAction) =>
    apiFetch<ConfirmResponse>(`/holds/${encodeURIComponent(holdId)}/confirm`, { method: 'POST', studentId }),
  )
}

export function useCancel() {
  return useInvalidatingMutation(({ holdId, studentId }: HoldAction) =>
    apiFetch<void>(`/holds/${encodeURIComponent(holdId)}`, { method: 'DELETE', studentId }),
  )
}
