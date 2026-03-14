import React from 'react'
import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  redirect,
} from '@tanstack/react-router'
import { motion } from 'framer-motion'
import { LoginPage } from '@pages/LoginPage'
import { DashboardPage } from '@pages/DashboardPage'
import { DagEditorPage } from '@pages/DagEditorPage'
import { useAuthStore } from '@features/auth/store/auth.store'

// ─── Root route ───────────────────────────────────────────────────────────────
const rootRoute = createRootRoute({
  component: () => <Outlet />,
})

// ─── Animated page wrapper ────────────────────────────────────────────────────
// CRITICAL: Use min-height: 0 (not 100vh) so the flex chain can shrink properly.
// The actual page height is controlled by each page component.
function PageWrapper({ children }: { children: React.ReactNode }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 6 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25, ease: 'easeOut' }}
      style={{
        flex: '1 1 0%',
        display: 'flex',
        flexDirection: 'column',
        minHeight: 0,
        height: '100vh',
        overflow: 'hidden',
      }}
    >
      {children}
    </motion.div>
  )
}

// ─── Auth guard helper ────────────────────────────────────────────────────────
function requireAuth() {
  const { isAuthenticated } = useAuthStore.getState()
  if (!isAuthenticated) {
    throw redirect({ to: '/login' })
  }
}

function requireGuest() {
  const { isAuthenticated } = useAuthStore.getState()
  if (isAuthenticated) {
    throw redirect({ to: '/dashboard' })
  }
}

// ─── Routes ───────────────────────────────────────────────────────────────────
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  beforeLoad: () => {
    const { isAuthenticated } = useAuthStore.getState()
    throw redirect({ to: isAuthenticated ? '/dashboard' : '/login' })
  },
  component: () => null,
})

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  beforeLoad: requireGuest,
  component: () => (
    <PageWrapper>
      <LoginPage />
    </PageWrapper>
  ),
})

const dashboardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/dashboard',
  beforeLoad: requireAuth,
  component: () => (
    <PageWrapper>
      <DashboardPage />
    </PageWrapper>
  ),
})

const editorRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/editor/$surveyId',
  beforeLoad: requireAuth,
  component: () => (
    <PageWrapper>
      <DagEditorPage />
    </PageWrapper>
  ),
})

// ─── Router ──────────────────────────────────────────────────────────────────
const routeTree = rootRoute.addChildren([
  indexRoute,
  loginRoute,
  dashboardRoute,
  editorRoute,
])

export const router = createRouter({ routeTree })

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
