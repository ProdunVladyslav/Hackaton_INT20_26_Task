import { apiClient } from './axios'
import type { LoginRequest, LoginResponse, MeResponse } from '@shared/types/auth.types'

export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/api/auth/login', data)
    return response.data
  },

  me: async (): Promise<MeResponse> => {
    const response = await apiClient.get<MeResponse>('/api/auth/me')
    return response.data
  },

  logout: async (): Promise<void> => {
    await apiClient.post('/api/auth/logout')
  },
}
