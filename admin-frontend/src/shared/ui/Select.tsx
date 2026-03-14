import React from 'react'
import styled from 'styled-components'
import { ChevronDown } from 'lucide-react'

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  options: { value: string; label: string }[]
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

const SelectWrapper = styled.div`
  position: relative;
`

const StyledSelect = styled.select<{ $error: boolean }>`
  width: 100%;
  height: 40px;
  padding: 0 40px 0 14px;
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 1px solid ${({ $error, theme }) => ($error ? theme.colors.error : theme.colors.border)};
  border-radius: ${({ theme }) => theme.radii.md};
  color: ${({ theme }) => theme.colors.textPrimary};
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-family: inherit;
  appearance: none;
  cursor: pointer;
  transition: border-color ${({ theme }) => theme.transitions.fast};

  &:focus {
    outline: none;
    border-color: ${({ theme }) => theme.colors.borderFocus};
    box-shadow: 0 0 0 3px ${({ theme }) => `${theme.colors.accent}22`};
  }
`

const Arrow = styled.span`
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  color: ${({ theme }) => theme.colors.textTertiary};
  pointer-events: none;
  display: flex;
`

const ErrorText = styled.span`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.error};
`

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, options, id, ...props }, ref) => {
    const selectId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

    return (
      <Wrapper>
        {label && <Label htmlFor={selectId}>{label}</Label>}
        <SelectWrapper>
          <StyledSelect ref={ref} id={selectId} $error={!!error} {...props}>
            {options.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </StyledSelect>
          <Arrow>
            <ChevronDown size={16} />
          </Arrow>
        </SelectWrapper>
        {error && <ErrorText>{error}</ErrorText>}
      </Wrapper>
    )
  }
)

Select.displayName = 'Select'
