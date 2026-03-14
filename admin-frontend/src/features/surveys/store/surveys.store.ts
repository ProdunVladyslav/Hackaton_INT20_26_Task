import { create } from 'zustand'
import { Survey, SurveyStatus, NodeType, AttributeKey, AnswerType, Operator } from '@shared/types/dag.types'

// ─── Complete BetterMe-style demo DAG ─────────────────────────────────────────
// Layout: left-to-right tree. Columns at x=60, 400, 740, 1080, 1420
// Each node is ~260px wide + 30px gap. Rows staggered vertically.

const DEMO_NODES = [
  // ── Column 1: Entry ──────────────────────────────────────────────────────
  {
    id: 'n-goal',
    type: NodeType.Question,
    position: { x: 60, y: 340 },
    data: {
      type: NodeType.Question,
      questionText: "What is your main health goal?",
      attribute: AttributeKey.Goal,
      answerType: AnswerType.SingleChoice,
      options: [
        { id: 'o1', label: 'Lose weight', icon: '⚖️', value: 'weight_loss' },
        { id: 'o2', label: 'Build strength', icon: '💪', value: 'strength' },
        { id: 'o3', label: 'Reduce stress', icon: '🧘', value: 'stress_reduction' },
        { id: 'o4', label: 'More energy', icon: '⚡', value: 'energy' },
      ],
    },
  },

  // ── Column 2: Branches ───────────────────────────────────────────────────
  {
    id: 'n-context',
    type: NodeType.Question,
    position: { x: 400, y: 60 },
    data: {
      type: NodeType.Question,
      questionText: "Where do you prefer to workout?",
      attribute: AttributeKey.Location,
      answerType: AnswerType.SingleChoice,
      options: [
        { id: 'o5', label: 'At home', icon: '🏠', value: 'home' },
        { id: 'o6', label: 'At the gym', icon: '🏋️', value: 'gym' },
        { id: 'o7', label: 'Outdoors', icon: '🌳', value: 'outdoor' },
      ],
    },
  },
  {
    id: 'n-fitness',
    type: NodeType.Question,
    position: { x: 400, y: 380 },
    data: {
      type: NodeType.Question,
      questionText: "What is your current fitness level?",
      attribute: AttributeKey.FitnessLevel,
      answerType: AnswerType.SingleChoice,
      options: [
        { id: 'o8', label: 'Beginner', icon: '🌱', value: 'beginner' },
        { id: 'o9', label: 'Intermediate', icon: '🔥', value: 'intermediate' },
        { id: 'o10', label: 'Advanced', icon: '⚡', value: 'advanced' },
      ],
    },
  },
  {
    id: 'n-stress-q',
    type: NodeType.Question,
    position: { x: 400, y: 640 },
    data: {
      type: NodeType.Question,
      questionText: "How would you rate your current stress level?",
      attribute: AttributeKey.StressLevel,
      answerType: AnswerType.SingleChoice,
      options: [
        { id: 'o11', label: 'Low (1–3)', icon: '😊', value: 'low' },
        { id: 'o12', label: 'Moderate (4–6)', icon: '😐', value: 'moderate' },
        { id: 'o13', label: 'High (7–10)', icon: '😰', value: 'high' },
      ],
    },
  },

  // ── Column 3: Sub-branches ───────────────────────────────────────────────
  {
    id: 'n-time',
    type: NodeType.Question,
    position: { x: 740, y: 60 },
    data: {
      type: NodeType.Question,
      questionText: "How much time can you dedicate each day?",
      attribute: AttributeKey.AvailableTime,
      answerType: AnswerType.SingleChoice,
      options: [
        { id: 'o14', label: '10–15 min', icon: '⏱️', value: '10_15' },
        { id: 'o15', label: '20–30 min', icon: '⏰', value: '20_30' },
        { id: 'o16', label: '45+ min', icon: '🕐', value: '45_plus' },
      ],
    },
  },
  {
    id: 'n-injuries',
    type: NodeType.Question,
    position: { x: 740, y: 300 },
    data: {
      type: NodeType.Question,
      questionText: "Do you have any physical limitations?",
      attribute: AttributeKey.Injuries,
      answerType: AnswerType.MultipleChoice,
      options: [
        { id: 'o17', label: 'None', icon: '✅', value: 'none' },
        { id: 'o18', label: 'Knee pain', icon: '🦵', value: 'knee' },
        { id: 'o19', label: 'Back issues', icon: '🔙', value: 'back' },
        { id: 'o20', label: 'Shoulder', icon: '🤷', value: 'shoulder' },
      ],
    },
  },
  {
    id: 'n-age',
    type: NodeType.Question,
    position: { x: 740, y: 540 },
    data: {
      type: NodeType.Question,
      questionText: "What is your age group?",
      attribute: AttributeKey.Age,
      answerType: AnswerType.SingleChoice,
      options: [
        { id: 'o21', label: 'Under 25', icon: '🧑', value: 'under_25' },
        { id: 'o22', label: '25–35', icon: '👤', value: '25_35' },
        { id: 'o23', label: '36–50', icon: '👨', value: '36_50' },
        { id: 'o24', label: '50+', icon: '👴', value: '50_plus' },
      ],
    },
  },
  {
    id: 'n-sleep',
    type: NodeType.Question,
    position: { x: 740, y: 780 },
    data: {
      type: NodeType.Question,
      questionText: "How is your sleep quality lately?",
      attribute: AttributeKey.SleepLevel,
      answerType: AnswerType.SingleChoice,
      options: [
        { id: 'o25', label: 'Great (7–9 hrs)', icon: '😴', value: 'great' },
        { id: 'o26', label: 'Fair (5–7 hrs)', icon: '🌙', value: 'fair' },
        { id: 'o27', label: 'Poor (<5 hrs)', icon: '😵', value: 'poor' },
      ],
    },
  },

  // ── Column 3.5: Info screens ─────────────────────────────────────────────
  {
    id: 'n-info-strength',
    type: NodeType.Info,
    position: { x: 1080, y: 380 },
    data: {
      type: NodeType.Info,
      title: "You're stronger than you think! 💪",
      body: "Users at your fitness level typically see visible strength gains within 3 weeks of consistent training.",
    },
  },

  // ── Column 4: Offers ─────────────────────────────────────────────────────
  {
    id: 'n-offer-home',
    type: NodeType.Offer,
    position: { x: 1080, y: 60 },
    data: {
      type: NodeType.Offer,
      headline: 'Home Fat-Burn Starter',
      description: '4-week home workout plan targeting fat loss. No equipment needed. 15–30 min sessions.',
      ctaText: 'Start for $19.99',
      price: 19.99,
    },
  },
  {
    id: 'n-offer-gym',
    type: NodeType.Offer,
    position: { x: 1080, y: 220 },
    data: {
      type: NodeType.Offer,
      headline: 'Gym Weight Loss Pro',
      description: '6-week gym program with personalized machine and free-weight circuits for maximum calorie burn.',
      ctaText: 'Get Plan — $29.99',
      price: 29.99,
    },
  },
  {
    id: 'n-offer-strength',
    type: NodeType.Offer,
    position: { x: 1420, y: 380 },
    data: {
      type: NodeType.Offer,
      headline: 'BetterMe Strength Builder',
      description: '8-week progressive overload program. Includes resistance bands, training log & nutrition guide.',
      ctaText: 'Build Strength — $39.99',
      price: 39.99,
    },
  },
  {
    id: 'n-offer-stress',
    type: NodeType.Offer,
    position: { x: 1080, y: 700 },
    data: {
      type: NodeType.Offer,
      headline: 'Stress Reset Wellness Kit',
      description: '21-day guided meditation + yoga flow program. Includes sleep tracker, breathing exercises & journaling kit.',
      ctaText: 'Reset Now — $24.99',
      price: 24.99,
    },
  },
]

