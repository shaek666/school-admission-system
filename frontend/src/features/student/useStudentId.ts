import { useSyncExternalStore } from 'react'
import { isValidStudentId } from './studentId.ts'

export const STUDENT_ID_KEY = 'studentId'

type Listener = () => void
const listeners = new Set<Listener>()

// Single source of truth. undefined means "not read from storage yet".
let cached: string | null | undefined

function readStorage(): string | null {
  try {
    const stored = window.localStorage.getItem(STUDENT_ID_KEY)
    return stored !== null && isValidStudentId(stored) ? stored.trim() : null
  } catch {
    return null
  }
}

function getSnapshot(): string | null {
  if (cached === undefined) cached = readStorage()
  return cached
}

function notify(): void {
  listeners.forEach((listener) => listener())
}

// The value stays in memory when storage is unavailable (private mode) so the tab still works.
function save(id: string): void {
  const trimmed = id.trim()
  if (!isValidStudentId(trimmed)) return
  cached = trimmed
  try {
    window.localStorage.setItem(STUDENT_ID_KEY, trimmed)
  } catch {
    // keep the in-memory value
  }
  notify()
}

function onStorage(event: StorageEvent): void {
  if (event.key !== null && event.key !== STUDENT_ID_KEY) return
  cached = readStorage()
  notify()
}

function subscribe(listener: Listener): () => void {
  if (listeners.size === 0) {
    // Storage may have changed while nobody was listening; read it again on next access.
    cached = undefined
    window.addEventListener('storage', onStorage)
  }
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
    if (listeners.size === 0) window.removeEventListener('storage', onStorage)
  }
}

export function useStudentId(): [string | null, (id: string) => void] {
  const id = useSyncExternalStore(subscribe, getSnapshot, () => null)
  return [id, save]
}
