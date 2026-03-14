import { memo } from 'react'
import { Handle, Position } from 'reactflow'
import type { NodeProps } from 'reactflow'
import styled from 'styled-components'
import { Info } from 'lucide-react'
import type { InfoNodeData } from '@shared/types/dag.types'

const NodeCard = styled.div<{ $selected: boolean }>`
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 2px solid ${({ $selected, theme }) =>
    $selected ? theme.colors.nodeInfo : theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.lg};
  min-width: 220px;
  max-width: 260px;
  overflow: hidden;
  box-shadow: ${({ $selected, theme }) =>
    $selected ? `0 0 0 4px ${theme.colors.nodeInfo}22` : theme.shadows.sm};
  transition: all 0.15s ease;
  cursor: pointer;

  &:hover {
    border-color: ${({ theme }) => theme.colors.nodeInfo};
    box-shadow: ${({ theme }) => `0 0 0 3px ${theme.colors.nodeInfo}18`};
  }
`

const NodeHeader = styled.div`
  background: ${({ theme }) => theme.colors.nodeInfo};
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

const TitleText = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const BodyText = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.textSecondary};
  margin-top: 4px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-height: 1.4;
`

const StyledHandle = styled(Handle)`
  width: 10px !important;
  height: 10px !important;
  background: ${({ theme }) => theme.colors.nodeInfo} !important;
  border: 2px solid white !important;
`

export const InfoNode = memo(function InfoNode({
  data,
  selected,
}: NodeProps<InfoNodeData>) {
  return (
    <NodeCard $selected={!!selected}>
      <StyledHandle type="target" position={Position.Left} />
      <NodeHeader>
        <Info size={13} color="white" />
        <HeaderLabel>Info</HeaderLabel>
      </NodeHeader>
      <NodeBody>
        <TitleText>{data.title || 'Untitled'}</TitleText>
        {data.body && <BodyText>{data.body}</BodyText>}
      </NodeBody>
      <StyledHandle type="source" position={Position.Right} />
    </NodeCard>
  )
})
