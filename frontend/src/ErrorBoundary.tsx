import { Component, type ErrorInfo, type ReactNode } from 'react'

type Props = { children: ReactNode }
type State = { failed: boolean }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { failed: false }

  static getDerivedStateFromError(): State {
    return { failed: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('Render error caught by ErrorBoundary', error, info.componentStack)
  }

  render(): ReactNode {
    if (!this.state.failed) return this.props.children
    return (
      <div
        role="alert"
        style={{
          maxWidth: '32rem',
          margin: 'var(--space-xl) auto',
          padding: 'var(--space-lg)',
          background: 'var(--color-surface)',
          border: '1px solid var(--color-border)',
          borderRadius: 'var(--radius)',
        }}
      >
        <h1 style={{ marginTop: 0, fontSize: 'var(--font-size-lg)' }}>Something went wrong</h1>
        <p style={{ color: 'var(--color-text-muted)' }}>
          The page could not be shown. Please reload to try again.
        </p>
        <button
          type="button"
          onClick={() => window.location.reload()}
          style={{
            padding: 'var(--space-sm) var(--space-md)',
            background: 'var(--color-accent)',
            color: 'var(--color-accent-text)',
            border: 0,
            borderRadius: 'var(--radius)',
            fontSize: 'var(--font-size-md)',
            cursor: 'pointer',
          }}
        >
          Reload page
        </button>
      </div>
    )
  }
}
