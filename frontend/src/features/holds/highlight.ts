import { useSyncExternalStore } from 'react'

const HIGHLIGHT_MS = 6000

let highlightedClassId: string | null = null
let timer: ReturnType<typeof setTimeout> | undefined
const listeners = new Set<() => void>()

function emit(): void {
  listeners.forEach((listener) => listener())
}

/** Highlights the application card of a class for a few seconds. */
export function highlightClass(classId: string): void {
  clearTimeout(timer)
  highlightedClassId = classId
  timer = setTimeout(() => {
    highlightedClassId = null
    emit()
  }, HIGHLIGHT_MS)
  emit()
}

/** Test-only: drop any highlight. */
export function resetHighlight(): void {
  clearTimeout(timer)
  highlightedClassId = null
  emit()
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
