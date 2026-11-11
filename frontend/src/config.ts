// config.d.ts is shadowed by config.ts (same basename), so include it explicitly.
// oxlint-disable-next-line typescript/triple-slash-reference
/// <reference path="./config.d.ts" />

const DEFAULT_API_BASE_URL = 'http://localhost:8080'

export function getApiBaseUrl(): string {
  const configured = window.APP_CONFIG?.apiBaseUrl
  if (typeof configured !== 'string' || configured === '') {
    return DEFAULT_API_BASE_URL
  }
  return configured.endsWith('/') ? configured.slice(0, -1) : configured
}
