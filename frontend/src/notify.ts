import { notifications } from '@mantine/notifications'

// One accent color for positive and informational messages, red for failures.
const ACCENT = 'accent'
const ERROR = 'red'

export const notifySuccess = (title: string, message: string) =>
  notifications.show({ color: ACCENT, title, message })

export const notifyInfo = (title: string, message: string) =>
  notifications.show({ color: ACCENT, title, message })

export const notifyError = (title: string, message: string) =>
  notifications.show({ color: ERROR, title, message })
