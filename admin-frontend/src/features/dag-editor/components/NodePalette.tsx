import styled from 'styled-components'
import { HelpCircle, Info, Gift, MousePointer2, ArrowRight } from 'lucide-react'
import type { DragEvent } from 'react'
import { NodeType } from '@shared/types/dag.types'

const Panel = styled.aside`
  width: 210px;
  background: ${({ theme }) => theme.colors.bgSurface};
  border-right: 1px solid ${({ theme }) => theme.colors.border};
  padding: 14px 12px 20px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  overflow-y: auto;
  flex-shrink: 0;
`

const SectionTitle = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textTertiary};
  text-transform: uppercase;
  letter-spacing: 0.8px;
  padding: 2px 4px 10px;
`

const NodeBlock = styled.div<{ $color: string }>`
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 11px;
  background: ${({ theme }) => theme.colors.bgElevated};
  border: 1px solid ${({ theme }) => theme.colors.border};
  border-left: 3px solid ${({ $color }) => $color};
  border-radius: ${({ theme }) => theme.radii.md};
  cursor: grab;
  transition: all 0.15s ease;
  user-select: none;

  &:hover {
    background: ${({ $color }) => $color}0d;
    border-color: ${({ $color }) => $color}50;
    transform: translateX(3px);
    box-shadow: -2px 0 0 ${({ $color }) => $color};
  }
  &:active {
    cursor: grabbing;
    opacity: 0.85;
  }
`

const BlockLabel = styled.span`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const BlockDesc = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.textTertiary};
  margin-top: 1px;
`

const BlockContent = styled.div``

const HintBox = styled.div`
  margin-top: 16px;
  padding: 10px 12px;
  background: ${({ theme }) => theme.colors.bgElevated};
  border-radius: ${({ theme }) => theme.radii.md};
  border: 1px dashed ${({ theme }) => theme.colors.border};
`

const HintTitle = styled.p`
  font-size: 10.5px;
  font-weight: 600;
  color: ${({ theme }) => theme.colors.textSecondary};
  margin-bottom: 8px;
`

const HintItem = styled.div`
  display: flex;
  align-items: flex-start;
  gap: 7px;
  margin-bottom: 7px;
  font-size: 10.5px;
  color: ${({ theme }) => theme.colors.textTertiary};
  line-height: 1.4;

  svg {
    flex-shrink: 0;
    margin-top: 1px;
  }
`

const nodeItems = [
  {
    type: NodeType.Question,
    label: 'Question',
    desc: 'Collect user data',
    icon: <HelpCircle size={15} />,
    color: '#6366F1',
  },
  {
    type: NodeType.Info,
    label: 'Info Screen',
    desc: 'Motivate & engage',
    icon: <Info size={15} />,
    color: '#10B981',
  },
  {
    type: NodeType.Offer,
    label: 'Offer',
    desc: 'Terminal / CTA',
    icon: <Gift size={15} />,
    color: '#F59E0B',
  },
]

interface NodePaletteProps {
  onDragStart: (e: DragEvent, type: NodeType) => void
}

export function NodePalette({ onDragStart }: NodePaletteProps) {
  return (
    <Panel>
      <SectionTitle>Node Palette</SectionTitle>
      {nodeItems.map(({ type, label, desc, icon, color }) => (
        <NodeBlock
          key={type}
          $color={color}
          draggable
          onDragStart={(e) => onDragStart(e, type)}
        >
          <span style={{ color, display: 'flex' }}>{icon}</span>
          <BlockContent>
            <BlockLabel>{label}</BlockLabel>
            <BlockDesc>{desc}</BlockDesc>
          </BlockContent>
        </NodeBlock>
      ))}

      <HintBox>
        <HintTitle>How to use</HintTitle>
        <HintItem>
          <MousePointer2 size={12} color="#6366F1" />
          Drag a node type onto the canvas
        </HintItem>
        <HintItem>
          <ArrowRight size={12} color="#10B981" />
          Connect nodes: drag from the right ● handle to another node's left ● handle
        </HintItem>
        <HintItem>
          <HelpCircle size={12} color="#F59E0B" />
          Click any node or edge to edit its properties
        </HintItem>
      </HintBox>
    </Panel>
  )
}
