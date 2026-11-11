import { useSyncExternalStore } from 'react'
import { serverNow } from '../../api/clock.ts'

const TICK_MS = 1000

// One shared interval for all subscribed cards; null until read or after the last unsubscribe.
let snapshot: number | null = null
let intervalId: ReturnType<typeof setInterval> | undefined
const listeners = new Set<() => void>()

function tick(): void {
  snapshot = serverNow()
  listeners.forEach((listener) => listener())
}

function subscribe(listener: () => void): () => void {
  if (listeners.size === 0) intervalId = setInterval(tick, TICK_MS)
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
    if (listeners.size === 0) {
      clearInterval(intervalId)
      snapshot = null
    }
  }
}

const subscribeNever = () => () => {}
const noTime = () => 0

function getSnapshot(): number {
  snapshot ??= serverNow()
  return snapshot
}

/** Server-corrected "now", refreshed every second; inactive callers do not subscribe and get 0. */
export function useTickingNow(active: boolean): number {
  return useSyncExternalStore(active ? subscribe : subscribeNever, active ? getSnapshot : noTime)
}
