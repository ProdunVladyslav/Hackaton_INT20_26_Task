import styled from 'styled-components'
import { Sun, Moon, Monitor } from 'lucide-react'
import { useAppTheme } from '@shared/theme/ThemeProvider'

const Wrapper = styled.div`
  display: flex;
  align-items: center;
  background: ${({ theme }) => theme.colors.bgElevated};
  border: 1px solid ${({ theme }) => theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.full};
  padding: 4px;
  gap: 2px;
`

const Option = styled.button<{ $active: boolean }>`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: ${({ theme }) => theme.radii.full};
  background: ${({ $active, theme }) => ($active ? theme.colors.bgSurface : 'transparent')};
  color: ${({ $active, theme }) => ($active ? theme.colors.textPrimary : theme.colors.textTertiary)};
  border: none;
  cursor: pointer;
  transition: all ${({ theme }) => theme.transitions.fast};
  box-shadow: ${({ $active, theme }) => ($active ? theme.shadows.sm : 'none')};

  &:hover {
    color: ${({ theme }) => theme.colors.textPrimary};
  }
`

export function ThemeSwitcher() {
  const { mode, setMode } = useAppTheme()

  return (
    <Wrapper>
      <Option $active={mode === 'light'} onClick={() => setMode('light')} title="Light">
        <Sun size={14} />
      </Option>
      <Option $active={mode === 'dark'} onClick={() => setMode('dark')} title="Dark">
        <Moon size={14} />
      </Option>
      <Option $active={mode === 'system'} onClick={() => setMode('system')} title="System">
        <Monitor size={14} />
      </Option>
    </Wrapper>
  )
}
