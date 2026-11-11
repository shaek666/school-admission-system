import { createTheme, type MantineColorsTuple } from '@mantine/core'
import { theme } from './theme.ts'

const PRIMARY_SHADE = 6
// Mix weights toward white (lighter shades) or black (darker shades); index 6 is the accent.
const SHADE_MIX: readonly number[] = [0.92, 0.82, 0.68, 0.52, 0.34, 0.16, 0, -0.14, -0.28, -0.42]

function mix(hex: string, amount: number): string {
  const target = amount >= 0 ? 255 : 0
  const weight = Math.abs(amount)
  const channels = [1, 3, 5].map((start) => {
    const value = parseInt(hex.slice(start, start + 2), 16)
    return Math.round(value + (target - value) * weight)
  })
  return `#${channels.map((c) => c.toString(16).padStart(2, '0')).join('')}`
}

export const accentShades = SHADE_MIX.map((amount) =>
  mix(theme.color.accent, amount),
) as unknown as MantineColorsTuple

export const mantineTheme = createTheme({
  fontFamily: theme.fontFamily,
  primaryColor: 'accent',
  primaryShade: PRIMARY_SHADE,
  colors: { accent: accentShades },
  defaultRadius: theme.radius,
})
