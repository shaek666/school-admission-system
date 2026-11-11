import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '../../test-utils.tsx'
import { CreateClassForm } from './CreateClassForm.tsx'

describe('CreateClassForm', () => {
  const fetchMock = vi.fn()
  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })
  afterEach(() => vi.unstubAllGlobals())

  it('shows the seats error and sends nothing for an out-of-range count', async () => {
    renderWithProviders(<CreateClassForm />)
    await userEvent.type(screen.getByLabelText('Class name'), 'Class Six')
    await userEvent.type(screen.getByLabelText('Total seats'), '150000')
    await userEvent.click(screen.getByRole('button', { name: 'Create class' }))
    expect(await screen.findByText(/seats from 1 to 100000/)).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
