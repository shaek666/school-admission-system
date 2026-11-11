import { act, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { recordServerDate, resetClock } from '../../api/clock.ts'
import { renderWithProviders } from '../../test-utils.tsx'
import { ClassList } from '../classes/ClassList.tsx'
import { STUDENT_ID_KEY } from '../student/useStudentId.ts'
import { resetHighlight } from './highlight.ts'
import { MyApplications } from './MyApplications.tsx'

const VALID_ID = '12345678901234567'
const NOW = '2026-01-01T00:00:00Z'

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

const held = (expiresAt: string) => ({ holdId: 'h1', classId: 'c1', className: 'Class One', status: 'HELD', expiresAt })

describe('MyApplications', () => {
  const fetchMock = vi.fn()

  beforeEach(() => {
    window.localStorage.setItem(STUDENT_ID_KEY, VALID_ID)
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })
  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    window.localStorage.clear()
    resetClock()
    resetHighlight()
  })

  it('counts down in mm:ss and flips to EXPIRED with Pay disabled at zero', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date(NOW))
    recordServerDate('Thu, 01 Jan 2026 00:00:00 GMT')
    fetchMock.mockImplementation(() => Promise.resolve(json([held('2026-01-01T00:01:05Z')])))
    renderWithProviders(<MyApplications />)

    expect(await screen.findByText('01:05')).toBeInTheDocument()
    const pay = screen.getByRole('button', { name: /^Pay admission fee/ })
    expect(pay).toBeEnabled()
    expect(screen.getByText('HELD')).toBeInTheDocument()

    await act(async () => {
      await vi.advanceTimersByTimeAsync(65_000)
    })
    expect(screen.getByText('EXPIRED')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^Pay admission fee/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /^Cancel/ })).toBeDisabled()
  })

  it('names each Pay and Cancel button after its class', async () => {
    fetchMock.mockResolvedValue(json([held('2099-01-01T00:00:00Z')]))
    renderWithProviders(<MyApplications />)
    expect(await screen.findByRole('button', { name: 'Pay admission fee for Class One' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel application for Class One' })).toBeInTheDocument()
  })

  it('sends POST /holds/{id}/confirm with the student header when paying', async () => {
    fetchMock.mockImplementation((_url: string, init?: RequestInit) => {
      if (init?.method === 'POST') return Promise.resolve(json({ holdId: 'h1', status: 'CONFIRMED' }))
      return Promise.resolve(json([held(new Date(Date.now() + 600_000).toISOString())]))
    })
    renderWithProviders(<MyApplications />)
    await userEvent.click(await screen.findByRole('button', { name: /^Pay admission fee/ }))

    await waitFor(() => {
      const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')
      expect(post).toBeDefined()
      expect(post![0]).toMatch(/\/holds\/h1\/confirm$/)
      expect(new Headers(post![1].headers).get('X-Student-Id')).toBe(VALID_ID)
    })
  })

  it('sends DELETE /holds/{id} when cancelling', async () => {
    fetchMock.mockImplementation((_url: string, init?: RequestInit) => {
      if (init?.method === 'DELETE') return Promise.resolve(new Response(null, { status: 204 }))
      return Promise.resolve(json([held(new Date(Date.now() + 600_000).toISOString())]))
    })
    renderWithProviders(<MyApplications />)
    await userEvent.click(await screen.findByRole('button', { name: /^Cancel/ }))
    await waitFor(() => {
      const del = fetchMock.mock.calls.find(([, init]) => init?.method === 'DELETE')
      expect(del![0]).toMatch(/\/holds\/h1$/)
    })
  })

  it('shows empty text, a hint without a student, and an error alert with the network message', async () => {
    fetchMock.mockImplementation(() => Promise.resolve(json([])))
    const first = renderWithProviders(<MyApplications />)
    expect(await screen.findByText('No applications yet.')).toBeInTheDocument()
    first.unmount()

    fetchMock.mockImplementation(() => Promise.reject(new TypeError('down')))
    renderWithProviders(<MyApplications />)
    expect(await screen.findByText(/admission server cannot be reached/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
  })

  it('highlights the existing card when apply returns ALREADY_APPLIED', async () => {
    const other = { holdId: 'h2', classId: 'c2', className: 'Class Two', status: 'HELD', expiresAt: new Date(Date.now() + 600_000).toISOString() }
    const mine = { ...held(new Date(Date.now() + 600_000).toISOString()) }
    const classes = [{ id: 'c1', name: 'Class One', totalSeats: 10, heldSeats: 1, confirmedSeats: 0, availableSeats: 9 }]
    fetchMock.mockImplementation((url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        return Promise.resolve(json({ error: { code: 'ALREADY_APPLIED', message: 'x' } }, 409))
      }
      return Promise.resolve(json(url.endsWith('/classes') ? classes : [other, mine]))
    })
    renderWithProviders(<><ClassList /><MyApplications /></>)
    await userEvent.click(await screen.findByRole('button', { name: 'Apply to Class One' }))

    const note = await screen.findByText('You already applied to this class. Your application is below.')
    expect(note.closest('li')).toHaveTextContent('Class One')
    expect(screen.getAllByText('You already applied to this class. Your application is below.')).toHaveLength(1)
    expect(screen.getByText('Class Two').closest('li')).not.toHaveAttribute('data-highlighted')
  })
})
