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

function useInvalidateAll() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: CLASSES_KEY }),
      queryClient.invalidateQueries({ queryKey: ['myHolds'] }),
    ])
}

export function useCreateClass() {
  const invalidateAll = useInvalidateAll()
  return useMutation({
    mutationFn: (body: { name: string; totalSeats: number }) =>
      apiFetch<ClassResponse>('/classes', { method: 'POST', body: JSON.stringify(body) }),
    onSettled: () => {
      void invalidateAll()
    },
  })
}

export function useApply() {
  const invalidateAll = useInvalidateAll()
  return useMutation({
    mutationFn: ({ classId, studentId }: { classId: string; studentId: string }) =>
      apiFetch<HoldCreatedResponse>(`/classes/${encodeURIComponent(classId)}/holds`, { method: 'POST', studentId }),
    onSettled: () => {
      void invalidateAll()
    },
  })
}
