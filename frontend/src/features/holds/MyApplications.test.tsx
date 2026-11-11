import { act, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { recordServerDate, resetClock } from '../../api/clock.ts'
import { json, useFetchStub } from '../../test-fetch.ts'
import { renderWithProviders } from '../../test-utils.tsx'
import { ClassList } from '../classes/ClassList.tsx'
import { STUDENT_ID_KEY } from '../student/useStudentId.ts'
import { highlightClass, resetHighlight } from './highlight.ts'
import { MyApplications, NO_STUDENT_APPLICATIONS_HINT } from './MyApplications.tsx'

const VALID_ID = '12345678901234567'
const NOW = '2026-01-01T00:00:00Z'

const held = (expiresAt: string) => ({ holdId: 'h1', classId: 'c1', className: 'Class One', status: 'HELD', expiresAt })

describe('MyApplications', () => {
  const fetchMock = useFetchStub()

  beforeEach(() => {
    window.localStorage.setItem(STUDENT_ID_KEY, VALID_ID)
  })
  afterEach(() => {
    vi.useRealTimers()
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

  it('refetches /me/holds exactly once when a held card reaches zero', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date(NOW))
    recordServerDate('Thu, 01 Jan 2026 00:00:00 GMT')
    fetchMock.mockImplementation(() => Promise.resolve(json([held('2026-01-01T00:00:02Z')])))
    renderWithProviders(<MyApplications />)
    expect(await screen.findByText('00:02')).toBeInTheDocument()
    const holdsCalls = () => fetchMock.mock.calls.filter(([url]) => String(url).endsWith('/me/holds')).length
    expect(holdsCalls()).toBe(1)

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3_000)
    })
    expect(screen.getByText('EXPIRED')).toBeInTheDocument()
    expect(holdsCalls()).toBe(2)

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1_000)
    })
    expect(holdsCalls()).toBe(2)
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

  it('shows empty text when there are no applications', async () => {
    fetchMock.mockImplementation(() => Promise.resolve(json([])))
    renderWithProviders(<MyApplications />)
    expect(await screen.findByText('No applications yet.')).toBeInTheDocument()
  })

  it('shows the no-student hint and no loader without a student', async () => {
    window.localStorage.clear()
    renderWithProviders(<MyApplications />)
    expect(await screen.findByText(NO_STUDENT_APPLICATIONS_HINT)).toBeInTheDocument()
    expect(screen.queryByLabelText('Loading applications')).not.toBeInTheDocument()
  })

  it('shows an error alert with the network message and Retry', async () => {
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

  it('shows CONFIRMED with no Pay button right after a successful Pay, before any refetch', async () => {
    let posted = false
    fetchMock.mockImplementation((_url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        posted = true
        return Promise.resolve(json({ holdId: 'h1', status: 'CONFIRMED' }))
      }
      if (posted) return new Promise(() => {})
      return Promise.resolve(json([held(new Date(Date.now() + 600_000).toISOString())]))
    })
    renderWithProviders(<MyApplications />)
    await userEvent.click(await screen.findByRole('button', { name: /^Pay admission fee/ }))
    expect(await screen.findByText('CONFIRMED')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Pay admission fee/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Cancel/ })).not.toBeInTheDocument()
  })

  it('shows CANCELLED with no buttons right after a successful Cancel, before any refetch', async () => {
    let deleted = false
    fetchMock.mockImplementation((_url: string, init?: RequestInit) => {
      if (init?.method === 'DELETE') {
        deleted = true
        return Promise.resolve(new Response(null, { status: 204 }))
      }
      if (deleted) return new Promise(() => {})
      return Promise.resolve(json([held(new Date(Date.now() + 600_000).toISOString())]))
    })
    renderWithProviders(<MyApplications />)
    await userEvent.click(await screen.findByRole('button', { name: /^Cancel/ }))
    expect(await screen.findByText('CANCELLED')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Cancel/ })).not.toBeInTheDocument()
  })

  it('still highlights a card that appears 7 s after the ALREADY_APPLIED error, then clears after 6 s', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    fetchMock.mockImplementation(() => Promise.resolve(json([held(new Date(Date.now() + 600_000).toISOString())])))
    highlightClass('c1')
    await act(async () => {
      await vi.advanceTimersByTimeAsync(7_000)
    })
    renderWithProviders(<MyApplications />)
    const card = (await screen.findByText('Class One')).closest('li')
    expect(card).toHaveAttribute('data-highlighted')

    await act(async () => {
      await vi.advanceTimersByTimeAsync(6_100)
    })
    expect(screen.getByText('Class One').closest('li')).not.toHaveAttribute('data-highlighted')
  })

  it('flips to EXPIRED at the exact expiry, not at the next 1 s tick', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date(NOW))
    recordServerDate('Thu, 01 Jan 2026 00:00:00 GMT')
    fetchMock.mockImplementation(() => Promise.resolve(json([held('2026-01-01T00:00:01.550Z')])))
    renderWithProviders(<MyApplications />)
    expect(await screen.findByText('00:02')).toBeInTheDocument()

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1_000)
    })
    expect(screen.getByText('HELD')).toBeInTheDocument()
    await act(async () => {
      await vi.advanceTimersByTimeAsync(600)
    })
    expect(screen.getByText('EXPIRED')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^Pay admission fee/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /^Cancel/ })).toBeDisabled()
  })

  it('moves a locally expired hold out of the active group', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date(NOW))
    recordServerDate('Thu, 01 Jan 2026 00:00:00 GMT')
    const paid = { holdId: 'h2', classId: 'c2', className: 'Class Two', status: 'CONFIRMED', expiresAt: '2026-01-01T00:10:00Z' }
    fetchMock.mockImplementation(() => Promise.resolve(json([held('2026-01-01T00:00:02Z'), paid])))
    renderWithProviders(<MyApplications />)
    expect(await screen.findByText('00:02')).toBeInTheDocument()
    const names = () => screen.getAllByRole('listitem').map((li) => li.textContent?.slice(0, 9))
    expect(names()).toEqual(['Class One', 'Class Two'])

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3_000)
    })
    expect(names()).toEqual(['Class Two', 'Class One'])
  })
})
