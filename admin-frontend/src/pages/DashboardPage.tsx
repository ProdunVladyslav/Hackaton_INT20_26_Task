import { useState } from 'react'
import styled from 'styled-components'
import { motion } from 'framer-motion'
import { Plus, LayoutGrid, Search } from 'lucide-react'
import { useNavigate } from '@tanstack/react-router'
import { Button } from '@shared/ui/Button'
import { Input } from '@shared/ui/Input'
import { AdminLayout } from '@/components/AdminLayout'
import { SurveyCard } from '@features/surveys/components/SurveyCard'
import { CreateSurveyModal } from '@features/surveys/components/CreateSurveyModal'
import { useSurveysStore } from '@features/surveys/store/surveys.store'
import toast from 'react-hot-toast'

const PageContent = styled.div`
  padding: 32px 40px;
  flex: 1;
  max-width: 1400px;
  width: 100%;
  margin: 0 auto;

  @media (max-width: ${({ theme }) => theme.breakpoints.mobile}) {
    padding: 16px;
  }
`

const PageHeader = styled.div`
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 32px;
  flex-wrap: wrap;
`

const TitleBlock = styled.div``

const PageTitle = styled.h1`
  font-size: ${({ theme }) => theme.typography.sizes.xxl};
  font-weight: ${({ theme }) => theme.typography.weights.bold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const PageSubtitle = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textSecondary};
  margin-top: 6px;
`

const Toolbar = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 24px;
  flex-wrap: wrap;
`

const SearchWrapper = styled.div`
  flex: 1;
  max-width: 320px;
`

const StatsRow = styled.div`
  display: flex;
  gap: 16px;
  margin-bottom: 32px;
  flex-wrap: wrap;
`

const StatCard = styled(motion.div)`
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 1px solid ${({ theme }) => theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.lg};
  padding: 20px 24px;
  min-width: 160px;
  flex: 1;
`

const StatValue = styled.div`
  font-size: ${({ theme }) => theme.typography.sizes.xxl};
  font-weight: ${({ theme }) => theme.typography.weights.bold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const StatLabel = styled.div`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textSecondary};
  margin-top: 4px;
`

const Grid = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
`

const EmptyState = styled(motion.div)`
  text-align: center;
  padding: 80px 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
`

const EmptyIcon = styled.div`
  width: 64px;
  height: 64px;
  background: ${({ theme }) => theme.colors.bgElevated};
  border-radius: ${({ theme }) => theme.radii.xl};
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  margin-bottom: 8px;
`

const EmptyTitle = styled.h3`
  font-size: ${({ theme }) => theme.typography.sizes.lg};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const EmptyDesc = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textSecondary};
  max-width: 300px;
`

export function DashboardPage() {
  const navigate = useNavigate()
  const [createOpen, setCreateOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')

  const surveys = useSurveysStore((s) => s.surveys)
  const deleteSurvey = useSurveysStore((s) => s.deleteSurvey)

  const filtered = surveys.filter(
    (s) =>
      s.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      s.description?.toLowerCase().includes(searchQuery.toLowerCase())
  )

  const totalCompletions = surveys.reduce((acc, s) => acc + s.completionCount, 0)
  const published = surveys.filter((s) => s.status === 'published').length

  const handleEdit = (id: string) => {
    navigate({ to: '/editor/$surveyId', params: { surveyId: id } })
  }

  const handleDelete = (id: string) => {
    deleteSurvey(id)
    toast.success('Survey deleted')
  }

  const handleCopyLink = (id: string) => {
    const url = `${window.location.origin}/survey/${id}`
    navigator.clipboard.writeText(url).then(() => toast.success('Link copied to clipboard!'))
  }

  return (
    <AdminLayout>
      <PageContent>
        <PageHeader>
          <TitleBlock>
            <PageTitle>Surveys</PageTitle>
            <PageSubtitle>Manage your wellness funnels and DAG editors</PageSubtitle>
          </TitleBlock>
          <Button
            size="lg"
            icon={<Plus size={18} />}
            onClick={() => setCreateOpen(true)}
          >
            Create Survey
          </Button>
        </PageHeader>

        <StatsRow>
          {[
            { label: 'Total surveys', value: surveys.length },
            { label: 'Published', value: published },
            { label: 'Total completions', value: totalCompletions.toLocaleString() },
          ].map((stat, i) => (
            <StatCard
              key={stat.label}
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.05 }}
            >
              <StatValue>{stat.value}</StatValue>
              <StatLabel>{stat.label}</StatLabel>
            </StatCard>
          ))}
        </StatsRow>

        <Toolbar>
          <SearchWrapper>
            <Input
              placeholder="Search surveys…"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              leftIcon={<Search size={15} />}
            />
          </SearchWrapper>
        </Toolbar>

        {filtered.length === 0 ? (
          <EmptyState
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
          >
            <EmptyIcon>
              <LayoutGrid size={28} color="#A1A1AA" />
            </EmptyIcon>
            <EmptyTitle>
              {searchQuery ? 'No surveys found' : 'No surveys yet'}
            </EmptyTitle>
            <EmptyDesc>
              {searchQuery
                ? 'Try a different search term'
                : 'Create your first survey to get started with the DAG editor.'}
            </EmptyDesc>
            {!searchQuery && (
              <Button icon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
                Create Survey
              </Button>
            )}
          </EmptyState>
        ) : (
          <Grid>
            {filtered.map((survey, i) => (
              <SurveyCard
                key={survey.id}
                survey={survey}
                index={i}
                onEdit={handleEdit}
                onDelete={handleDelete}
                onCopyLink={handleCopyLink}
              />
            ))}
          </Grid>
        )}

        <CreateSurveyModal open={createOpen} onClose={() => setCreateOpen(false)} />
      </PageContent>
    </AdminLayout>
  )
}
