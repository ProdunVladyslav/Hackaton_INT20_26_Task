import styled from 'styled-components'
import { motion } from 'framer-motion'
import { Edit2, Trash2, Copy, BarChart2, Clock, CheckCircle, Circle } from 'lucide-react'
import { Badge } from '@shared/ui/Badge'
import { Button } from '@shared/ui/Button'
import type { Survey } from '@shared/types/dag.types'
import { SurveyStatus } from '@shared/types/dag.types'
import { formatDate } from '@shared/utils/format'

interface SurveyCardProps {
  survey: Survey
  onEdit: (id: string) => void
  onDelete: (id: string) => void
  onCopyLink: (id: string) => void
  index: number
}

const Card = styled(motion.article)`
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 1px solid ${({ theme }) => theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.lg};
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  transition: border-color ${({ theme }) => theme.transitions.fast},
              box-shadow ${({ theme }) => theme.transitions.fast};
  cursor: default;

  &:hover {
    border-color: ${({ theme }) => theme.colors.borderHover};
    box-shadow: ${({ theme }) => theme.shadows.md};
  }
`

const Header = styled.div`
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
`

const TitleBlock = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
  min-width: 0;
`

const Title = styled.h3`
  font-size: ${({ theme }) => theme.typography.sizes.md};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
`

const Description = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textSecondary};
  line-height: ${({ theme }) => theme.typography.lineHeights.normal};
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
`

const Meta = styled.div`
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
`

const MetaItem = styled.div`
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textSecondary};
`

const Actions = styled.div`
  display: flex;
  align-items: center;
  gap: 4px;
  padding-top: 12px;
  border-top: 1px solid ${({ theme }) => theme.colors.border};
`

const Spacer = styled.div`
  flex: 1;
`

function statusVariant(status: SurveyStatus) {
  switch (status) {
    case SurveyStatus.Published: return 'success'
    case SurveyStatus.Draft: return 'neutral'
    case SurveyStatus.Archived: return 'warning'
  }
}

function statusIcon(status: SurveyStatus) {
  switch (status) {
    case SurveyStatus.Published: return <CheckCircle size={11} />
    case SurveyStatus.Draft: return <Circle size={11} />
    case SurveyStatus.Archived: return <Clock size={11} />
  }
}

export function SurveyCard({ survey, onEdit, onDelete, onCopyLink, index }: SurveyCardProps) {
  return (
    <Card
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.06, duration: 0.35, ease: 'easeOut' }}
    >
      <Header>
        <TitleBlock>
          <Title title={survey.title}>{survey.title}</Title>
          {survey.description && <Description>{survey.description}</Description>}
        </TitleBlock>
        <Badge $variant={statusVariant(survey.status)}>
          {statusIcon(survey.status)}
          {survey.status.charAt(0).toUpperCase() + survey.status.slice(1)}
        </Badge>
      </Header>

      <Meta>
        <MetaItem>
          <BarChart2 size={13} />
          {survey.completionCount.toLocaleString()} completions
        </MetaItem>
        <MetaItem>
          <Clock size={13} />
          {formatDate(survey.updatedAt)}
        </MetaItem>
      </Meta>

      <Actions>
        <Button
          variant="ghost"
          size="sm"
          icon={<Edit2 size={14} />}
          onClick={() => onEdit(survey.id)}
        >
          Edit
        </Button>
        <Button
          variant="ghost"
          size="sm"
          icon={<Copy size={14} />}
          onClick={() => onCopyLink(survey.id)}
        >
          Copy Link
        </Button>
        <Spacer />
        <Button
          variant="ghost"
          size="sm"
          icon={<Trash2 size={14} />}
          onClick={() => onDelete(survey.id)}
          style={{ color: '#EF4444' }}
        >
          Delete
        </Button>
      </Actions>
    </Card>
  )
}
