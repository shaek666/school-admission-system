import { MantineProvider } from '@mantine/core'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { mantineTheme } from './mantineTheme.ts'

export function renderWithMantine(ui: ReactElement) {
  return render(<MantineProvider theme={mantineTheme}>{ui}</MantineProvider>)
}
