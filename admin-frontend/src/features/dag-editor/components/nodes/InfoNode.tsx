import { memo } from 'react'
import { Handle, Position } from 'reactflow'
import type { NodeProps } from 'reactflow'
import { useTheme } from 'styled-components'
import styled from 'styled-components'
import { Info } from 'lucide-react'
import type { InfoNodeData } from '@shared/types/dag.types'

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
const ImagePreview = styled.div`
  width: 100%;
  height: 54px;
  background: ${({ theme }) => theme.colors.bgElevated};
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 10px;
  overflow: hidden;
  font-size: 22px;
`
const TitleText = styled.p`
  font-size: 13px;
  font-weight: 600;
  color: ${({ theme }) => theme.colors.textPrimary};
  margin-bottom: 5px;
`
const BodyText = styled.p`
  font-size: 11.5px;
  color: ${({ theme }) => theme.colors.textSecondary};
  line-height: 1.45;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
`

export const InfoNode = memo(function InfoNode({ data, selected }: NodeProps<InfoNodeData>) {
  const theme = useTheme()
  const accent = theme.colors.nodeInfo

  return (
    <>
      <Handle
        type="target"
        position={Position.Left}
        style={{ width: 12, height: 12, background: accent, border: '2px solid white', boxShadow: `0 0 0 2px ${accent}` }}
      />
      <Card $selected={!!selected} $accent={accent}>
        <Header $bg={accent}>
          <Info size={13} color="white" strokeWidth={2.5} />
          <HeaderLabel>Info Screen</HeaderLabel>
        </Header>
        <Body>
          <ImagePreview>
            {data.imageUrl ? (
              <img src={data.imageUrl} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
            ) : (
              '🌟'
            )}
          </ImagePreview>
          <TitleText>{data.title || 'Untitled'}</TitleText>
          {data.body && <BodyText>{data.body}</BodyText>}
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
