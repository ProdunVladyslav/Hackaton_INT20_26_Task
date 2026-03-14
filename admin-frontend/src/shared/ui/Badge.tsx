import styled, { css } from 'styled-components'

type BadgeVariant = 'success' | 'warning' | 'error' | 'info' | 'neutral' | 'accent'

const variantMap: Record<BadgeVariant, ReturnType<typeof css>> = {
  success: css`
    background: ${({ theme }) => theme.colors.successLight};
    color: ${({ theme }) => theme.colors.success};
  `,
  warning: css`
    background: ${({ theme }) => theme.colors.warningLight};
    color: ${({ theme }) => theme.colors.warning};
  `,
  error: css`
    background: ${({ theme }) => theme.colors.errorLight};
    color: ${({ theme }) => theme.colors.error};
  `,
  info: css`
    background: ${({ theme }) => theme.colors.infoLight};
    color: ${({ theme }) => theme.colors.info};
  `,
  neutral: css`
    background: ${({ theme }) => theme.colors.bgElevated};
    color: ${({ theme }) => theme.colors.textSecondary};
  `,
  accent: css`
    background: ${({ theme }) => theme.colors.accentLight};
    color: ${({ theme }) => theme.colors.accentText};
  `,
}

export const Badge = styled.span<{ $variant?: BadgeVariant }>`
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  border-radius: ${({ theme }) => theme.radii.full};
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  font-weight: ${({ theme }) => theme.typography.weights.medium};
  white-space: nowrap;
  ${({ $variant = 'neutral' }) => variantMap[$variant]}
`
