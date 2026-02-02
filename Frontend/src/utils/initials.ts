/**
 * Get first initial + last initial from a user's name, for avatar display.
 */
export function getInitials(user: { name: string }): string {
  const parts = user.name.trim().split(/\s+/)
  if (parts.length >= 2) {
    const first = parts[0]?.[0]
    const last = parts[parts.length - 1]?.[0]
    return ((first ?? '') + (last ?? '')).toUpperCase() || '?'
  }
  return (parts[0]?.[0] ?? '?').toUpperCase()
}

/** Distinct colors for user identification (stable per userId). */
const USER_COLORS = [
  '#6366f1', '#8b5cf6', '#a855f7', '#d946ef',
  '#ec4899', '#f43f5e', '#ef4444', '#f97316',
  '#eab308', '#84cc16', '#22c55e', '#14b8a6',
  '#06b6d4', '#0ea5e9', '#3b82f6', '#4f46e5'
]

export function getUserColor(userId: number): string {
  return USER_COLORS[Math.abs(userId) % USER_COLORS.length] ?? '#6b7280'
}
