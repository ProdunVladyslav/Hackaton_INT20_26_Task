import { create } from 'zustand'
import { Survey, SurveyStatus, NodeType, AttributeKey, AnswerType } from '@shared/types/dag.types'

const MOCK_SURVEYS: Survey[] = [
  {
    id: '1',
    title: 'Wellness Onboarding',
    description: 'Main user onboarding flow for wellness profile setup',
    status: SurveyStatus.Published,
    completionCount: 1842,
    createdAt: '2026-02-10T10:00:00Z',
    updatedAt: '2026-03-12T15:30:00Z',
    nodes: [
      {
        id: 'n1',
        type: NodeType.Question,
        position: { x: 100, y: 100 },
        data: {
          type: NodeType.Question,
          questionText: 'What is your primary fitness goal?',
          attribute: AttributeKey.Goal,
          answerType: AnswerType.SingleChoice,
          options: [
            { id: 'o1', label: 'Lose weight', icon: '⚖️', value: 'weight_loss' },
            { id: 'o2', label: 'Build strength', icon: '💪', value: 'strength' },
            { id: 'o3', label: 'Improve endurance', icon: '🏃', value: 'endurance' },
            { id: 'o4', label: 'Reduce stress', icon: '🧘', value: 'stress_reduction' },
          ],
        },
      },
      {
        id: 'n2',
        type: NodeType.Question,
        position: { x: 450, y: 100 },
        data: {
          type: NodeType.Question,
          questionText: 'What is your current fitness level?',
          attribute: AttributeKey.FitnessLevel,
          answerType: AnswerType.SingleChoice,
          options: [
            { id: 'o5', label: 'Beginner', icon: '🌱', value: 'beginner' },
            { id: 'o6', label: 'Intermediate', icon: '🔥', value: 'intermediate' },
            { id: 'o7', label: 'Advanced', icon: '⚡', value: 'advanced' },
          ],
        },
      },
      {
        id: 'n3',
        type: NodeType.Info,
        position: { x: 800, y: 100 },
        data: {
          type: NodeType.Info,
          title: 'Almost there!',
          body: 'Most people your age achieve results in just 3 weeks with a personalized plan.',
        },
      },
      {
        id: 'n4',
        type: NodeType.Offer,
        position: { x: 1150, y: 100 },
        data: {
          type: NodeType.Offer,
          headline: 'Your Plan is Ready!',
          description: '12-week personalized wellness program tailored to your goals.',
          ctaText: 'Get My Plan',
          price: 29.99,
        },
      },
    ],
    edges: [
      { id: 'e1', source: 'n1', target: 'n2' },
      { id: 'e2', source: 'n2', target: 'n3' },
      { id: 'e3', source: 'n3', target: 'n4' },
    ],
  },
  {
    id: '2',
    title: 'Stress & Sleep Assessment',
    description: 'Psychometric evaluation for stress, sleep, and energy levels',
    status: SurveyStatus.Draft,
    completionCount: 0,
    createdAt: '2026-03-01T09:00:00Z',
    updatedAt: '2026-03-14T08:00:00Z',
    nodes: [],
    edges: [],
  },
  {
    id: '3',
    title: 'Quick Fitness Quiz',
    description: 'Short 3-question funnel for engagement campaigns',
    status: SurveyStatus.Published,
    completionCount: 523,
    createdAt: '2026-01-20T12:00:00Z',
    updatedAt: '2026-02-28T11:00:00Z',
    nodes: [],
    edges: [],
  },
]

interface SurveysState {
  surveys: Survey[]
  addSurvey: (survey: Survey) => void
  updateSurvey: (id: string, updates: Partial<Survey>) => void
  deleteSurvey: (id: string) => void
  getSurveyById: (id: string) => Survey | undefined
}

export const useSurveysStore = create<SurveysState>((set, get) => ({
  surveys: MOCK_SURVEYS,
  addSurvey: (survey) => set((s) => ({ surveys: [...s.surveys, survey] })),
  updateSurvey: (id, updates) =>
    set((s) => ({
      surveys: s.surveys.map((sv) => (sv.id === id ? { ...sv, ...updates, updatedAt: new Date().toISOString() } : sv)),
    })),
  deleteSurvey: (id) => set((s) => ({ surveys: s.surveys.filter((sv) => sv.id !== id) })),
  getSurveyById: (id) => get().surveys.find((sv) => sv.id === id),
}))
