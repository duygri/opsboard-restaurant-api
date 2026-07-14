import { describe, expect, test } from 'vitest'

import { readStoredSession, storageKey } from './session'

describe('readStoredSession', () => {
  test('clears corrupt session JSON and falls back to null', () => {
    const values = new Map([[storageKey, '{broken']])
    const storage = {
      getItem: (key: string) => values.get(key) ?? null,
      setItem: (key: string, value: string) => values.set(key, value),
      removeItem: (key: string) => values.delete(key),
    }

    expect(readStoredSession(storage)).toBeNull()
    expect(values.has(storageKey)).toBe(false)
  })
})
