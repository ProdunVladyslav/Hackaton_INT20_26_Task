import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Mail, Lock, Zap } from 'lucide-react'
import styled from 'styled-components'
import { motion } from 'framer-motion'
import { Input } from '@shared/ui/Input'
import { Button } from '@shared/ui/Button'
import { useLogin, useDemoLogin } from '../hooks/useAuth'

const loginSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

type LoginFormValues = z.infer<typeof loginSchema>

const Form = styled.form`
  display: flex;
  flex-direction: column;
  gap: 18px;
`

const ForgotLink = styled.a`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.accent};
  text-align: right;
  cursor: pointer;
  transition: opacity ${({ theme }) => theme.transitions.fast};
  &:hover { opacity: 0.8; }
`

const Divider = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
  color: ${({ theme }) => theme.colors.textTertiary};
  font-size: ${({ theme }) => theme.typography.sizes.sm};

  &::before, &::after {
    content: '';
    flex: 1;
    height: 1px;
    background: ${({ theme }) => theme.colors.border};
  }
`

const DemoButton = styled(motion.button)`
  width: 100%;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border: 1.5px dashed ${({ theme }) => theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.md};
  background: ${({ theme }) => theme.colors.bgElevated};
  color: ${({ theme }) => theme.colors.textSecondary};
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.medium};
  font-family: ${({ theme }) => theme.typography.fontFamily};
  cursor: pointer;
  transition: all ${({ theme }) => theme.transitions.fast};

  &:hover {
    border-color: ${({ theme }) => theme.colors.accent};
    color: ${({ theme }) => theme.colors.accent};
    background: ${({ theme }) => theme.colors.accentLight};
  }
`

export function LoginForm() {
  const { mutate, isPending } = useLogin()
  const enterDemo = useDemoLogin()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  })

  const onSubmit = (data: LoginFormValues) => mutate(data)

  return (
    <Form onSubmit={handleSubmit(onSubmit)} noValidate>
      <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}>
        <Input
          label="Email"
          type="email"
          placeholder="you@example.com"
          autoComplete="email"
          leftIcon={<Mail size={16} />}
          error={errors.email?.message}
          {...register('email')}
        />
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.15 }}>
        <Input
          label="Password"
          type="password"
          placeholder="••••••••"
          autoComplete="current-password"
          leftIcon={<Lock size={16} />}
          error={errors.password?.message}
          {...register('password')}
        />
        <ForgotLink style={{ display: 'block', marginTop: 6 }}>Forgot password?</ForgotLink>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }}>
        <Button type="submit" fullWidth loading={isPending} size="lg">
          {isPending ? 'Signing in…' : 'Sign in'}
        </Button>
      </motion.div>

      <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.3 }}>
        <Divider>or</Divider>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.35 }}>
        <DemoButton
          type="button"
          whileTap={{ scale: 0.98 }}
          onClick={enterDemo}
        >
          <Zap size={15} />
          Continue with Demo (no backend needed)
        </DemoButton>
      </motion.div>
    </Form>
  )
}
