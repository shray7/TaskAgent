import { describe, it, expect, beforeEach } from 'vitest'
import { api, normalizeTaskFromPayload, resetMockData, getMockData } from '../api'

describe('normalizeTaskFromPayload', () => {
  it('normalizes task from payload with camelCase/ISO dates', () => {
    const payload = {
      id: 1,
      title: 'Task',
      description: 'Desc',
      status: 'todo',
      priority: 'high',
      assigneeId: 1,
      createdBy: 1,
      createdAt: '2024-01-15T10:00:00.000Z',
      dueDate: '2024-01-20T00:00:00.000Z',
      tags: ['a', 'b'],
      projectId: 1,
      sprintId: 2,
      size: 5
    }
    const task = normalizeTaskFromPayload(payload)
    expect(task.id).toBe(1)
    expect(task.title).toBe('Task')
    expect(task.status).toBe('todo')
    expect(task.priority).toBe('high')
    expect(task.tags).toEqual(['a', 'b'])
    expect(task.createdAt).toBeInstanceOf(Date)
    expect(task.dueDate).toBeInstanceOf(Date)
  })

  it('uses defaults for missing optional fields', () => {
    const payload = {
      id: 1,
      title: 'T',
      assigneeId: 1,
      createdBy: 1,
      projectId: 1
    }
    const task = normalizeTaskFromPayload(payload)
    expect(task.description).toBe('')
    expect(task.status).toBe('todo')
    expect(task.priority).toBe('medium')
    expect(task.tags).toEqual([])
    expect(task.sprintId).toBeUndefined()
  })
})

describe('api (mock mode)', () => {
  beforeEach(() => {
    resetMockData()
  })

  describe('auth', () => {
    it('login succeeds for demo user', async () => {
      const result = await api.auth.login('alice@example.com', 'any')
      expect(result.success).toBe(true)
      expect(result.user).toBeDefined()
      expect(result.user?.name).toBe('Alice Johnson')
      expect(result.user?.email).toBe('alice@example.com')
    })

    it('login fails for unknown email', async () => {
      const result = await api.auth.login('unknown@example.com', 'pass')
      expect(result.success).toBe(false)
      expect(result.message).toMatch(/invalid|password/i)
    })

    it('register rejects duplicate email', async () => {
      const result = await api.auth.register('alice@example.com', 'Alice', 'password123')
      expect(result.success).toBe(false)
      expect(result.message).toMatch(/already registered/i)
    })

    it('register creates new user', async () => {
      const result = await api.auth.register('new@example.com', 'New User', 'password123')
      expect(result.success).toBe(true)
      expect(result.user?.email).toBe('new@example.com')
      expect(result.user?.name).toBe('New User')
      const mock = getMockData()
      expect(mock.users.some((u) => u.email === 'new@example.com')).toBe(true)
    })
  })

  describe('users', () => {
    it('getAll returns mock users', async () => {
      const users = await api.users.getAll()
      expect(users.length).toBeGreaterThanOrEqual(4)
      expect(users.some((u) => u.email === 'alice@example.com')).toBe(true)
    })

    it('getByEmail returns user when found', async () => {
      const user = await api.users.getByEmail('alice@example.com')
      expect(user).not.toBeNull()
      expect(user?.name).toBe('Alice Johnson')
    })

    it('getByEmail returns null when not found', async () => {
      const user = await api.users.getByEmail('nobody@example.com')
      expect(user).toBeNull()
    })
  })

  describe('projects', () => {
    it('getAll returns mock projects', async () => {
      const projects = await api.projects.getAll()
      expect(projects.length).toBeGreaterThanOrEqual(1)
    })

    it('getById returns project when found', async () => {
      const project = await api.projects.getById(1)
      expect(project).not.toBeNull()
      expect(project?.id).toBe(1)
    })

    it('getById returns null when not found', async () => {
      const project = await api.projects.getById(99999)
      expect(project).toBeNull()
    })
  })

  describe('tasks', () => {
    it('getAll returns mock tasks', async () => {
      const tasks = await api.tasks.getAll()
      expect(tasks.length).toBeGreaterThanOrEqual(1)
    })

    it('getAll filters by projectId when provided', async () => {
      const tasks = await api.tasks.getAll({ projectId: 1 })
      expect(tasks.every((t) => t.projectId === 1)).toBe(true)
    })

    it('getById returns task when found', async () => {
      const task = await api.tasks.getById(1)
      expect(task).not.toBeNull()
      expect(task?.id).toBe(1)
    })
  })
})
