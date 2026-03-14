import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { MeResponse } from '@shared/types/auth.types'

interface AuthState {
  user: MeResponse | null
  isAuthenticated: boolean
  setUser: (user: MeResponse | null) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      setUser: (user) => set({ user, isAuthenticated: !!user }),
      clearAuth: () => set({ user: null, isAuthenticated: false }),
    }),
    {
      name: 'wellness-auth',
      partialize: (state) => ({ user: state.user, isAuthenticated: state.isAuthenticated }),
    }
  )
)
