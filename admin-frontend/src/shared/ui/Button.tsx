import React from 'react'
import styled, { css } from 'styled-components'
import { motion } from 'framer-motion'
import { Spinner } from './Spinner'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size = 'sm' | 'md' | 'lg'

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  fullWidth?: boolean
  icon?: React.ReactNode
}

const variantStyles = {
  primary: css`
    background: ${({ theme }) => theme.colors.accent};
    color: ${({ theme }) => theme.colors.textInverse};
    &:hover:not(:disabled) {
      background: ${({ theme }) => theme.colors.accentHover};
    }
  `,
  secondary: css`
    background: ${({ theme }) => theme.colors.bgElevated};
    color: ${({ theme }) => theme.colors.textPrimary};
    border: 1px solid ${({ theme }) => theme.colors.border};
    &:hover:not(:disabled) {
      background: ${({ theme }) => theme.colors.border};
    }
  `,
  ghost: css`
    background: transparent;
    color: ${({ theme }) => theme.colors.textSecondary};
    &:hover:not(:disabled) {
      background: ${({ theme }) => theme.colors.bgElevated};
      color: ${({ theme }) => theme.colors.textPrimary};
    }
  `,
  danger: css`
    background: ${({ theme }) => theme.colors.error};
    color: #fff;
    &:hover:not(:disabled) {
      background: #DC2626;
    }
  `,
}

const sizeStyles = {
  sm: css`
    height: 32px;
    padding: 0 12px;
    font-size: ${({ theme }) => theme.typography.sizes.sm};
    border-radius: ${({ theme }) => theme.radii.sm};
    gap: 6px;
  `,
  md: css`
    height: 40px;
    padding: 0 16px;
    font-size: ${({ theme }) => theme.typography.sizes.sm};
    border-radius: ${({ theme }) => theme.radii.md};
    gap: 8px;
  `,
  lg: css`
    height: 48px;
    padding: 0 24px;
    font-size: ${({ theme }) => theme.typography.sizes.md};
    border-radius: ${({ theme }) => theme.radii.md};
    gap: 10px;
  `,
}

const StyledButton = styled(motion.button)<{
  $variant: Variant
  $size: Size
  $fullWidth: boolean
}>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: ${({ theme }) => theme.typography.weights.medium};
  transition: all ${({ theme }) => theme.transitions.fast};
  cursor: pointer;
  white-space: nowrap;
  width: ${({ $fullWidth }) => ($fullWidth ? '100%' : 'auto')};
  position: relative;
  outline: none;

  &:focus-visible {
    box-shadow: 0 0 0 2px ${({ theme }) => theme.colors.bg}, 0 0 0 4px ${({ theme }) => theme.colors.accent};
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  ${({ $variant }) => variantStyles[$variant]}
  ${({ $size }) => sizeStyles[$size]}
`

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  fullWidth = false,
  icon,
  children,
  disabled,
  ...props
}: ButtonProps) {
  return (
    <StyledButton
      $variant={variant}
      $size={size}
      $fullWidth={fullWidth}
      disabled={disabled || loading}
      whileTap={{ scale: 0.98 }}
      {...(props as React.ComponentPropsWithoutRef<typeof motion.button>)}
    >
      {loading ? (
        <Spinner size={size === 'sm' ? 14 : 16} />
      ) : (
        icon && <span style={{ display: 'flex' }}>{icon}</span>
      )}
      {children}
    </StyledButton>
  )
}
