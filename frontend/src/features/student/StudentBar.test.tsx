import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { renderWithMantine } from '../../test-utils.tsx'
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
    renderWithMantine(<StudentBar />)
    await userEvent.type(screen.getByLabelText(LABEL), '1234')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText('Enter exactly 17 digits.')).toBeInTheDocument()
    expect(window.localStorage.getItem('studentId')).toBeNull()
  })

  it('stores the trimmed id and shows it for a valid submit', async () => {
    renderWithMantine(<StudentBar />)
    await userEvent.type(screen.getByLabelText(LABEL), VALID)
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText(VALID)).toBeInTheDocument()
    expect(window.localStorage.getItem('studentId')).toBe(VALID)
  })

  it('keeps other hook users in sync in the same tab', async () => {
    renderWithMantine(
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
    renderWithMantine(<Reader />)
    expect(screen.getByTestId('reader')).toHaveTextContent('none')
  })
})
