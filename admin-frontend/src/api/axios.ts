import axios, { AxiosError } from 'axios'
import toast from 'react-hot-toast'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export const apiClient = axios.create({
  baseURL: API_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor
apiClient.interceptors.request.use(
  (config) => config,
  (error: AxiosError) => Promise.reject(error)
)

// Response interceptor – centralized error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response) {
      const status = error.response.status
      if (status === 401) {
        // Let components handle 401 (redirect to login)
        return Promise.reject(error)
      }
      if (status >= 500) {
        console.error('[API] Server error:', error.response.data)
        toast.error('Server error. Please try again later.')
      }
    } else if (error.request) {
      toast.error('Network error. Check your connection.')
    }
    return Promise.reject(error)
  }
)
