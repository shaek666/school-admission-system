import { getApiBaseUrl } from '../config'
import { recordServerDate } from './clock'

export class ApiError extends Error {
  readonly code: string
  readonly status: number

  constructor(code: string, status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
  }
}

export type ApiInit = RequestInit & { studentId?: string }

export const STUDENT_HEADER = 'X-Student-Id'

function buildInit({ studentId, headers, ...rest }: ApiInit): RequestInit {
  const merged = new Headers(headers)
  if (rest.body != null) merged.set('Content-Type', 'application/json')
  if (studentId !== undefined) merged.set(STUDENT_HEADER, studentId)
  return { ...rest, headers: merged }
}

function parseJson(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

function toApiError(status: number, text: string): ApiError {
  const body = parseJson(text) as { error?: { code?: unknown; message?: unknown } } | undefined
  const code = body?.error?.code
  const message = body?.error?.message
  if (typeof code === 'string' && code !== '') {
    return new ApiError(code, status, typeof message === 'string' ? message : `Request failed (${status}).`)
  }
  return new ApiError('REQUEST_ERROR', status, `Request failed (${status}).`)
}

export async function apiFetch<T>(path: string, init: ApiInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, buildInit(init))
  } catch (error) {
    // fetch rejects with TypeError only for network failures. Anything else
    // (an AbortError from a cancelled request) is not a server outage.
    if (!(error instanceof TypeError)) throw error
    throw new ApiError('NETWORK', 0, 'The admission server cannot be reached.')
  }
  recordServerDate(response.headers.get('Date'))

  const text = await response.text()
  if (!response.ok) throw toApiError(response.status, text)
  return (text === '' ? undefined : JSON.parse(text)) as T
}
