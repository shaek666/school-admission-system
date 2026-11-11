import { useSyncExternalStore } from 'react'

const HIGHLIGHT_MS = 6000

let highlightedClassId: string | null = null
let timer: ReturnType<typeof setTimeout> | undefined
const listeners = new Set<() => void>()

function set(classId: string | null): void {
  clearTimeout(timer)
  highlightedClassId = classId
  if (classId !== null) timer = setTimeout(() => set(null), HIGHLIGHT_MS)
  listeners.forEach((listener) => listener())
}

/** Highlights the application card of a class for a few seconds. */
export function highlightClass(classId: string): void {
  set(classId)
}

/** Test-only: drop any highlight. */
export function resetHighlight(): void {
  set(null)
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function useHighlightedClassId(): string | null {
  return useSyncExternalStore(subscribe, () => highlightedClassId, () => null)
}
