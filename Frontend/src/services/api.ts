/**
 * API client for TaskAgent. All data flows through this layer.
 * Set VITE_API_BASE to your backend URL (e.g. http://localhost:5180).
 * When unset, uses in-memory mock data so the app works without a backend.
 */

import type { User, Project, Sprint, Task, TaskStatus, Comment } from '@/types'
import { DEFAULT_SPRINT_DURATION_DAYS, sprintDurationMs } from '@/types'

// Empty string = relative URLs (e.g. when served behind nginx proxy in Docker)
const baseUrl = (import.meta.env.VITE_API_BASE as string | undefined) ?? ''
const useHttp = (import.meta.env.VITE_API_BASE !== undefined && import.meta.env.VITE_API_BASE !== 'mock') || (baseUrl.length > 0 && baseUrl !== 'mock')

// --- Date normalization (API returns ISO strings) ---

function parseDate(value: string | Date | undefined): Date | undefined {
  if (value == null) return undefined
  if (value instanceof Date) return value
  const d = new Date(value)
  return isNaN(d.getTime()) ? undefined : d
}

function normalizeProject(p: Record<string, unknown>): Project {
  return {
    id: p.id as number,
    name: p.name as string,
    description: (p.description as string) ?? '',
    color: (p.color as string) ?? '#B05A36',
    createdAt: parseDate(p.createdAt as string) ?? new Date(),
    startDate: parseDate(p.startDate as string),
    endDate: parseDate(p.endDate as string),
    ownerId: p.ownerId as number,
    visibleToUserIds: p.visibleToUserIds as number[] | undefined,
    sprintDurationDays: (p.sprintDurationDays as number) ?? DEFAULT_SPRINT_DURATION_DAYS,
    visibleColumns: (p.visibleColumns as TaskStatus[] | undefined),
    taskSizeUnit: (p.taskSizeUnit as 'hours' | 'days') ?? 'hours'
  }
}

function normalizeComment(c: Record<string, unknown>): Comment {
  return {
    id: c.id as number,
    content: c.content as string,
    authorId: c.authorId as number,
    authorName: c.authorName as string,
    authorAvatar: c.authorAvatar as string,
    createdAt: parseDate(c.createdAt as string) ?? new Date(),
    projectId: c.projectId as number | undefined,
    taskId: c.taskId as number | undefined
  }
}

function normalizeSprint(s: Record<string, unknown>): Sprint {
  return {
    id: s.id as number,
    projectId: s.projectId as number,
    name: s.name as string,
    goal: (s.goal as string) ?? '',
    startDate: parseDate(s.startDate as string) ?? new Date(),
    endDate: parseDate(s.endDate as string) ?? new Date(),
    status: (s.status as Sprint['status']) ?? 'planning'
  }
}

function normalizeTask(t: Record<string, unknown>): Task {
  return {
    id: t.id as number,
    title: t.title as string,
    description: (t.description as string) ?? '',
    status: (t.status as Task['status']) ?? 'todo',
    priority: (t.priority as Task['priority']) ?? 'medium',
    assigneeId: t.assigneeId as number,
    createdBy: t.createdBy as number,
    createdAt: parseDate(t.createdAt as string) ?? new Date(),
    dueDate: parseDate(t.dueDate as string),
    tags: Array.isArray(t.tags) ? (t.tags as string[]) : [],
    projectId: t.projectId as number,
    sprintId: t.sprintId as number | undefined,
    size: t.size as number | undefined
  }
}

// --- HTTP client ---

async function fetchJson<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers }
  })
  if (!res.ok) throw new Error(`API error ${res.status}: ${res.statusText}`)
  return res.json()
}

// --- Mock data (used when no backend) ---

const defaultColumns: TaskStatus[] = ['todo', 'in-progress', 'completed']

