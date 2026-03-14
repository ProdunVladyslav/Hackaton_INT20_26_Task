import React from 'react'
import styled from 'styled-components'

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  hint?: string
  leftIcon?: React.ReactNode
  rightIcon?: React.ReactNode
}

const Wrapper = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
`

const Label = styled.label`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.medium};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const InputWrapper = styled.div`
  position: relative;
  display: flex;
  align-items: center;
`

const IconLeft = styled.span`
  position: absolute;
  left: 12px;
  color: ${({ theme }) => theme.colors.textTertiary};
  display: flex;
  pointer-events: none;
`

const IconRight = styled.span`
  position: absolute;
  right: 12px;
  color: ${({ theme }) => theme.colors.textTertiary};
  display: flex;
`

const StyledInput = styled.input<{ $hasLeft: boolean; $hasRight: boolean; $error: boolean }>`
  width: 100%;
  height: 40px;
  padding: 0 ${({ $hasRight }) => ($hasRight ? '40px' : '14px')} 0 ${({ $hasLeft }) => ($hasLeft ? '40px' : '14px')};
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 1px solid ${({ $error, theme }) => ($error ? theme.colors.error : theme.colors.border)};
  border-radius: ${({ theme }) => theme.radii.md};
  color: ${({ theme }) => theme.colors.textPrimary};
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  transition: border-color ${({ theme }) => theme.transitions.fast},
              box-shadow ${({ theme }) => theme.transitions.fast};

  &::placeholder {
    color: ${({ theme }) => theme.colors.textTertiary};
  }

  &:focus {
    outline: none;
    border-color: ${({ $error, theme }) => ($error ? theme.colors.error : theme.colors.borderFocus)};
    box-shadow: 0 0 0 3px ${({ $error, theme }) =>
      $error ? `${theme.colors.error}22` : `${theme.colors.accent}22`};
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`

const ErrorText = styled.span`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.error};
`

const HintText = styled.span`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.textTertiary};
`

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, hint, leftIcon, rightIcon, id, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

    return (
      <Wrapper>
        {label && <Label htmlFor={inputId}>{label}</Label>}
        <InputWrapper>
          {leftIcon && <IconLeft>{leftIcon}</IconLeft>}
          <StyledInput
            ref={ref}
            id={inputId}
            $hasLeft={!!leftIcon}
            $hasRight={!!rightIcon}
            $error={!!error}
            aria-invalid={!!error}
            aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
            {...props}
          />
          {rightIcon && <IconRight>{rightIcon}</IconRight>}
        </InputWrapper>
        {error && <ErrorText id={`${inputId}-error`}>{error}</ErrorText>}
        {!error && hint && <HintText id={`${inputId}-hint`}>{hint}</HintText>}
      </Wrapper>
    )
  }
)

Input.displayName = 'Input'
