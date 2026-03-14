import { memo } from 'react'
import { Handle, Position } from 'reactflow'
import type { NodeProps } from 'reactflow'
import { useTheme } from 'styled-components'
import styled from 'styled-components'
import { HelpCircle } from 'lucide-react'
import type { QuestionNodeData } from '@shared/types/dag.types'

const Card = styled.div<{ $selected: boolean; $accent: string }>`
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 2px solid ${({ $selected, $accent, theme }) => ($selected ? $accent : theme.colors.border)};
  border-radius: 12px;
  min-width: 230px;
  max-width: 270px;
  overflow: visible;
  box-shadow: ${({ $selected, $accent, theme }) =>
    $selected ? `0 0 0 4px ${$accent}22, ${theme.shadows.md}` : theme.shadows.sm};
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
  cursor: pointer;
  &:hover {
    border-color: ${({ $accent }) => $accent};
    box-shadow: ${({ $accent, theme }) => `0 0 0 3px ${$accent}18, ${theme.shadows.md}`};
  }
`
const Header = styled.div<{ $bg: string }>`
  background: ${({ $bg }) => $bg};
  padding: 9px 13px;
  display: flex;
  align-items: center;
  gap: 7px;
  border-radius: 10px 10px 0 0;
`
const HeaderLabel = styled.span`
  font-size: 10px;
  font-weight: 700;
  color: rgba(255,255,255,0.95);
  text-transform: uppercase;
  letter-spacing: 0.8px;
`
const Body = styled.div`
  padding: 11px 13px 10px;
`
const QuestionText = styled.p`
  font-size: 13px;
  font-weight: 600;
  color: ${({ theme }) => theme.colors.textPrimary};
  line-height: 1.4;
  margin-bottom: 10px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
`
const OptionsGrid = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-bottom: 9px;
`
const OptionChip = styled.span<{ $color: string }>`
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 8px;
  background: ${({ $color }) => $color}15;
  border: 1px solid ${({ $color }) => $color}35;
  border-radius: 20px;
  font-size: 10.5px;
  font-weight: 500;
  color: ${({ theme }) => theme.colors.textPrimary};
  white-space: nowrap;
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
`
const Footer = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 8px;
  border-top: 1px solid ${({ theme }) => theme.colors.border};
`
const AttrBadge = styled.code`
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textTertiary};
  background: ${({ theme }) => theme.colors.bgElevated};
  padding: 1px 5px;
  border-radius: 4px;
`
const TypeLabel = styled.span`
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textTertiary};
  text-transform: capitalize;
`

export const QuestionNode = memo(function QuestionNode({ data, selected }: NodeProps<QuestionNodeData>) {
  const theme = useTheme()
  const accent = theme.colors.nodeQuestion
  const visible = data.options.slice(0, 4)
  const extra = data.options.length - 4

  return (
    <>
      <Handle
        type="target"
        position={Position.Left}
        style={{ width: 12, height: 12, background: accent, border: '2px solid white', boxShadow: `0 0 0 2px ${accent}` }}
      />
      <Card $selected={!!selected} $accent={accent}>
        <Header $bg={accent}>
          <HelpCircle size={13} color="white" strokeWidth={2.5} />
          <HeaderLabel>Question</HeaderLabel>
        </Header>
        <Body>
          <QuestionText>{data.questionText || 'Untitled question'}</QuestionText>
          {data.options.length > 0 && (
            <OptionsGrid>
              {visible.map((opt) => (
                <OptionChip key={opt.id} $color={accent} title={opt.label}>
                  {opt.icon && <span>{opt.icon}</span>}
                  {opt.label}
                </OptionChip>
              ))}
              {extra > 0 && <OptionChip $color={accent}>+{extra} more</OptionChip>}
            </OptionsGrid>
          )}
          <Footer>
            <AttrBadge>@{data.attribute}</AttrBadge>
            <TypeLabel>{data.answerType.replace(/_/g, ' ')}</TypeLabel>
          </Footer>
        </Body>
      </Card>
      <Handle
        type="source"
        position={Position.Right}
        style={{ width: 12, height: 12, background: accent, border: '2px solid white', boxShadow: `0 0 0 2px ${accent}` }}
      />
    </>
  )
})
