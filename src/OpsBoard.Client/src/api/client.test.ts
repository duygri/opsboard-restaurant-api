import { afterEach, describe, expect, test, vi } from 'vitest'

import { ApiError, apiRequest } from './client'

describe('apiRequest', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  test('throws an ApiError with the response status', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: () => Promise.resolve({ detail: 'Unauthorized' }),
    }))

    await expect(apiRequest('/api/orders', null)).rejects.toMatchObject({
      status: 401,
      message: 'Unauthorized',
    })
    await expect(apiRequest('/api/orders', null)).rejects.toBeInstanceOf(ApiError)
  })
})
