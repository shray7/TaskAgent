import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useThemeStore } from '../theme'

const classListMock = {
  add: vi.fn(),
  remove: vi.fn()
}

describe('useThemeStore', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    Object.defineProperty(document.documentElement, 'classList', {
      value: classListMock,
      configurable: true
    })
    setActivePinia(createPinia())
  })

  it('starts with light theme', () => {
    const store = useThemeStore()
    expect(store.theme).toBe('light')
    expect(store.resolvedTheme).toBe('light')
  })

  it('setTheme updates theme and resolvedTheme', () => {
    const store = useThemeStore()
    store.setTheme('dark')
    expect(store.theme).toBe('dark')
    expect(store.resolvedTheme).toBe('dark')
  })

  it('applyTheme adds class to documentElement', () => {
    const store = useThemeStore()
    store.setTheme('dark')
    expect(classListMock.remove).toHaveBeenCalledWith('light', 'dark')
    expect(classListMock.add).toHaveBeenCalledWith('dark')
  })

  it('toggleTheme switches between light and dark', () => {
    const store = useThemeStore()
    expect(store.theme).toBe('light')
    store.toggleTheme()
    expect(store.theme).toBe('dark')
    store.toggleTheme()
    expect(store.theme).toBe('light')
  })

  it('applyThemeAfterRestore normalizes system to light', () => {
    const store = useThemeStore()
    store.setTheme('dark')
    // Simulate persisted value that might be 'system'
    store.theme = 'system' as 'light' | 'dark'
    store.applyThemeAfterRestore()
    expect(store.theme).toBe('light')
  })

  it('applyThemeAfterRestore leaves light/dark unchanged', () => {
    const store = useThemeStore()
    store.setTheme('dark')
    store.applyThemeAfterRestore()
    expect(store.theme).toBe('dark')
  })
})
