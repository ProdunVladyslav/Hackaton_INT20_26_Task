import { memo } from 'react'
import { getBezierPath, EdgeLabelRenderer, BaseEdge } from 'reactflow'
import type { EdgeProps } from 'reactflow'
import { useTheme } from 'styled-components'

export interface ConditionEdgeData {
  label?: string
}

export const ConditionEdge = memo(function ConditionEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  data,
  markerEnd,
  selected,
}: EdgeProps<ConditionEdgeData>) {
  const theme = useTheme()
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  })

  const strokeColor = selected ? theme.colors.accent : theme.colors.textTertiary
  const hasLabel = !!data?.label

  return (
    <>
      <BaseEdge
        id={id}
        path={edgePath}
        markerEnd={markerEnd}
        style={{
          stroke: strokeColor,
          strokeWidth: selected ? 2.5 : 1.5,
          transition: 'stroke 0.15s ease, stroke-width 0.15s ease',
        }}
      />
      {hasLabel && (
        <EdgeLabelRenderer>
          <div
            className="nodrag nopan"
            style={{
              position: 'absolute',
              transform: `translate(-50%, -50%) translate(${labelX}px,${labelY}px)`,
              fontSize: 11,
              fontFamily: theme.typography.fontFamily,
              fontWeight: theme.typography.weights.medium,
              background: selected ? theme.colors.accentLight : theme.colors.bgSurface,
              color: selected ? theme.colors.accentText : theme.colors.textSecondary,
              border: `1px solid ${selected ? theme.colors.accent : theme.colors.border}`,
              borderRadius: theme.radii.full,
              padding: '2px 8px',
              pointerEvents: 'all',
              whiteSpace: 'nowrap',
              boxShadow: theme.shadows.sm,
              transition: 'all 0.15s ease',
            }}
          >
            {data?.label}
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  )
})
