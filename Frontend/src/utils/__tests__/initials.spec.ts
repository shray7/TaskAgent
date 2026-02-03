import { describe, it, expect } from 'vitest'
import { getInitials, getUserColor } from '../initials'

describe('getInitials', () => {
  it('returns first and last initial for full name', () => {
    expect(getInitials({ name: 'John Doe' })).toBe('JD')
    expect(getInitials({ name: 'Alice Johnson' })).toBe('AJ')
  })

  it('returns single initial for single name', () => {
    expect(getInitials({ name: 'Madonna' })).toBe('M')
    expect(getInitials({ name: 'Prince' })).toBe('P')
  })

  it('handles multiple words', () => {
    expect(getInitials({ name: 'Jean Claude Van Damme' })).toBe('JD')
  })

  it('trims whitespace', () => {
    expect(getInitials({ name: '  John   Doe  ' })).toBe('JD')
  })

  it('returns uppercase', () => {
    expect(getInitials({ name: 'john doe' })).toBe('JD')
  })

  it('returns ? for empty name', () => {
    expect(getInitials({ name: '' })).toBe('?')
    expect(getInitials({ name: '   ' })).toBe('?')
  })
})

describe('getUserColor', () => {
  it('returns a color from the palette', () => {
    const color = getUserColor(1)
    expect(color).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it('returns stable color for same userId', () => {
    expect(getUserColor(5)).toBe(getUserColor(5))
    expect(getUserColor(100)).toBe(getUserColor(100))
  })

  it('handles negative userId', () => {
    const color = getUserColor(-1)
    expect(color).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it('returns different colors for different ids', () => {
    const a = getUserColor(0)
    const b = getUserColor(1)
    const c = getUserColor(16)
    expect(a).not.toBe(b)
    expect(b).not.toBe(c)
  })
})