const DEMO_EDGES = [
  // Entry → Column 2
  {
    id: 'e-goal-wl',
    source: 'n-goal', target: 'n-context',
    type: 'conditionEdge',
    data: { label: 'goal = weight_loss' },
  },
  {
    id: 'e-goal-str',
    source: 'n-goal', target: 'n-fitness',
    type: 'conditionEdge',
    data: { label: 'goal = strength' },
  },
  {
    id: 'e-goal-stress',
    source: 'n-goal', target: 'n-stress-q',
    type: 'conditionEdge',
    data: { label: 'goal = stress_reduction' },
  },
  {
    id: 'e-goal-energy',
    source: 'n-goal', target: 'n-age',
    type: 'conditionEdge',
    data: { label: 'goal = energy (fallback)' },
  },

  // Context → Time / Injuries
  {
    id: 'e-ctx-home',
    source: 'n-context', target: 'n-time',
    type: 'conditionEdge',
    data: { label: 'context = home | outdoor' },
  },
  {
    id: 'e-ctx-gym',
    source: 'n-context', target: 'n-injuries',
    type: 'conditionEdge',
    data: { label: 'context = gym' },
  },

  // Fitness → Info → Offer
  {
    id: 'e-fit-info',
    source: 'n-fitness', target: 'n-info-strength',
    type: 'conditionEdge',
    data: { label: 'always' },
  },
  {
    id: 'e-info-offer',
    source: 'n-info-strength', target: 'n-offer-strength',
    type: 'conditionEdge',
    data: {},
  },

  // Time → Offers
  {
    id: 'e-time-home-offer',
    source: 'n-time', target: 'n-offer-home',
    type: 'conditionEdge',
    data: { label: 'time = 10_15 | 20_30' },
  },
  {
    id: 'e-time-gym-offer',
    source: 'n-time', target: 'n-offer-gym',
    type: 'conditionEdge',
    data: { label: 'time = 45_plus' },
  },

  // Injuries → Offers
  {
    id: 'e-inj-gym',
    source: 'n-injuries', target: 'n-offer-gym',
    type: 'conditionEdge',
    data: { label: 'no injury' },
  },
  {
    id: 'e-inj-home',
    source: 'n-injuries', target: 'n-offer-home',
    type: 'conditionEdge',
    data: { label: 'has injury → low impact' },
  },

  // Stress → Sleep → Stress Offer
  {
    id: 'e-stress-sleep',
    source: 'n-stress-q', target: 'n-sleep',
    type: 'conditionEdge',
    data: { label: 'always' },
  },
  {
    id: 'e-sleep-offer',
    source: 'n-sleep', target: 'n-offer-stress',
    type: 'conditionEdge',
    data: { label: 'always' },
  },

  // Age → Stress offer (energy path)
  {
    id: 'e-age-offer',
    source: 'n-age', target: 'n-offer-stress',
    type: 'conditionEdge',
    data: { label: 'fallback → wellness' },
  },
]

