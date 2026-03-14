import styled from 'styled-components'
import { motion, AnimatePresence } from 'framer-motion'
import { X, Plus, Trash2 } from 'lucide-react'
import type { Node, Edge } from 'reactflow'
import { Input } from '@shared/ui/Input'
import { Select } from '@shared/ui/Select'
import { Button } from '@shared/ui/Button'
import { useDagStore, selectSelectedNode, selectSelectedEdge } from '../store/dag.store'
import type { DagNodeData, AnswerOption, EdgeCondition } from '@shared/types/dag.types'
import { NodeType, AttributeKey, AnswerType, Operator } from '@shared/types/dag.types'

const Panel = styled(motion.aside)`
  width: 280px;
  background: ${({ theme }) => theme.colors.bgSurface};
  border-left: 1px solid ${({ theme }) => theme.colors.border};
  display: flex;
  flex-direction: column;
  overflow: hidden;
  flex-shrink: 0;
`

const PanelHeader = styled.div`
  padding: 16px 20px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  display: flex;
  align-items: center;
  justify-content: space-between;
`

const PanelTitle = styled.h3`
  font-size: ${({ theme }) => theme.typography.sizes.md};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const PanelBody = styled.div`
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 18px;
`

const FieldGroup = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
`

const GroupLabel = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.xs};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textTertiary};
  text-transform: uppercase;
  letter-spacing: 0.7px;
`

const OptionRow = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
`

const attributeOptions = Object.values(AttributeKey).map((v) => ({
  value: v,
  label: v.replace(/_/g, ' '),
}))

const answerTypeOptions = Object.values(AnswerType).map((v) => ({
  value: v,
  label: v.replace(/_/g, ' '),
}))

const operatorOptions = Object.values(Operator).map((v) => ({
  value: v,
  label: v.toUpperCase(),
}))

function QuestionProperties({ node }: { node: Node<DagNodeData> }) {
  const updateNodeData = useDagStore((s) => s.updateNodeData)
  const data = node.data as import('@shared/types/dag.types').QuestionNodeData

  const addOption = () => {
    const newOption: AnswerOption = {
      id: crypto.randomUUID(),
      label: 'New option',
      value: `option_${data.options.length + 1}`,
    }
    updateNodeData(node.id, { options: [...data.options, newOption] } as Partial<DagNodeData>)
  }

  const removeOption = (id: string) => {
    updateNodeData(node.id, { options: data.options.filter((o) => o.id !== id) } as Partial<DagNodeData>)
  }

  const updateOption = (id: string, field: keyof AnswerOption, value: string) => {
    updateNodeData(node.id, {
      options: data.options.map((o) => (o.id === id ? { ...o, [field]: value } : o)),
    } as Partial<DagNodeData>)
  }

  return (
    <>
      <FieldGroup>
        <GroupLabel>Content</GroupLabel>
        <Input
          label="Question text"
          value={data.questionText}
          onChange={(e) => updateNodeData(node.id, { questionText: e.target.value } as Partial<DagNodeData>)}
          placeholder="Enter your question"
        />
        <Select
          label="Attribute to save"
          value={data.attribute}
          options={attributeOptions}
          onChange={(e) => updateNodeData(node.id, { attribute: e.target.value as AttributeKey } as Partial<DagNodeData>)}
        />
        <Select
          label="Answer type"
          value={data.answerType}
          options={answerTypeOptions}
          onChange={(e) => updateNodeData(node.id, { answerType: e.target.value as AnswerType } as Partial<DagNodeData>)}
        />
      </FieldGroup>

      <FieldGroup>
        <GroupLabel>Answer Options</GroupLabel>
        {data.options.map((opt) => (
          <OptionRow key={opt.id}>
            <Input
              value={opt.label}
              onChange={(e) => updateOption(opt.id, 'label', e.target.value)}
              placeholder="Option label"
            />
            <Button
              variant="ghost"
              size="sm"
              onClick={() => removeOption(opt.id)}
              style={{ flexShrink: 0 }}
            >
              <Trash2 size={14} />
            </Button>
          </OptionRow>
        ))}
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={addOption}>
          Add option
        </Button>
      </FieldGroup>
    </>
  )
}

