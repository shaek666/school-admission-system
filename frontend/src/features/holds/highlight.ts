import { useSyncExternalStore } from 'react'

// The card clears its own highlight once shown; this cap only frees a highlight whose card never appears.
const SAFETY_CAP_MS = 30_000

let highlightedClassId: string | null = null
let timer: ReturnType<typeof setTimeout> | undefined
const listeners = new Set<() => void>()

function set(classId: string | null): void {
  clearTimeout(timer)
  highlightedClassId = classId
  if (classId !== null) timer = setTimeout(() => set(null), SAFETY_CAP_MS)
  listeners.forEach((listener) => listener())
}

/** Highlights the application card of a class until that card has been shown for a while. */
export function highlightClass(classId: string): void {
  set(classId)
}

/** Clears the highlight if it still belongs to `classId`. */
export function clearHighlight(classId: string): void {
  if (highlightedClassId === classId) set(null)
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
