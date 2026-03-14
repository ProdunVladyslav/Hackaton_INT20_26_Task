import React from 'react'
import styled from 'styled-components'
import { AlertTriangle } from 'lucide-react'

interface ErrorBoundaryState {
  hasError: boolean
  error: Error | null
}

const ErrorPage = styled.div`
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 16px;
  padding: 24px;
  text-align: center;
`

const Title = styled.h2`
  font-size: 24px;
  font-weight: 600;
  color: #18181b;
`

const Message = styled.p`
  font-size: 14px;
  color: #71717a;
  max-width: 400px;
`

const ReloadBtn = styled.button`
  padding: 10px 20px;
  background: #6366f1;
  color: white;
  border: none;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  margin-top: 8px;
`

export class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  ErrorBoundaryState
> {
  constructor(props: { children: React.ReactNode }) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error('[ErrorBoundary]', error, info)
  }

  render() {
    if (this.state.hasError) {
      return (
        <ErrorPage>
          <AlertTriangle size={40} color="#EF4444" />
          <Title>Something went wrong</Title>
          <Message>
            {this.state.error?.message ?? 'An unexpected error occurred. Please reload the page.'}
          </Message>
          <ReloadBtn onClick={() => window.location.reload()}>Reload page</ReloadBtn>
        </ErrorPage>
      )
    }
    return this.props.children
  }
}
