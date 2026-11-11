import { useSyncExternalStore } from 'react'
import { isValidStudentId } from './studentId.ts'

export const STUDENT_ID_KEY = 'studentId'

type Listener = () => void
const listeners = new Set<Listener>()

function read(): string | null {
  try {
    const stored = window.localStorage.getItem(STUDENT_ID_KEY)
    return stored !== null && isValidStudentId(stored) ? stored.trim() : null
  } catch {
    return null
  }
}

// Keeps the value when storage is unavailable (private mode) so the tab still works.
let memoryValue: string | null = null
let storageWorks = true

function snapshot(): string | null {
  return storageWorks ? read() : memoryValue
}

function notify(): void {
  listeners.forEach((listener) => listener())
}

function save(id: string): void {
  const trimmed = id.trim()
  if (!isValidStudentId(trimmed)) return
  try {
    window.localStorage.setItem(STUDENT_ID_KEY, trimmed)
    storageWorks = true
  } catch {
    storageWorks = false
    memoryValue = trimmed
  }
  notify()
}

function subscribe(listener: Listener): () => void {
  listeners.add(listener)
  const onStorage = (event: StorageEvent) => {
    if (event.key === null || event.key === STUDENT_ID_KEY) listener()
  }
  window.addEventListener('storage', onStorage)
  return () => {
    listeners.delete(listener)
    window.removeEventListener('storage', onStorage)
  }
}

export function useStudentId(): [string | null, (id: string) => void] {
  const id = useSyncExternalStore(subscribe, snapshot, () => null)
  return [id, save]
}
