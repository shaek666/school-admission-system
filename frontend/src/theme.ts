// Design tokens. index.css exposes the same values as CSS custom properties.
export const theme = {
  color: {
    accent: '#1f5f8b',
    accentText: '#ffffff',
    surface: '#ffffff',
    surfaceMuted: '#f3f5f7',
    text: '#1c2733',
    textMuted: '#5b6877',
    border: '#d5dbe2',
  },
  fontFamily:
    'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
  fontSize: {
    xs: '0.75rem',
    sm: '0.875rem',
    md: '1rem',
    lg: '1.25rem',
    xl: '1.5rem',
  },
  spacing: {
    xs: '0.25rem',
    sm: '0.5rem',
    md: '1rem',
    lg: '1.5rem',
    xl: '2.5rem',
  },
  radius: '6px',
} as const
