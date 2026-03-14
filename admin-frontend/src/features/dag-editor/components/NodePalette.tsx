import styled from 'styled-components'
import { HelpCircle, Info, Gift } from 'lucide-react'
import type { DragEvent } from 'react'
import { NodeType } from '@shared/types/dag.types'

const Panel = styled.aside`
  width: 200px;
  background: ${({ theme }) => theme.colors.bgSurface};
  border-right: 1px solid ${({ theme }) => theme.colors.border};
  padding: 16px 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  overflow-y: auto;
  flex-shrink: 0;
`

const SectionTitle = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textTertiary};
  text-transform: uppercase;
  letter-spacing: 0.8px;
  padding: 4px 4px 8px;
`

const NodeBlock = styled.div<{ $color: string }>`
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  background: ${({ theme }) => theme.colors.bgElevated};
  border: 1px solid ${({ theme }) => theme.colors.border};
  border-left: 3px solid ${({ $color }) => $color};
  border-radius: ${({ theme }) => theme.radii.md};
  cursor: grab;
  transition: all 0.15s ease;
  user-select: none;

  &:hover {
    background: ${({ theme }) => theme.colors.border};
    transform: translateX(2px);
  }

  &:active {
    cursor: grabbing;
  }
`

const BlockLabel = styled.span`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.medium};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const BlockDesc = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.textTertiary};
  margin-top: 2px;
`

const BlockContent = styled.div``

const nodeTypes = [
  {
    type: NodeType.Question,
    label: 'Question',
    desc: 'Collect data',
    icon: <HelpCircle size={16} />,
    color: '#6366F1',
  },
  {
    type: NodeType.Info,
    label: 'Info',
    desc: 'Show content',
    icon: <Info size={16} />,
    color: '#10B981',
  },
  {
    type: NodeType.Offer,
    label: 'Offer',
    desc: 'Present offer',
    icon: <Gift size={16} />,
    color: '#F59E0B',
  },
]

interface NodePaletteProps {
  onDragStart: (e: DragEvent, type: NodeType) => void
}

export function NodePalette({ onDragStart }: NodePaletteProps) {
  return (
    <Panel>
      <SectionTitle>Palette</SectionTitle>
      {nodeTypes.map(({ type, label, desc, icon, color }) => (
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
    </Panel>
  )
}
