import { request } from '../../api/client'

export type User = { id: string; name: string; email: string }

export async function authRequest<T>(path: string, body?: object): Promise<T> {
  try {
    return await request<T>(`/auth/${path}`, {
    method: path === 'me' ? 'GET' : 'POST',
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
    }, path === 'login'
      ? 'Email ou password inválidos, ou conta temporariamente bloqueada.'
      : undefined)
  } catch (error) {
    if (path === 'logout' && error instanceof Error && error.message === 'A sessão terminou. Entra novamente.') {
      return undefined as T
    }
    throw error
  }
}

export async function apiRequest<T>(path: string, query?: Record<string, string | number>): Promise<T> {
  const search = query ? `?${new URLSearchParams(Object.entries(query).map(([key, value]) => [key, String(value)]))}` : ''
  return request<T>(`${baseUrl}/api/${path}${search}`, { method: 'GET' })
}

export async function apiMutation<T>(
  path: string,
  method: 'POST' | 'PUT' | 'DELETE',
  body?: object,
): Promise<T> {
  return request<T>(`${baseUrl}/api/${path}`, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}
