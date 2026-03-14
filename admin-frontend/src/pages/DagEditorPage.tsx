import { useEffect } from 'react'
import styled from 'styled-components'
import { useParams, useNavigate, Link } from '@tanstack/react-router'
import { ReactFlowProvider } from 'reactflow'
import { ArrowLeft, Save, Eye, Circle } from 'lucide-react'
import { Button } from '@shared/ui/Button'
import { Badge } from '@shared/ui/Badge'
import { ThemeSwitcher } from '@/components/ThemeSwitcher'
import { DagCanvas } from '@features/dag-editor/components/DagCanvas'
import { useDagStore } from '@features/dag-editor/store/dag.store'
import { useSurveysStore } from '@features/surveys/store/surveys.store'
import type { Node, Edge } from 'reactflow'
import type { DagNodeData } from '@shared/types/dag.types'
import toast from 'react-hot-toast'

/* ── Height chain explanation ───────────────────────────────────────────
   html/body → #root (flex column, min-h: 100vh)
     → motion.div PageWrapper (flex: 1, height: 100vh, overflow: hidden)
       → PageLayout (flex column, height: 100%)
         → Header (56px fixed)
         → CanvasArea (flex: 1, min-h: 0, display: flex)
           → DagCanvas's EditorLayout (flex: 1, h: 100%, display: flex)
             → NodePalette (w: 210px)
             → CanvasWrapper (flex: 1, h: 100%)
               → <ReactFlow> (fills wrapper via CSS)
             → PropertiesPanel (w: 280px, conditional)
   
   Every flex ancestor has min-height: 0 so React Flow gets real pixels.
   ─────────────────────────────────────────────────────────────────────── */

const PageLayout = styled.div`
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: ${({ theme }) => theme.colors.bg};
`

const EditorHeader = styled.header`
  height: 56px;
  background: ${({ theme }) => theme.colors.bgSurface};
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 12px;
  flex-shrink: 0;
  z-index: 10;
`

const BackLink = styled(Link)`
  display: flex;
  align-items: center;
  gap: 6px;
  color: ${({ theme }) => theme.colors.textSecondary};
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  text-decoration: none;
  padding: 6px 10px;
  border-radius: ${({ theme }) => theme.radii.md};
  transition: all ${({ theme }) => theme.transitions.fast};

  &:hover {
    background: ${({ theme }) => theme.colors.bgElevated};
    color: ${({ theme }) => theme.colors.textPrimary};
  }
`

const Divider = styled.div`
  width: 1px;
  height: 24px;
  background: ${({ theme }) => theme.colors.border};
`

const SurveyTitle = styled.h2`
  font-size: ${({ theme }) => theme.typography.sizes.md};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
  flex: 1;
`

const SaveIndicator = styled.div`
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textTertiary};
`

const CanvasArea = styled.div`
  flex: 1 1 0%;
  min-height: 0;
  min-width: 0;
  overflow: hidden;
  display: flex;
`

const NotFound = styled.div`
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  color: ${({ theme }) => theme.colors.textSecondary};
`

export function DagEditorPage() {
  const { surveyId } = useParams({ from: '/editor/$surveyId' })
  const navigate = useNavigate()

  const getSurveyById = useSurveysStore((s) => s.getSurveyById)
  const updateSurvey = useSurveysStore((s) => s.updateSurvey)
  const survey = getSurveyById(surveyId)

  const loadSurvey = useDagStore((s) => s.loadSurvey)
  const nodes = useDagStore((s) => s.nodes)
  const edges = useDagStore((s) => s.edges)
  const isDirty = useDagStore((s) => s.isDirty)
  const markSaved = useDagStore((s) => s.markSaved)

  useEffect(() => {
    if (survey) {
      loadSurvey(
        survey.id,
        survey.nodes as Node<DagNodeData>[],
        survey.edges as Edge[]
      )
    }
  }, [survey?.id]) // eslint-disable-line react-hooks/exhaustive-deps

  if (!survey) {
    return (
      <PageLayout>
        <NotFound>
          <p>Survey not found.</p>
          <Button onClick={() => navigate({ to: '/dashboard' })}>Back to Dashboard</Button>
        </NotFound>
      </PageLayout>
    )
  }

  const handleSave = () => {
    updateSurvey(surveyId, { nodes: nodes as never, edges: edges as never })
    markSaved()
    toast.success('Survey saved!')
  }

  const handleTest = () => {
    toast.success('Preview mode coming soon!')
  }

  return (
    <PageLayout>
      <EditorHeader>
        <BackLink to="/dashboard">
          <ArrowLeft size={15} />
          Surveys
        </BackLink>
        <Divider />
        <SurveyTitle>{survey.title}</SurveyTitle>
        <Badge $variant="neutral">{nodes.length} nodes · {edges.length} edges</Badge>

        <SaveIndicator>
          {isDirty ? (
            <>
              <Circle size={10} fill="#F59E0B" color="#F59E0B" />
              Unsaved changes
            </>
          ) : (
            <Badge $variant="success">Saved</Badge>
          )}
        </SaveIndicator>

        <ThemeSwitcher />

        <Button
          variant="secondary"
          size="sm"
          icon={<Eye size={14} />}
          onClick={handleTest}
        >
          Preview
        </Button>
        <Button
          size="sm"
          icon={<Save size={14} />}
          onClick={handleSave}
          disabled={!isDirty}
        >
          Save
        </Button>
      </EditorHeader>

      <CanvasArea>
        <ReactFlowProvider>
          <DagCanvas />
        </ReactFlowProvider>
      </CanvasArea>
    </PageLayout>
  )
}
