import { memo } from 'react'
import { Handle, Position } from 'reactflow'
import type { NodeProps } from 'reactflow'
import styled from 'styled-components'
import { HelpCircle } from 'lucide-react'
import type { QuestionNodeData } from '@shared/types/dag.types'

const NodeCard = styled.div<{ $selected: boolean }>`
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 2px solid ${({ $selected, theme }) =>
    $selected ? theme.colors.nodeQuestion : theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.lg};
  min-width: 220px;
  max-width: 260px;
  overflow: hidden;
  box-shadow: ${({ $selected, theme }) =>
    $selected ? `0 0 0 4px ${theme.colors.nodeQuestion}22` : theme.shadows.sm};
  transition: all 0.15s ease;
  cursor: pointer;

  &:hover {
    border-color: ${({ theme }) => theme.colors.nodeQuestion};
    box-shadow: ${({ theme }) => `0 0 0 3px ${theme.colors.nodeQuestion}18`};
  }
`

const NodeHeader = styled.div`
  background: ${({ theme }) => theme.colors.nodeQuestion};
  padding: 8px 12px;
  display: flex;
  align-items: center;
  gap: 6px;
`

const HeaderLabel = styled.span`
  font-size: 11px;
  font-weight: 600;
  color: white;
  text-transform: uppercase;
  letter-spacing: 0.5px;
`

const NodeBody = styled.div`
  padding: 12px;
`

const QuestionText = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.medium};
  color: ${({ theme }) => theme.colors.textPrimary};
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
`

const OptionCount = styled.span`
  display: inline-block;
  margin-top: 8px;
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.textTertiary};
`

const StyledHandle = styled(Handle)`
  width: 10px !important;
  height: 10px !important;
  background: ${({ theme }) => theme.colors.nodeQuestion} !important;
  border: 2px solid white !important;
`

export const QuestionNode = memo(function QuestionNode({
  data,
  selected,
}: NodeProps<QuestionNodeData>) {
  return (
    <NodeCard $selected={!!selected}>
      <StyledHandle type="target" position={Position.Left} />
      <NodeHeader>
        <HelpCircle size={13} color="white" />
        <HeaderLabel>Question</HeaderLabel>
      </NodeHeader>
      <NodeBody>
        <QuestionText>
          {data.questionText || 'Untitled question'}
        </QuestionText>
        <OptionCount>
          {data.options.length} option{data.options.length !== 1 ? 's' : ''} · {data.answerType.replace('_', ' ')}
        </OptionCount>
      </NodeBody>
      <StyledHandle type="source" position={Position.Right} />
    </NodeCard>
  )
})
