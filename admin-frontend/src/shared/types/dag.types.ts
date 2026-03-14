// ─── DAG Node Types ───────────────────────────────────────────────────────────

export enum NodeType {
  Question = 'question',
  Info = 'info',
  Offer = 'offer',
}

export enum AttributeKey {
  Age = 'age',
  Gender = 'gender',
  Goal = 'goal',
  Location = 'location',
  FitnessLevel = 'fitness_level',
  AvailableTime = 'available_time',
  Injuries = 'injuries',
  Motivation = 'motivation',
  StressLevel = 'stress_level',
  SleepLevel = 'sleep_level',
  EnergyLevel = 'energy_level',
}

export enum AnswerType {
  SingleChoice = 'single_choice',
  MultipleChoice = 'multiple_choice',
  Slider = 'slider',
  TextInput = 'text_input',
}

export enum Operator {
  Equals = 'eq',
  NotEquals = 'neq',
  GreaterThan = 'gt',
  LessThan = 'lt',
  Contains = 'contains',
}

export interface AnswerOption {
  id: string
  label: string
  icon?: string
  value: string
}

// ─── Node Data Discriminated Union ───────────────────────────────────────────

export interface QuestionNodeData {
  type: NodeType.Question
  questionText: string
  attribute: AttributeKey
  answerType: AnswerType
  options: AnswerOption[]
}

export interface InfoNodeData {
  type: NodeType.Info
  title: string
  body: string
  imageUrl?: string
}

export interface OfferNodeData {
  type: NodeType.Offer
  headline: string
  description: string
  ctaText: string
  price?: number
}

export type DagNodeData = QuestionNodeData | InfoNodeData | OfferNodeData

// ─── DAG Node (React Flow node) ───────────────────────────────────────────────

export interface DagNode {
  id: string
  type: NodeType
  position: { x: number; y: number }
  data: DagNodeData
}

// ─── Edge Condition ──────────────────────────────────────────────────────────

export interface EdgeCondition {
  attribute: AttributeKey
  operator: Operator
  value: string
}

// ─── DAG Edge (React Flow edge) ──────────────────────────────────────────────

export interface DagEdge {
  id: string
  source: string
  target: string
  condition?: EdgeCondition
}

// ─── Survey / Funnel ─────────────────────────────────────────────────────────

export enum SurveyStatus {
  Draft = 'draft',
  Published = 'published',
  Archived = 'archived',
}

export interface Survey {
  id: string
  title: string
  description?: string
  status: SurveyStatus
  completionCount: number
  createdAt: string
  updatedAt: string
  nodes: DagNode[]
  edges: DagEdge[]
}
