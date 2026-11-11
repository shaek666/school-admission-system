import { act, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { renderWithProviders } from '../../test-utils.tsx'
import { StudentBar } from './StudentBar.tsx'
import { useStudentId } from './useStudentId.ts'

const VALID = '12345678901234567'
const LABEL = /student birth registration number/i

function Reader() {
  const [id] = useStudentId()
  return <p data-testid="reader">{id ?? 'none'}</p>
}

beforeEach(() => window.localStorage.clear())
afterEach(() => window.localStorage.clear())

describe('StudentBar', () => {
  it('shows the message and stores nothing for an invalid id', async () => {
    renderWithProviders(<StudentBar />)
    await userEvent.type(screen.getByLabelText(LABEL), '1234')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText('Enter exactly 17 digits.')).toBeInTheDocument()
    expect(window.localStorage.getItem('studentId')).toBeNull()
  })

  it('stores the trimmed id and shows it for a valid submit', async () => {
    renderWithProviders(<StudentBar />)
    await userEvent.type(screen.getByLabelText(LABEL), VALID)
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText(VALID)).toBeInTheDocument()
    expect(window.localStorage.getItem('studentId')).toBe(VALID)
  })

  it('saves the trimmed id when a value with a leading space is pasted', async () => {
    renderWithProviders(<StudentBar />)
    await userEvent.click(screen.getByLabelText(LABEL))
    await userEvent.paste(` ${VALID}`)
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText(VALID)).toBeInTheDocument()
    expect(window.localStorage.getItem('studentId')).toBe(VALID)
  })

  it('keeps other hook users in sync in the same tab', async () => {
    renderWithProviders(
      <>
        <StudentBar />
        <Reader />
      </>,
    )
    expect(screen.getByTestId('reader')).toHaveTextContent('none')
    await userEvent.type(screen.getByLabelText(LABEL), VALID)
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText(VALID, { selector: '[data-testid="reader"]' })).toBeInTheDocument()
  })
})

describe('useStudentId', () => {
  it('ignores an invalid stored value', () => {
    window.localStorage.setItem('studentId', 'abc')
    renderWithProviders(<Reader />)
    expect(screen.getByTestId('reader')).toHaveTextContent('none')
  })

  it('picks up a change made in another tab via a storage event', () => {
    renderWithProviders(<Reader />)
    expect(screen.getByTestId('reader')).toHaveTextContent('none')
    act(() => {
      window.localStorage.setItem('studentId', VALID)
      window.dispatchEvent(new StorageEvent('storage', { key: 'studentId', newValue: VALID }))
    })
    expect(screen.getByTestId('reader')).toHaveTextContent(VALID)
  })
})
