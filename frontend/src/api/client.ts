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
  if (rest.body != null && !merged.has('Content-Type')) merged.set('Content-Type', 'application/json')
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

const NETWORK_MESSAGE = 'The admission server cannot be reached.'

// fetch and body reads reject with TypeError only for network failures.
// Anything else (an AbortError from a cancelled request) is rethrown as is.
function toNetworkError(error: unknown): ApiError {
  if (!(error instanceof TypeError)) throw error
  return new ApiError('NETWORK', 0, NETWORK_MESSAGE)
}

export async function apiFetch<T>(path: string, init: ApiInit = {}): Promise<T> {
  // Built outside the try: a bad header value is a caller error, not an outage.
  const request = buildInit(init)
  let response: Response
  let text: string
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, request)
    recordServerDate(response.headers.get('Date'))
    text = await response.text()
  } catch (error) {
    throw toNetworkError(error)
  }

  if (!response.ok) throw toApiError(response.status, text)
  if (text === '') return undefined as T
  const body = parseJson(text)
  if (body === undefined) throw new ApiError('REQUEST_ERROR', response.status, 'The server sent an unreadable response.')
  return body as T
}
