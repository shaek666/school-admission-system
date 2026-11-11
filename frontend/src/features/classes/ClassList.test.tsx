import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '../../test-utils.tsx'
import { STUDENT_ID_KEY } from '../student/useStudentId.ts'
import { ClassList, NO_STUDENT_HINT } from './ClassList.tsx'

const VALID_ID = '12345678901234567'

const open = { id: 'c1', name: 'Class One', totalSeats: 10, heldSeats: 2, confirmedSeats: 3, availableSeats: 5 }
const full = { id: 'c2', name: 'Class Two', totalSeats: 4, heldSeats: 1, confirmedSeats: 3, availableSeats: 0 }

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

describe('ClassList', () => {
  const fetchMock = vi.fn()

  beforeEach(() => {
    window.localStorage.clear()
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows a disabled Full button when no seats are available', async () => {
    window.localStorage.setItem(STUDENT_ID_KEY, VALID_ID)
    fetchMock.mockImplementation(() => Promise.resolve(json([full])))
    renderWithProviders(<ClassList />)
    expect(await screen.findByRole('button', { name: 'Full' })).toBeDisabled()
    expect(screen.queryByRole('button', { name: /Apply/ })).not.toBeInTheDocument()
  })

  it('disables Apply and shows a hint without a valid student ID', async () => {
    fetchMock.mockImplementation(() => Promise.resolve(json([open])))
    renderWithProviders(<ClassList />)
    expect(await screen.findByRole('button', { name: 'Apply to Class One' })).toBeDisabled()
    expect(screen.getByText(NO_STUDENT_HINT)).toBeInTheDocument()
  })

  it('sends the student ID header when applying', async () => {
    window.localStorage.setItem(STUDENT_ID_KEY, VALID_ID)
    fetchMock.mockImplementation((url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        return Promise.resolve(json({ holdId: 'h1', classId: 'c1', studentId: VALID_ID, status: 'HELD', expiresAt: 'x' }, 201))
      }
      return Promise.resolve(json(url.endsWith('/classes') ? [open] : []))
    })
    renderWithProviders(<ClassList />)
    await userEvent.click(await screen.findByRole('button', { name: 'Apply to Class One' }))

    await waitFor(() => {
      const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')
      expect(post).toBeDefined()
      expect(post![0]).toMatch(/\/classes\/c1\/holds$/)
      expect(new Headers(post![1].headers).get('X-Student-Id')).toBe(VALID_ID)
    })
  })

  it('shows an inline alert with Retry when loading fails', async () => {
    fetchMock.mockRejectedValue(new TypeError('failed'))
    renderWithProviders(<ClassList />)
    expect(await screen.findByText(/admission server cannot be reached/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
  })
})
