import React from 'react'
import styled from 'styled-components'
import { Link, useRouterState } from '@tanstack/react-router'
import { LayoutGrid, LogOut, User } from 'lucide-react'
import { ThemeSwitcher } from './ThemeSwitcher'
import { Button } from '@shared/ui/Button'
import { useAuthStore } from '@features/auth/store/auth.store'
import { useLogout } from '@features/auth/hooks/useAuth'

const Layout = styled.div`
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: ${({ theme }) => theme.colors.bg};
`

const Navbar = styled.header`
  height: 60px;
  background: ${({ theme }) => theme.colors.bgSurface};
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  display: flex;
  align-items: center;
  padding: 0 24px;
  gap: 16px;
  flex-shrink: 0;
  position: sticky;
  top: 0;
  z-index: 100;
`

const Logo = styled(Link)`
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
`

const LogoMark = styled.div`
  width: 32px;
  height: 32px;
  background: ${({ theme }) => theme.colors.accent};
  border-radius: ${({ theme }) => theme.radii.sm};
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
`

const LogoText = styled.span`
  font-size: ${({ theme }) => theme.typography.sizes.md};
  font-weight: ${({ theme }) => theme.typography.weights.semibold};
  color: ${({ theme }) => theme.colors.textPrimary};
`

const NavLinks = styled.nav`
  display: flex;
  align-items: center;
  gap: 4px;
  margin-left: 16px;
`

const NavLink = styled(Link)<{ $active?: boolean }>`
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 6px 12px;
  border-radius: ${({ theme }) => theme.radii.md};
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  font-weight: ${({ theme }) => theme.typography.weights.medium};
  color: ${({ $active, theme }) => ($active ? theme.colors.accent : theme.colors.textSecondary)};
  background: ${({ $active, theme }) => ($active ? theme.colors.accentLight : 'transparent')};
  transition: all ${({ theme }) => theme.transitions.fast};
  text-decoration: none;

  &:hover {
    color: ${({ theme }) => theme.colors.textPrimary};
    background: ${({ theme }) => theme.colors.bgElevated};
  }
`

const Spacer = styled.div`
  flex: 1;
`

const RightSection = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
`

const UserChip = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  background: ${({ theme }) => theme.colors.bgElevated};
  border-radius: ${({ theme }) => theme.radii.full};
  font-size: ${({ theme }) => theme.typography.sizes.sm};
  color: ${({ theme }) => theme.colors.textSecondary};
`

const Main = styled.main`
  flex: 1;
  display: flex;
  flex-direction: column;
`

export function AdminLayout({ children }: { children: React.ReactNode }) {
  const user = useAuthStore((s) => s.user)
  const { mutate: logout, isPending } = useLogout()
  const routerState = useRouterState()
  const pathname = routerState.location.pathname

  return (
    <Layout>
      <Navbar>
        <Logo to="/dashboard">
          <LogoMark>🌿</LogoMark>
          <LogoText>Wellness Admin</LogoText>
        </Logo>

        <NavLinks>
          <NavLink to="/dashboard" $active={pathname === '/dashboard'}>
            <LayoutGrid size={15} />
            Surveys
          </NavLink>
        </NavLinks>

        <Spacer />

        <RightSection>
          <ThemeSwitcher />
          {user && (
            <UserChip>
              <User size={13} />
              {user.userName}
            </UserChip>
          )}
          <Button
            variant="ghost"
            size="sm"
            icon={<LogOut size={14} />}
            loading={isPending}
            onClick={() => logout()}
          >
            Sign out
          </Button>
        </RightSection>
      </Navbar>

      <Main>{children}</Main>
    </Layout>
  )
}
