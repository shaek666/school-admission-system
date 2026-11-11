import { afterEach, describe, expect, it } from 'vitest'
import { DEFAULT_API_BASE_URL, getApiBaseUrl } from './config'


describe('getApiBaseUrl', () => {
  afterEach(() => {
    delete window.APP_CONFIG
  })

  it('returns the configured value', () => {
    window.APP_CONFIG = { apiBaseUrl: 'http://api.example.test:9000' }
    expect(getApiBaseUrl()).toBe('http://api.example.test:9000')
  })

  it('falls back to the default without APP_CONFIG', () => {
    expect(getApiBaseUrl()).toBe(DEFAULT_API_BASE_URL)
  })

  it('falls back when apiBaseUrl is missing or empty', () => {
    window.APP_CONFIG = {}
    expect(getApiBaseUrl()).toBe(DEFAULT_API_BASE_URL)
    window.APP_CONFIG = { apiBaseUrl: '' }
    expect(getApiBaseUrl()).toBe(DEFAULT_API_BASE_URL)
  })

  it('strips one trailing slash', () => {
    window.APP_CONFIG = { apiBaseUrl: 'http://api.example.test/' }
    expect(getApiBaseUrl()).toBe('http://api.example.test')
  })
})
