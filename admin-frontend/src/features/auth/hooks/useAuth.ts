import { useMutation, useQuery } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { authApi } from '@api/auth.api'
import { useAuthStore } from '../store/auth.store'
import toast from 'react-hot-toast'
import type { LoginRequest } from '@shared/types/auth.types'

export function useMe() {
  const setUser = useAuthStore((s) => s.setUser)
  const clearAuth = useAuthStore((s) => s.clearAuth)

  return useQuery({
    queryKey: ['me'],
    queryFn: async () => {
      try {
        const user = await authApi.me()
        setUser(user)
        return user
      } catch {
        clearAuth()
        throw new Error('Unauthenticated')
      }
    },
    retry: false,
    staleTime: 5 * 60 * 1000,
  })
}

export function useLogin() {
  const setUser = useAuthStore((s) => s.setUser)
  const navigate = useNavigate()

  return useMutation({
    mutationFn: async (data: LoginRequest) => {
      await authApi.login(data)
      const user = await authApi.me()
      return user
    },
    onSuccess: (user) => {
      setUser(user)
      toast.success(`Welcome back, ${user.userName}!`)
      navigate({ to: '/dashboard' })
    },
    onError: (error: { response?: { status?: number } }) => {
      if (error?.response?.status === 401) {
        toast.error('Invalid email or password.')
      } else if (error?.response?.status === 423) {
        toast.error('Account is temporarily locked. Try again in 5 minutes.')
      } else {
        toast.error('Login failed. Please try again.')
      }
    },
  })
}

export function useLogout() {
  const clearAuth = useAuthStore((s) => s.clearAuth)
  const navigate = useNavigate()

  return useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      clearAuth()
      navigate({ to: '/login' })
    },
    onError: () => {
      // Force clear even if server fails
      clearAuth()
      navigate({ to: '/login' })
    },
  })
}
