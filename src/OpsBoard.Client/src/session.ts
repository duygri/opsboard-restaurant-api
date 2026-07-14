import type { Session } from './types'

export const storageKey = 'opsboard.session'

type SessionStorageLike = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>

function browserStorage() {
  return window.localStorage
}

export function readStoredSession(storage: SessionStorageLike = browserStorage()) {
  const raw = storage.getItem(storageKey)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as Session
  } catch {
    storage.removeItem(storageKey)
    return null
  }
}

export function writeStoredSession(session: Session, storage: SessionStorageLike = browserStorage()) {
  storage.setItem(storageKey, JSON.stringify(session))
}

export function clearStoredSession(storage: SessionStorageLike = browserStorage()) {
  storage.removeItem(storageKey)
}
