// Shared type definitions

export interface User {
  id: number
  name: string
  email: string
  avatar: string
}

export type TaskSizeUnit = 'hours' | 'days'

export interface Project {
  id: number
  name: string
  description: string
  color: string
  createdAt: Date
  /** Project start date */
  startDate?: Date
  /** Project end date */
  endDate?: Date
  ownerId: number
  /** User IDs who can see this board. Empty/undefined = visible to all. */
  visibleToUserIds?: number[]
  /** Sprint length in days. Default 14. */
  sprintDurationDays?: number
  /** Which status columns to show on the board. Default all. */
  visibleColumns?: TaskStatus[]
  /** Unit for task size estimates. Default 'hours'. */
  taskSizeUnit?: TaskSizeUnit
}

export interface Comment {
  id: number
  content: string
  authorId: number
  authorName: string
  authorAvatar: string
  createdAt: Date
  projectId?: number
  taskId?: number
}

export const DEFAULT_SPRINT_DURATION_DAYS = 14
export const SPRINT_DURATION_OPTIONS = [7, 14, 21, 28] as const
export const TASK_SIZE_UNITS = { Hours: 'hours', Days: 'days' } as const
export const COLUMN_OPTIONS: { value: TaskStatus; label: string }[] = [
  { value: 'todo', label: 'To Do' },
  { value: 'in-progress', label: 'In Progress' },
  { value: 'completed', label: 'Completed' }
]

export interface Sprint {
  id: number
  projectId: number
  name: string
  goal: string
  startDate: Date
  endDate: Date
  status: SprintStatus
}

export interface Task {
  id: number
  title: string
  description: string
  status: TaskStatus
  priority: TaskPriority
  assigneeId: number
  createdBy: number
  createdAt: Date
  dueDate?: Date
  tags: string[]
  projectId: number
  sprintId?: number
  /** Estimated size in the project's taskSizeUnit (hours or days). */
  size?: number
}

export type TaskStatus = 'todo' | 'in-progress' | 'completed'
export type TaskPriority = 'low' | 'medium' | 'high'
export type SprintStatus = 'planning' | 'active' | 'completed'
export type Theme = 'light' | 'dark' | 'system'

// Constants as maps (following cursor rules - avoid enums)
export const TASK_STATUS = {
  Todo: 'todo',
  InProgress: 'in-progress',
  Completed: 'completed'
} as const

export const TASK_PRIORITY = {
  Low: 'low',
  Medium: 'medium',
  High: 'high'
} as const

export const SPRINT_STATUS = {
  Planning: 'planning',
  Active: 'active',
  Completed: 'completed'
} as const

export const THEME = {
  Light: 'light',
  Dark: 'dark',
  System: 'system'
} as const

// Sprint duration: default 2 weeks (used when project has no sprintDurationDays)
export const SPRINT_DURATION_DAYS = 14
const MS_PER_DAY = 24 * 60 * 60 * 1000
export const SPRINT_DURATION_MS = SPRINT_DURATION_DAYS * MS_PER_DAY
export function sprintDurationMs(days: number): number {
  return days * MS_PER_DAY
}