function InfoProperties({ node }: { node: Node<DagNodeData> }) {
  const updateNodeData = useDagStore((s) => s.updateNodeData)
  const data = node.data as import('@shared/types/dag.types').InfoNodeData

  return (
    <FieldGroup>
      <GroupLabel>Content</GroupLabel>
      <Input
        label="Title"
        value={data.title}
        onChange={(e) => updateNodeData(node.id, { title: e.target.value } as Partial<DagNodeData>)}
        placeholder="Screen title"
      />
      <Input
        label="Body text"
        value={data.body}
        onChange={(e) => updateNodeData(node.id, { body: e.target.value } as Partial<DagNodeData>)}
        placeholder="Motivational message"
      />
      <Input
        label="Image URL (optional)"
        value={data.imageUrl ?? ''}
        onChange={(e) => updateNodeData(node.id, { imageUrl: e.target.value } as Partial<DagNodeData>)}
        placeholder="https://..."
      />
    </FieldGroup>
  )
}

function OfferProperties({ node }: { node: Node<DagNodeData> }) {
  const updateNodeData = useDagStore((s) => s.updateNodeData)
  const data = node.data as import('@shared/types/dag.types').OfferNodeData

  return (
    <FieldGroup>
      <GroupLabel>Offer Details</GroupLabel>
      <Input
        label="Headline"
        value={data.headline}
        onChange={(e) => updateNodeData(node.id, { headline: e.target.value } as Partial<DagNodeData>)}
        placeholder="Your plan is ready!"
      />
      <Input
        label="Description"
        value={data.description}
        onChange={(e) => updateNodeData(node.id, { description: e.target.value } as Partial<DagNodeData>)}
        placeholder="Plan description"
      />
      <Input
        label="CTA text"
        value={data.ctaText}
        onChange={(e) => updateNodeData(node.id, { ctaText: e.target.value } as Partial<DagNodeData>)}
        placeholder="Get my plan"
      />
      <Input
        label="Price (optional)"
        type="number"
        value={data.price ?? ''}
        onChange={(e) =>
          updateNodeData(node.id, {
            price: e.target.value ? parseFloat(e.target.value) : undefined,
          } as Partial<DagNodeData>)
        }
        placeholder="29.99"
      />
    </FieldGroup>
  )
}

function EdgeProperties({ edge }: { edge: Edge }) {
  const updateEdgeCondition = useDagStore((s) => s.updateEdgeCondition)
  const condition: EdgeCondition = edge.data?.condition ?? {
    attribute: AttributeKey.Goal,
    operator: Operator.Equals,
    value: '',
  }

  const update = (field: keyof EdgeCondition, value: string) => {
    updateEdgeCondition(edge.id, { ...condition, [field]: value })
  }

  return (
    <FieldGroup>
      <GroupLabel>Condition (optional)</GroupLabel>
      <Select
        label="Attribute"
        value={condition.attribute}
        options={attributeOptions}
        onChange={(e) => update('attribute', e.target.value)}
      />
      <Select
        label="Operator"
        value={condition.operator}
        options={operatorOptions}
        onChange={(e) => update('operator', e.target.value)}
      />
      <Input
        label="Value"
        value={condition.value}
        onChange={(e) => update('value', e.target.value)}
        placeholder="e.g. weight_loss"
      />
    </FieldGroup>
  )
}

export function PropertiesPanel() {
  const selectedNode = useDagStore(selectSelectedNode)
  const selectedEdge = useDagStore(selectSelectedEdge)
  const setSelectedNode = useDagStore((s) => s.setSelectedNode)
  const setSelectedEdge = useDagStore((s) => s.setSelectedEdge)

  const isOpen = !!selectedNode || !!selectedEdge

  return (
    <AnimatePresence>
      {isOpen && (
        <Panel
          initial={{ x: 280, opacity: 0 }}
          animate={{ x: 0, opacity: 1 }}
          exit={{ x: 280, opacity: 0 }}
          transition={{ type: 'spring', damping: 30, stiffness: 300 }}
        >
          <PanelHeader>
            <PanelTitle>
              {selectedNode
                ? `${selectedNode.type?.charAt(0).toUpperCase()}${selectedNode.type?.slice(1)} Node`
                : 'Edge Condition'}
            </PanelTitle>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setSelectedNode(null)
                setSelectedEdge(null)
              }}
            >
              <X size={16} />
            </Button>
          </PanelHeader>

          <PanelBody>
            {selectedNode && selectedNode.type === NodeType.Question && (
              <QuestionProperties node={selectedNode} />
            )}
            {selectedNode && selectedNode.type === NodeType.Info && (
              <InfoProperties node={selectedNode} />
            )}
            {selectedNode && selectedNode.type === NodeType.Offer && (
              <OfferProperties node={selectedNode} />
            )}
            {selectedEdge && <EdgeProperties edge={selectedEdge} />}
          </PanelBody>
        </Panel>
      )}
    </AnimatePresence>
  )
}
