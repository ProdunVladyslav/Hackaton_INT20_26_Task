import { RouterProvider } from '@tanstack/react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'react-hot-toast'
import { AppThemeProvider } from '@shared/theme/ThemeProvider'
import { GlobalStyles } from '@shared/theme/GlobalStyles'
import { ErrorBoundary } from '@/components/ErrorBoundary'
import { router } from './router'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
})

export function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <AppThemeProvider>
          <GlobalStyles />
          <RouterProvider router={router} />
          <Toaster
            position="top-right"
            toastOptions={{
              duration: 3500,
              style: {
                borderRadius: '10px',
                fontSize: '14px',
                fontWeight: 500,
              },
            }}
          />
        </AppThemeProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  )
}