function createMockData(): {
  users: User[]
  projects: Project[]
  sprints: Sprint[]
  tasks: Task[]
  comments: Comment[]
} {
  const users: User[] = [
    { id: 1, name: 'Alice Johnson', email: 'alice@example.com', avatar: '👩‍💼' },
    { id: 2, name: 'Bob Smith', email: 'bob@example.com', avatar: '👨‍💻' },
    { id: 3, name: 'Carol Davis', email: 'carol@example.com', avatar: '👩‍🎨' },
    { id: 4, name: 'David Wilson', email: 'david@example.com', avatar: '👨‍🔬' }
  ]

  const projects: Project[] = [
    {
      id: 1,
      name: 'TaskAgent Development',
      description: 'Main product development for TaskAgent application',
      color: '#B05A36',
      createdAt: new Date('2024-01-01'),
      startDate: new Date('2024-01-01'),
      endDate: new Date('2024-06-30'),
      ownerId: 1,
      visibleToUserIds: [1, 2],
      sprintDurationDays: 14,
      visibleColumns: defaultColumns,
      taskSizeUnit: 'hours'
    },
    {
      id: 2,
      name: 'Marketing Website',
      description: 'Company marketing website redesign',
      color: '#4F46E5',
      createdAt: new Date('2024-01-05'),
      startDate: new Date('2024-01-05'),
      endDate: new Date('2024-04-30'),
      ownerId: 2,
      visibleToUserIds: [2, 3],
      sprintDurationDays: 14,
      visibleColumns: defaultColumns,
      taskSizeUnit: 'days'
    },
    {
      id: 3,
      name: 'Mobile App',
      description: 'Native mobile application development',
      color: '#059669',
      createdAt: new Date('2024-01-10'),
      startDate: new Date('2024-01-10'),
      endDate: new Date('2024-12-31'),
      ownerId: 1,
      sprintDurationDays: 7,
      visibleColumns: defaultColumns,
      taskSizeUnit: 'hours'
    }
  ]

  const sprints: Sprint[] = [
    {
      id: 1,
      projectId: 1,
      name: 'Sprint 1',
      goal: 'Set up core infrastructure and authentication',
      startDate: new Date('2024-01-15'),
      endDate: new Date('2024-01-29'),
      status: 'completed'
    },
    {
      id: 2,
      projectId: 1,
      name: 'Sprint 2',
      goal: 'Implement task management features',
      startDate: new Date('2024-01-29'),
      endDate: new Date('2024-02-12'),
      status: 'active'
    },
    {
      id: 3,
      projectId: 1,
      name: 'Sprint 3',
      goal: 'Add collaboration and real-time features',
      startDate: new Date('2024-02-12'),
      endDate: new Date('2024-02-26'),
      status: 'planning'
    },
    {
      id: 4,
      projectId: 2,
      name: 'Sprint 1',
      goal: 'Design and implement landing page',
      startDate: new Date('2024-01-22'),
      endDate: new Date('2024-02-05'),
      status: 'active'
    },
    {
      id: 5,
      projectId: 3,
      name: 'Sprint 1',
      goal: 'Mobile app architecture and setup',
      startDate: new Date('2024-02-01'),
      endDate: new Date('2024-02-15'),
      status: 'planning'
    }
  ]

  const tasks: Task[] = [
    {
      id: 1,
      title: 'Design new landing page',
      description: 'Create mockups and wireframes for the new landing page design',
      status: 'todo',
      priority: 'high',
      assigneeId: 1,
      createdBy: 1,
      createdAt: new Date('2024-01-15'),
      dueDate: new Date('2024-01-25'),
      tags: ['design', 'frontend'],
      projectId: 1,
      sprintId: 2,
      size: 8
    },
    {
      id: 2,
      title: 'Implement user authentication',
      description: 'Set up JWT-based authentication system with login/logout functionality',
      status: 'in-progress',
      priority: 'high',
      assigneeId: 2,
      createdBy: 1,
      createdAt: new Date('2024-01-10'),
      dueDate: new Date('2024-01-20'),
      tags: ['backend', 'security'],
      projectId: 1,
      sprintId: 2,
      size: 16
    },
    {
      id: 3,
      title: 'Write API documentation',
      description: 'Document all REST API endpoints with examples',
      status: 'todo',
      priority: 'medium',
      assigneeId: 3,
      createdBy: 2,
      createdAt: new Date('2024-01-12'),
      dueDate: new Date('2024-01-30'),
      tags: ['documentation'],
      projectId: 1,
      sprintId: 2,
      size: 4
    },
    {
      id: 4,
      title: 'Fix responsive layout issues',
      description: 'Fix mobile layout problems on dashboard and task views',
      status: 'completed',
      priority: 'low',
      assigneeId: 4,
      createdBy: 3,
      createdAt: new Date('2024-01-08'),
      dueDate: new Date('2024-01-15'),
      tags: ['frontend', 'bug'],
      projectId: 1,
      sprintId: 1,
      size: 2
    },
    {
      id: 5,
      title: 'Add real-time notifications',
      description: 'Implement WebSocket-based notifications for task updates',
      status: 'todo',
      priority: 'medium',
      assigneeId: 2,
      createdBy: 1,
      createdAt: new Date('2024-02-01'),
      dueDate: new Date('2024-02-20'),
      tags: ['backend', 'websocket'],
      projectId: 1,
      sprintId: 3,
      size: 12
    },
    {
      id: 6,
      title: 'Performance optimization',
      description: 'Optimize database queries and implement caching',
      status: 'todo',
      priority: 'low',
      assigneeId: 3,
      createdBy: 1,
      createdAt: new Date('2024-02-05'),
      tags: ['backend', 'performance'],
      projectId: 1
    },
    {
      id: 7,
      title: 'Create hero section design',
      description: 'Design the hero section with animated illustrations',
      status: 'in-progress',
      priority: 'high',
      assigneeId: 1,
      createdBy: 2,
      createdAt: new Date('2024-01-22'),
      dueDate: new Date('2024-02-01'),
      tags: ['design'],
      projectId: 2,
      sprintId: 4,
      size: 2
    },
    {
      id: 8,
      title: 'Implement contact form',
      description: 'Build contact form with validation and email integration',
      status: 'todo',
      priority: 'medium',
      assigneeId: 4,
      createdBy: 2,
      createdAt: new Date('2024-01-25'),
      dueDate: new Date('2024-02-05'),
      tags: ['frontend', 'backend'],
      projectId: 2,
      sprintId: 4
    },
    {
      id: 9,
      title: 'Set up React Native project',
      description: 'Initialize React Native project with TypeScript and navigation',
      status: 'todo',
      priority: 'high',
      assigneeId: 2,
      createdBy: 1,
      createdAt: new Date('2024-02-01'),
      dueDate: new Date('2024-02-10'),
      tags: ['mobile', 'setup'],
      projectId: 3,
      sprintId: 5
    }
  ]

  const comments: Comment[] = [
    { id: 1, content: 'Excited to get started!', authorId: 1, authorName: 'Alice Johnson', authorAvatar: '👩‍💼', createdAt: new Date('2024-01-02'), projectId: 1 },
    { id: 2, content: 'Let\'s aim for MVP by end of Q1.', authorId: 2, authorName: 'Bob Smith', authorAvatar: '👨‍💻', createdAt: new Date('2024-01-03'), projectId: 1 },
    { id: 3, content: 'We should prioritize auth first.', authorId: 1, authorName: 'Alice Johnson', authorAvatar: '👩‍💼', createdAt: new Date('2024-01-12'), taskId: 2 }
  ]

  return { users, projects, sprints, tasks, comments }
}

