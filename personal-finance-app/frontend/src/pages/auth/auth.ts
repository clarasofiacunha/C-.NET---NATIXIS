import { request } from '../../api/client'

export type User = { id: string; name: string; email: string }

export async function authRequest<T>(path: string, body?: object): Promise<T> {
  try {
    return await request<T>(`/auth/${path}`, {
    method: path === 'me' ? 'GET' : 'POST',
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
    }, path === 'login'
      ? 'Invalid email or password, or the account is temporarily locked.'
      : undefined)
  } catch (error) {
    if (path === 'logout' && error instanceof Error && error.message === 'Your session has ended. Please sign in again.') {
      return undefined as T
    }
    throw error
  }
}
