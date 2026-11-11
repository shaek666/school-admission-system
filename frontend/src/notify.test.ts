import { notifications } from '@mantine/notifications'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { notifyError, notifyInfo, notifySuccess } from './notify.ts'

describe('notify', () => {
  afterEach(() => vi.restoreAllMocks())

  it('uses the accent color for success and info and red for errors', () => {
    const show = vi.spyOn(notifications, 'show').mockReturnValue('id')
    notifySuccess('a', 'b')
    notifyInfo('c', 'd')
    notifyError('e', 'f')
    expect(show.mock.calls.map(([n]) => n.color)).toEqual(['accent', 'accent', 'red'])
    expect(show.mock.calls[2][0]).toMatchObject({ title: 'e', message: 'f' })
  })
})