let mockData = createMockData()

function getMock(): typeof mockData {
  return mockData
}

function setMock(data: Partial<typeof mockData>): void {
  mockData = { ...mockData, ...data }
}

// --- Public API ---

// Auth response type
interface AuthResponse {
  success: boolean
  message?: string
  user?: User
}

export const api = {
  get useHttp(): boolean {
    return useHttp
  },

  auth: {
    async login(email: string, password: string): Promise<AuthResponse> {
      if (useHttp) {
        try {
          const res = await fetch(`${baseUrl}/api/tm/users/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
          })
          const data = await res.json()
          if (!res.ok) {
            return { success: false, message: data.message || 'Login failed' }
          }
          return {
            success: data.success,
            message: data.message,
            user: data.user ? {
              id: data.user.id,
              name: data.user.name,
              email: data.user.email,
              avatar: data.user.avatar
            } : undefined
          }
        } catch {
          return { success: false, message: 'Login failed. Please try again.' }
        }
      }
      // Mock login - accept any password for demo users
      const user = getMock().users.find(u => u.email === email)
      if (user) {
        return { success: true, message: 'Login successful', user: { ...user } }
      }
      return { success: false, message: 'Invalid email or password' }
    },

    async register(email: string, name: string, password: string): Promise<AuthResponse> {
      if (useHttp) {
        try {
          const res = await fetch(`${baseUrl}/api/tm/users/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, name, password })
          })
          const data = await res.json()
          if (!res.ok) {
            return { success: false, message: data.message || 'Registration failed' }
          }
          return {
            success: data.success,
            message: data.message,
            user: data.user ? {
              id: data.user.id,
              name: data.user.name,
              email: data.user.email,
              avatar: data.user.avatar
            } : undefined
          }
        } catch {
          return { success: false, message: 'Registration failed. Please try again.' }
        }
      }
      // Mock registration
      const existing = getMock().users.find(u => u.email === email)
      if (existing) {
        return { success: false, message: 'Email already registered' }
      }
      const avatars = ['👩‍💼', '👨‍💻', '👩‍🎨', '👨‍🔬', '👩‍🏫', '👨‍🎤']
      const newUser: User = {
        id: Date.now(),
        name,
        email,
        avatar: avatars[Math.abs(name.length) % avatars.length]!
      }
      mockData.users.push(newUser)
      return { success: true, message: 'Registration successful', user: { ...newUser } }
    }
  },

  users: {
    async getAll(): Promise<User[]> {
      if (useHttp) {
        const list = await fetchJson<unknown[]>(`/api/tm/users`)
        return (list as Record<string, unknown>[]).map((u) => ({
          id: u.id as number,
          name: u.name as string,
          email: u.email as string,
          avatar: u.avatar as string
        }))
      }
      return [...getMock().users]
    },

    async getByEmail(email: string): Promise<User | null> {
      if (useHttp) {
        try {
          const u = await fetchJson<Record<string, unknown>>(
            `/api/tm/users/by-email/${encodeURIComponent(email)}`
          )
          return u ? { id: u.id as number, name: u.name as string, email: u.email as string, avatar: u.avatar as string } : null
        } catch {
          return null
        }
      }
      const user = getMock().users.find(u => u.email === email) ?? null
      return user ? { ...user } : null
    }
  },

  projects: {
    async getAll(): Promise<Project[]> {
      if (useHttp) {
        const list = await fetchJson<Record<string, unknown>[]>(`/api/tm/projects`)
        return list.map(normalizeProject)
      }
      return [...getMock().projects]
    },

    async getById(id: number): Promise<Project | null> {
      if (useHttp) {
        try {
          const p = await fetchJson<Record<string, unknown>>(`/api/tm/projects/${id}`)
          return normalizeProject(p)
        } catch {
          return null
        }
      }
      const p = getMock().projects.find(pr => pr.id === id)
      return p ? { ...p } : null
    },

    async create(data: Omit<Project, 'id' | 'createdAt'>): Promise<Project> {
      if (useHttp) {
        const body = {
          ...data,
          createdAt: new Date().toISOString()
        }
        const res = await fetch(`${baseUrl}/api/tm/projects`, {
          method: 'POST',
          body: JSON.stringify(body),
          headers: { 'Content-Type': 'application/json' }
        })
        const p = await res.json()
        return normalizeProject(p)
      }
      const newProject: Project = {
        id: Date.now(),
        ...data,
        createdAt: new Date()
      }
      mockData.projects.push(newProject)
      return { ...newProject }
    },

    async update(id: number, data: Partial<Project>): Promise<Project | null> {
      if (useHttp) {
        try {
          const res = await fetch(`${baseUrl}/api/tm/projects/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
            headers: { 'Content-Type': 'application/json' }
          })
          if (!res.ok) return null
          const p = await res.json()
          return normalizeProject(p)
        } catch {
          return null
        }
      }
      const idx = mockData.projects.findIndex(p => p.id === id)
      if (idx === -1) return null
      mockData.projects[idx] = { ...mockData.projects[idx]!, ...data }
      return { ...mockData.projects[idx]! }
    },

    async delete(id: number, userId: number): Promise<{ success: boolean; message?: string }> {
      if (useHttp) {
        const res = await fetch(`${baseUrl}/api/tm/projects/${id}?userId=${userId}`, { method: 'DELETE' })
        if (res.status === 403) {
          return { success: false, message: 'Only the project owner can delete this project' }
        }
        if (!res.ok) {
          return { success: false, message: 'Failed to delete project' }
        }
        return { success: true }
      }
      // Mock: check ownership
      const project = mockData.projects.find(p => p.id === id)
      if (!project) {
        return { success: false, message: 'Project not found' }
      }
      if (project.ownerId !== userId) {
        return { success: false, message: 'Only the project owner can delete this project' }
      }
      mockData.projects = mockData.projects.filter(p => p.id !== id)
      mockData.tasks = mockData.tasks.filter(t => t.projectId !== id)
      mockData.sprints = mockData.sprints.filter(s => s.projectId !== id)
      return { success: true }
    }
  },

  sprints: {
    async getAll(projectId?: number): Promise<Sprint[]> {
      if (useHttp) {
        const path = projectId ? `/api/tm/sprints?projectId=${projectId}` : '/api/tm/sprints'
        const list = await fetchJson<Record<string, unknown>[]>(path)
        return list.map(normalizeSprint)
      }
      let list = getMock().sprints
      if (projectId != null) list = list.filter(s => s.projectId === projectId)
      return list.map(s => ({ ...s }))
    },

    async getById(id: number): Promise<Sprint | null> {
      if (useHttp) {
        try {
          const s = await fetchJson<Record<string, unknown>>(`/api/tm/sprints/${id}`)
          return normalizeSprint(s)
        } catch {
          return null
        }
      }
      const s = getMock().sprints.find(sp => sp.id === id)
      return s ? { ...s } : null
    },

    async create(data: { projectId: number; name: string; goal?: string; startDate: Date }): Promise<Sprint> {
      if (useHttp) {
        const body = {
          projectId: data.projectId,
          name: data.name,
          goal: data.goal ?? '',
          startDate: data.startDate.toISOString()
        }
        const res = await fetch(`${baseUrl}/api/tm/sprints`, {
          method: 'POST',
          body: JSON.stringify(body),
          headers: { 'Content-Type': 'application/json' }
        })
        const s = await res.json()
        return normalizeSprint(s)
      }
      const durationDays = mockData.projects.find(p => p.id === data.projectId)?.sprintDurationDays ?? DEFAULT_SPRINT_DURATION_DAYS
      const endDate = new Date(data.startDate.getTime() + sprintDurationMs(durationDays))
      const newSprint: Sprint = {
        id: Date.now(),
        projectId: data.projectId,
        name: data.name,
        goal: data.goal ?? '',
        startDate: data.startDate,
        endDate,
        status: 'planning'
      }
      mockData.sprints.push(newSprint)
      return { ...newSprint }
    },

    async update(id: number, data: Partial<Sprint>): Promise<Sprint | null> {
      if (useHttp) {
        try {
          const res = await fetch(`${baseUrl}/api/tm/sprints/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
            headers: { 'Content-Type': 'application/json' }
          })
          if (!res.ok) return null
          const s = await res.json()
          return normalizeSprint(s)
        } catch {
          return null
        }
      }
      const idx = mockData.sprints.findIndex(s => s.id === id)
      if (idx === -1) return null
      mockData.sprints[idx] = { ...mockData.sprints[idx]!, ...data }
      return { ...mockData.sprints[idx]! }
    },

    async start(id: number): Promise<Sprint | null> {
      if (useHttp) {
        try {
          const res = await fetch(`${baseUrl}/api/tm/sprints/${id}/start`, { method: 'PUT' })
          if (!res.ok) return null
          const s = await res.json()
          return normalizeSprint(s)
        } catch {
          return null
        }
      }
      const sprint = mockData.sprints.find(s => s.id === id)
      if (!sprint || sprint.status !== 'planning') return null
      const sameProject = mockData.sprints.filter(s => s.projectId === sprint.projectId && s.status === 'active')
      sameProject.forEach(s => { s.status = 'completed' })
      sprint.status = 'active'
      return { ...sprint }
    },

    async complete(id: number): Promise<Sprint | null> {
      if (useHttp) {
        try {
          const res = await fetch(`${baseUrl}/api/tm/sprints/${id}/complete`, { method: 'PUT' })
          if (!res.ok) return null
          const s = await res.json()
          return normalizeSprint(s)
        } catch {
          return null
        }
      }
      const sprint = mockData.sprints.find(s => s.id === id)
      if (!sprint || sprint.status !== 'active') return null
      sprint.status = 'completed'
      return { ...sprint }
    },

    async delete(id: number): Promise<void> {
      if (useHttp) {
        await fetch(`${baseUrl}/api/tm/sprints/${id}`, { method: 'DELETE' })
        return
      }
      mockData.sprints = mockData.sprints.filter(s => s.id !== id)
    }
  },

  tasks: {
    async getAll(params?: { projectId?: number; sprintId?: number }): Promise<Task[]> {
      if (useHttp) {
        const q = new URLSearchParams()
        if (params?.projectId != null) q.set('projectId', String(params.projectId))
        if (params?.sprintId != null) q.set('sprintId', String(params.sprintId))
        const query = q.toString()
        const list = await fetchJson<Record<string, unknown>[]>(`/api/tm/tasks${query ? `?${query}` : ''}`)
        return list.map(normalizeTask)
      }
      let list = getMock().tasks
      if (params?.projectId != null) list = list.filter(t => t.projectId === params.projectId)
      if (params?.sprintId != null) list = list.filter(t => t.sprintId === params.sprintId)
      return list.map(t => ({ ...t }))
    },

    async getById(id: number): Promise<Task | null> {
      if (useHttp) {
        try {
          const t = await fetchJson<Record<string, unknown>>(`/api/tm/tasks/${id}`)
          return normalizeTask(t)
        } catch {
          return null
        }
      }
      const t = getMock().tasks.find(tk => tk.id === id)
      return t ? { ...t } : null
    },

    async create(data: Omit<Task, 'id' | 'createdAt'>): Promise<Task> {
      if (useHttp) {
        const body = {
          ...data,
          createdAt: new Date().toISOString(),
          dueDate: data.dueDate ? (data.dueDate instanceof Date ? data.dueDate : new Date(data.dueDate)).toISOString() : undefined
        }
        const res = await fetch(`${baseUrl}/api/tm/tasks`, {
          method: 'POST',
          body: JSON.stringify(body),
          headers: { 'Content-Type': 'application/json' }
        })
        const t = await res.json()
        return normalizeTask(t)
      }
      const newTask: Task = {
        id: Date.now(),
        ...data,
        createdAt: new Date(),
        status: data.status ?? 'todo'
      }
      mockData.tasks.push(newTask)
      return { ...newTask }
    },

    async update(id: number, data: Partial<Task>): Promise<Task | null> {
      if (useHttp) {
        try {
          const res = await fetch(`${baseUrl}/api/tm/tasks/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
            headers: { 'Content-Type': 'application/json' }
          })
          if (!res.ok) return null
          const t = await res.json()
          return normalizeTask(t)
        } catch {
          return null
        }
      }
      const idx = mockData.tasks.findIndex(t => t.id === id)
      if (idx === -1) return null
      mockData.tasks[idx] = { ...mockData.tasks[idx]!, ...data }
      return { ...mockData.tasks[idx]! }
    },

    async delete(id: number): Promise<void> {
      if (useHttp) {
        await fetch(`${baseUrl}/api/tm/tasks/${id}`, { method: 'DELETE' })
        return
      }
      mockData.tasks = mockData.tasks.filter(t => t.id !== id)
    }
  },

  comments: {
    async getByProject(projectId: number): Promise<Comment[]> {
      if (useHttp) {
        const list = await fetchJson<Record<string, unknown>[]>(`/api/tm/comments/project/${projectId}`)
        return list.map(normalizeComment)
      }
      return getMock().comments.filter(c => c.projectId === projectId).map(c => ({ ...c }))
    },

    async getByTask(taskId: number): Promise<Comment[]> {
      if (useHttp) {
        const list = await fetchJson<Record<string, unknown>[]>(`/api/tm/comments/task/${taskId}`)
        return list.map(normalizeComment)
      }
      return getMock().comments.filter(c => c.taskId === taskId).map(c => ({ ...c }))
    },

    async create(data: { content: string; authorId: number; projectId?: number; taskId?: number }): Promise<Comment> {
      if (useHttp) {
        const res = await fetch(`${baseUrl}/api/tm/comments`, {
          method: 'POST',
          body: JSON.stringify(data),
          headers: { 'Content-Type': 'application/json' }
        })
        const c = await res.json()
        return normalizeComment(c)
      }
      const author = getMock().users.find(u => u.id === data.authorId)
      const newComment: Comment = {
        id: Date.now(),
        content: data.content,
        authorId: data.authorId,
        authorName: author?.name ?? 'Unknown',
        authorAvatar: author?.avatar ?? '👤',
        createdAt: new Date(),
        projectId: data.projectId,
        taskId: data.taskId
      }
      mockData.comments.push(newComment)
      return { ...newComment }
    },

    async delete(id: number): Promise<void> {
      if (useHttp) {
        await fetch(`${baseUrl}/api/tm/comments/${id}`, { method: 'DELETE' })
        return
      }
      mockData.comments = mockData.comments.filter(c => c.id !== id)
    }
  }
}

// Expose for tests that need to reset or read mock data
export function getMockData(): typeof mockData {
  return getMock()
}

export function resetMockData(): void {
  mockData = createMockData()
}
