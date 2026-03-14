import styled, { keyframes } from 'styled-components'

const spin = keyframes`
  to { transform: rotate(360deg); }
`

const Ring = styled.div<{ $size: number; $color?: string }>`
  width: ${({ $size }) => $size}px;
  height: ${({ $size }) => $size}px;
  border: 2px solid transparent;
  border-top-color: ${({ $color, theme }) => $color ?? theme.colors.textInverse};
  border-radius: 50%;
  animation: ${spin} 0.7s linear infinite;
  flex-shrink: 0;
`

interface SpinnerProps {
  size?: number
  color?: string
}

export function Spinner({ size = 20, color }: SpinnerProps) {
  return <Ring $size={size} $color={color} />
}
