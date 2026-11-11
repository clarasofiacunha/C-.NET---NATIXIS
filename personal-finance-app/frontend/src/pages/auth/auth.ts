export type User = { id: string; name: string; email: string }

const baseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

async function request<T>(url: string, options: RequestInit, unauthorizedMessage = 'A sessão terminou. Entra novamente.'): Promise<T> {
  let response: Response
  try {
    response = await fetch(url, {
      credentials: 'include',
      signal: AbortSignal.timeout(15000),
      ...options,
    })
  } catch {
    throw new Error('Não foi possível contactar o servidor. Tenta novamente.')
  }
  if (!response.ok) {
    const data = await response.json().catch(() => null)
    const messages = data?.errors ? Object.values(data.errors).flat().filter(value => typeof value === 'string') : []
    throw new Error(response.status === 401
      ? unauthorizedMessage
      : messages.length ? messages.join(' ') : data?.error ?? 'Não foi possível concluir o pedido. Tenta novamente.')
  }
  return response.status === 204 ? undefined as T : response.json()
}

export async function authRequest<T>(path: string, body?: object): Promise<T> {
  try {
    return await request<T>(`${baseUrl}/auth/${path}`, {
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
