import { Component, type ReactNode } from 'react'

type Props = { children: ReactNode }
type State = { failed: boolean }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { failed: false }

  // React 19 already logs caught render errors (createRoot onCaughtError).
  static getDerivedStateFromError(): State {
    return { failed: true }
  }


  render(): ReactNode {
    if (!this.state.failed) return this.props.children
    return (
      <div role="alert" className="card" style={{ maxWidth: '32rem', margin: 'var(--space-xl) auto' }}>
        <h1 style={{ marginTop: 0, fontSize: 'var(--font-size-lg)' }}>Something went wrong</h1>
        <p style={{ color: 'var(--color-text-muted)' }}>
          The page could not be shown. Please reload to try again.
        </p>
        <button type="button" className="btn" onClick={() => window.location.reload()}>
          Reload page
        </button>
      </div>
    )
  }
}