// ─── Zustand store ────────────────────────────────────────────────────────────

const MOCK_SURVEYS: Survey[] = [
  {
    id: 'demo-flow-1',
    title: 'BetterMe Wellness Onboarding',
    description: 'Main user onboarding funnel — branches by goal, location, and physical profile to match personalized offers',
    status: SurveyStatus.Published,
    completionCount: 4_218,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: '2026-03-13T15:30:00Z',
    nodes: DEMO_NODES as Survey['nodes'],
    edges: DEMO_EDGES as Survey['edges'],
  },
  {
    id: 'demo-flow-2',
    title: 'Stress & Sleep Assessment',
    description: 'Psychometric evaluation for stress, sleep quality, and energy levels targeting the wellness upsell flow',
    status: SurveyStatus.Published,
    completionCount: 1_031,
    createdAt: '2026-02-01T09:00:00Z',
    updatedAt: '2026-03-10T08:00:00Z',
    nodes: [
      {
        id: 's2-n1',
        type: NodeType.Question,
        position: { x: 80, y: 200 },
        data: {
          type: NodeType.Question,
          questionText: 'How many hours of sleep do you get per night?',
          attribute: AttributeKey.SleepLevel,
          answerType: AnswerType.Slider,
          options: [
            { id: 'a1', label: 'Under 5h', icon: '😵', value: 'under_5' },
            { id: 'a2', label: '5–7h', icon: '🌙', value: '5_7' },
            { id: 'a3', label: '7+ hours', icon: '😴', value: '7_plus' },
          ],
        },
      },
      {
        id: 's2-n2',
        type: NodeType.Question,
        position: { x: 420, y: 200 },
        data: {
          type: NodeType.Question,
          questionText: 'What is your energy level on a typical day?',
          attribute: AttributeKey.EnergyLevel,
          answerType: AnswerType.SingleChoice,
          options: [
            { id: 'b1', label: 'Very low', icon: '🔋', value: 'very_low' },
            { id: 'b2', label: 'Low', icon: '🪫', value: 'low' },
            { id: 'b3', label: 'Moderate', icon: '⚡', value: 'moderate' },
            { id: 'b4', label: 'High', icon: '🔆', value: 'high' },
          ],
        },
      },
      {
        id: 's2-n3',
        type: NodeType.Info,
        position: { x: 760, y: 200 },
        data: {
          type: NodeType.Info,
          title: 'You deserve better sleep 🌙',
          body: '78% of users who completed this program reported sleeping 2+ more hours per night within 3 weeks.',
        },
      },
      {
        id: 's2-n4',
        type: NodeType.Offer,
        position: { x: 1100, y: 200 },
        data: {
          type: NodeType.Offer,
          headline: 'Sleep & Calm Bundle',
          description: '30-day guided sleep improvement program with breathing exercises & evening yoga routines.',
          ctaText: 'Start Sleeping Better — $14.99',
          price: 14.99,
        },
      },
    ] as Survey['nodes'],
    edges: [
      {
        id: 's2-e1', source: 's2-n1', target: 's2-n2',
        type: 'conditionEdge', data: { label: 'always' },
        condition: undefined,
      },
      {
        id: 's2-e2', source: 's2-n2', target: 's2-n3',
        type: 'conditionEdge', data: { label: 'always' },
        condition: undefined,
      },
      {
        id: 's2-e3', source: 's2-n3', target: 's2-n4',
        type: 'conditionEdge', data: {},
        condition: undefined,
      },
    ] as Survey['edges'],
  },
  {
    id: 'demo-flow-3',
    title: 'Quick Fitness Quiz',
    description: '3-question fast-funnel for paid campaigns — optimized for 60-second completion',
    status: SurveyStatus.Draft,
    completionCount: 0,
    createdAt: '2026-03-14T08:00:00Z',
    updatedAt: '2026-03-14T08:00:00Z',
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
      surveys: s.surveys.map((sv) =>
        sv.id === id ? { ...sv, ...updates, updatedAt: new Date().toISOString() } : sv
      ),
    })),
  deleteSurvey: (id) => set((s) => ({ surveys: s.surveys.filter((sv) => sv.id !== id) })),
  getSurveyById: (id) => get().surveys.find((sv) => sv.id === id),
}))
