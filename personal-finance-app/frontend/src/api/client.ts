const baseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

export async function request<T>(
  url: string,
  options: RequestInit,
  unauthorizedMessage = 'Your session has ended. Please sign in again.',
): Promise<T> {
  let response: Response
  try {
    response = await fetch(url, {
      credentials: 'include',
      signal: AbortSignal.timeout(15000),
      ...options,
    })
  } catch {
    throw new Error('Unable to reach the server. Please try again.')
  }
  if (!response.ok) {
    const data = await response.json().catch(() => null)
    const messages = data?.errors
      ? Object.values(data.errors).flat().filter(value => typeof value === 'string')
      : []
    throw new Error(response.status === 401
      ? unauthorizedMessage
      : messages.length ? messages.join(' ') : data?.error ?? 'Unable to complete the request. Please try again.')
  }
  return response.status === 204 ? undefined as T : response.json()
}

export async function apiRequest<T>(
  path: string,
  query?: Record<string, string | number>,
): Promise<T> {
  const search = query
    ? `?${new URLSearchParams(Object.entries(query).map(([key, value]) => [key, String(value)]))}`
    : ''
  return request<T>(`${baseUrl}/api/${path}${search}`, { method: 'GET' })
}

export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// Percorre todas as paginas de um endpoint paginado (ex.: graficos que precisam do historico completo).
export async function apiRequestAllPages<T>(
  path: string,
  query: Record<string, string | number> = {},
  pageSize = 100,
): Promise<T[]> {
  const items: T[] = []
  for (let page = 1; ; page++) {
    const result = await apiRequest<PagedResponse<T>>(path, { ...query, page, pageSize })
    items.push(...result.items)
    if (page >= result.totalPages) return items
  }
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
