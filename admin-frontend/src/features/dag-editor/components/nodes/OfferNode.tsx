import { memo } from 'react'
import { Handle, Position } from 'reactflow'
import type { NodeProps } from 'reactflow'
import styled from 'styled-components'
import { Gift } from 'lucide-react'
import type { OfferNodeData } from '@shared/types/dag.types'

const NodeCard = styled.div<{ $selected: boolean }>`
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 2px solid ${({ $selected, theme }) =>
    $selected ? theme.colors.nodeOffer : theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.lg};
  min-width: 220px;
  max-width: 260px;
  overflow: hidden;
  box-shadow: ${({ $selected, theme }) =>
    $selected ? `0 0 0 4px ${theme.colors.nodeOffer}22` : theme.shadows.sm};
  transition: all 0.15s ease;
  cursor: pointer;

  &:hover {
    border-color: ${({ theme }) => theme.colors.nodeOffer};
    box-shadow: ${({ theme }) => `0 0 0 3px ${theme.colors.nodeOffer}18`};
  }
`

const NodeHeader = styled.div`
  background: ${({ theme }) => theme.colors.nodeOffer};
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

const HeadlineText = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const Price = styled.span`
  display: inline-block;
  margin-top: 6px;
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.bold};
  color: ${({ theme }) => theme.colors.nodeOffer};
`

const CtaText = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  color: ${({ theme }) => theme.colors.textTertiary};
  margin-top: 2px;
`

const StyledHandle = styled(Handle)`
  width: 10px !important;
  height: 10px !important;
  background: ${({ theme }) => theme.colors.nodeOffer} !important;
  border: 2px solid white !important;
`

export const OfferNode = memo(function OfferNode({
  data,
  selected,
}: NodeProps<OfferNodeData>) {
  return (
    <NodeCard $selected={!!selected}>
      <StyledHandle type="target" position={Position.Left} />
      <NodeHeader>
        <Gift size={13} color="white" />
        <HeaderLabel>Offer</HeaderLabel>
      </NodeHeader>
      <NodeBody>
        <HeadlineText>{data.headline || 'Untitled offer'}</HeadlineText>
        {data.price !== undefined && <Price>${data.price.toFixed(2)}</Price>}
        {data.ctaText && <CtaText>CTA: {data.ctaText}</CtaText>}
      </NodeBody>
      <StyledHandle type="source" position={Position.Right} />
    </NodeCard>
  )
})
