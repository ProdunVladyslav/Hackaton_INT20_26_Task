import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import styled from 'styled-components'
import { Modal } from '@shared/ui/Modal'
import { Input } from '@shared/ui/Input'
import { Button } from '@shared/ui/Button'
import { useSurveysStore } from '../store/surveys.store'
import { SurveyStatus } from '@shared/types/dag.types'
import toast from 'react-hot-toast'
import { useNavigate } from '@tanstack/react-router'

const schema = z.object({
  title: z.string().min(1, 'Title is required').max(80, 'Max 80 characters'),
  description: z.string().max(200, 'Max 200 characters').optional(),
})

type FormValues = z.infer<typeof schema>

interface CreateSurveyModalProps {
  open: boolean
  onClose: () => void
}

const FormBody = styled.form`
  display: flex;
  flex-direction: column;
  gap: 20px;
`

const Footer = styled.div`
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 8px;
`

export function CreateSurveyModal({ open, onClose }: CreateSurveyModalProps) {
  const addSurvey = useSurveysStore((s) => s.addSurvey)
  const navigate = useNavigate()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const onSubmit = (values: FormValues) => {
    const id = crypto.randomUUID()
    addSurvey({
      id,
      title: values.title,
      description: values.description,
      status: SurveyStatus.Draft,
      completionCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      nodes: [],
      edges: [],
    })
    toast.success('Survey created!')
    reset()
    onClose()
    navigate({ to: '/editor/$surveyId', params: { surveyId: id } })
  }

  return (
    <Modal open={open} onClose={() => { reset(); onClose() }} title="Create New Survey">
      <FormBody onSubmit={handleSubmit(onSubmit)} noValidate>
        <Input
          label="Survey Title"
          placeholder="e.g. Wellness Onboarding"
          error={errors.title?.message}
          autoFocus
          {...register('title')}
        />
        <Input
          label="Description (optional)"
          placeholder="Brief description of this funnel"
          error={errors.description?.message}
          {...register('description')}
        />
        <Footer>
          <Button variant="secondary" type="button" onClick={() => { reset(); onClose() }}>
            Cancel
          </Button>
          <Button type="submit">Create Survey</Button>
        </Footer>
      </FormBody>
    </Modal>
  )
}
