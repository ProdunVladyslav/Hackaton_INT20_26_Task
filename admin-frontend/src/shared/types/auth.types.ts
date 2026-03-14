export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  userId: string
  email: string
  userName: string
}

export interface MeResponse {
  userId: string
  email: string
  userName: string
  profileId: string
}
