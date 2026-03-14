import styled from 'styled-components'
import { motion } from 'framer-motion'
import { LoginForm } from '@features/auth/components/LoginForm'
import { ThemeSwitcher } from '@/components/ThemeSwitcher'

const Page = styled.div`
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: ${({ theme }) => theme.colors.bg};
  padding: 24px;
  position: relative;
`

const TopRight = styled.div`
  position: absolute;
  top: 20px;
  right: 20px;
`

const Card = styled(motion.div)`
  background: ${({ theme }) => theme.colors.bgSurface};
  border: 1px solid ${({ theme }) => theme.colors.border};
  border-radius: ${({ theme }) => theme.radii.xl};
  padding: 40px;
  width: 100%;
  max-width: 400px;
  box-shadow: ${({ theme }) => theme.shadows.lg};
`

const LogoMark = styled.div`
  width: 44px;
  height: 44px;
  background: ${({ theme }) => theme.colors.accent};
  border-radius: ${({ theme }) => theme.radii.md};
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  margin-bottom: 24px;
`

const Heading = styled.h1`
  font-size: ${({ theme }) => theme.typography.sizes.xxl};
  font-weight: ${({ theme }) => theme.typography.weights.bold};
  color: ${({ theme }) => theme.colors.textPrimary};
  margin-bottom: 8px;
`

const Subheading = styled.p`
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textSecondary};
  margin-bottom: 32px;
`

export function LoginPage() {
  return (
    <Page>
      <TopRight>
        <ThemeSwitcher />
      </TopRight>
      <Card
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: 'easeOut' }}
      >
        <LogoMark>🌿</LogoMark>
        <Heading>Welcome back</Heading>
        <Subheading>Sign in to your Wellness Admin panel</Subheading>
        <LoginForm />
      </Card>
    </Page>
  )
}
