import { memo } from 'react'
import { Handle, Position } from 'reactflow'
import type { NodeProps } from 'reactflow'
import { useTheme } from 'styled-components'
import styled from 'styled-components'
import { Gift, DollarSign } from 'lucide-react'
import type { OfferNodeData } from '@shared/types/dag.types'

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
  padding: 12px 13px;
`
const HeadlineText = styled.p`
  font-size: 13px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
  margin-bottom: 5px;
`
const DescText = styled.p`
  font-size: 11.5px;
  color: ${({ theme }) => theme.colors.textSecondary};
  line-height: 1.4;
  margin-bottom: 10px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
`
const PriceRow = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 8px;
  border-top: 1px solid ${({ theme }) => theme.colors.border};
`
const PriceTag = styled.span<{ $color: string }>`
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: 15px;
  font-weight: 700;
  color: ${({ $color }) => $color};
`
const CtaChip = styled.span<{ $color: string }>`
  font-size: 10px;
  font-weight: 600;
  background: ${({ $color }) => $color};
  color: white;
  padding: 3px 9px;
  border-radius: 20px;
`
const TerminalBadge = styled.div`
  display: flex;
  align-items: center;
  gap: 4px;
  margin-top: 6px;
  font-size: 9.5px;
  color: ${({ theme }) => theme.colors.textTertiary};
  text-transform: uppercase;
  letter-spacing: 0.5px;
`
const Dot = styled.span<{ $color: string }>`
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: ${({ $color }) => $color};
  display: inline-block;
`

export const OfferNode = memo(function OfferNode({ data, selected }: NodeProps<OfferNodeData>) {
  const theme = useTheme()
  const accent = theme.colors.nodeOffer

  return (
    <>
      <Handle
        type="target"
        position={Position.Left}
        style={{ width: 12, height: 12, background: accent, border: '2px solid white', boxShadow: `0 0 0 2px ${accent}` }}
      />
      <Card $selected={!!selected} $accent={accent}>
        <Header $bg={accent}>
          <Gift size={13} color="white" strokeWidth={2.5} />
          <HeaderLabel>Offer</HeaderLabel>
        </Header>
        <Body>
          <HeadlineText>{data.headline || 'Untitled offer'}</HeadlineText>
          {data.description && <DescText>{data.description}</DescText>}
          <PriceRow>
            {data.price !== undefined ? (
              <PriceTag $color={accent}>
                <DollarSign size={13} />
                {data.price.toFixed(2)}
              </PriceTag>
            ) : (
              <span />
            )}
            <CtaChip $color={accent}>{data.ctaText || 'Get Started'}</CtaChip>
          </PriceRow>
          <TerminalBadge>
            <Dot $color={accent} /> Terminal node
          </TerminalBadge>
        </Body>
      </Card>
      {/* Offer nodes have no source handle — they're terminal */}
    </>
  )
})
