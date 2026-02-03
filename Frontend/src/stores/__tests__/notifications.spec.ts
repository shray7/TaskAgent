import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useNotificationsStore } from '../notifications'

describe('useNotificationsStore', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    setActivePinia(createPinia())
  })

  it('starts with empty items', () => {
    const store = useNotificationsStore()
    expect(store.items).toEqual([])
  })

  it('showError adds error notification', () => {
    const store = useNotificationsStore()
    store.showError('Something failed')
    expect(store.items).toHaveLength(1)
    expect(store.items[0]).toMatchObject({ message: 'Something failed', type: 'error' })
  })

  it('showInfo adds info notification', () => {
    const store = useNotificationsStore()
    store.showInfo('Saved')
    expect(store.items).toHaveLength(1)
    expect(store.items[0]).toMatchObject({ message: 'Saved', type: 'info' })
  })

  it('add with default type is error', () => {
    const store = useNotificationsStore()
    store.showError('Error')
    expect(store.items[0]!.type).toBe('error')
  })

  it('dismiss removes notification by id', () => {
    const store = useNotificationsStore()
    store.showError('First')
    store.showInfo('Second')
    const id = store.items[0]!.id
    store.dismiss(id)
    expect(store.items).toHaveLength(1)
    expect(store.items[0]!.message).toBe('Second')
  })

  it('each notification has unique id', () => {
    const store = useNotificationsStore()
    store.showError('A')
    store.showError('B')
    const ids = store.items.map((n) => n.id)
    expect(new Set(ids).size).toBe(2)
  })
})
