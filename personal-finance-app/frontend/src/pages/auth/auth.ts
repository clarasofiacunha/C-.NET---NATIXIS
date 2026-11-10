export type User = { id: string; name: string; email: string }

const baseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

export async function authRequest<T>(path: string, body?: object): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}/auth/${path}`, {
      method: path === 'me' ? 'GET' : 'POST',
      credentials: 'include',
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(15000),
    })
  } catch {
    throw new Error('Não foi possível contactar o servidor. Tenta novamente.')
  }
  if (!response.ok) {
    if (path === 'logout' && response.status === 401) return undefined as T
    const data = await response.json().catch(() => null)
    const messages = data?.errors ? Object.values(data.errors).flat().filter(value => typeof value === 'string') : []
    throw new Error(response.status === 401
      ? path === 'login' ? 'Email ou password inválidos, ou conta temporariamente bloqueada.' : 'A sessão terminou. Entra novamente.'
      : messages.length ? messages.join(' ') : data?.error ?? 'Não foi possível concluir o pedido. Tenta novamente.')
  }
  return response.status === 204 ? undefined as T : response.json()
}
