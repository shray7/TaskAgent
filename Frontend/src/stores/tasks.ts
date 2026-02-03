import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Task, TaskStatus } from '@/types'
import { api } from '@/services/api'
import { useProjectsStore } from './projects'
import { useSprintsStore } from './sprints'
import { useNotificationsStore } from './notifications'

export const useTasksStore = defineStore('tasks', () => {
  const tasks = ref<Task[]>([])
  const loading = ref(false)
  const loaded = ref(false)

  const filteredTasks = computed(() => {
    const projectsStore = useProjectsStore()
    const sprintsStore = useSprintsStore()
    return tasks.value.filter(task => {
      if (task.projectId !== projectsStore.currentProjectId) return false
      if (sprintsStore.currentSprintId !== null) {
        return task.sprintId === sprintsStore.currentSprintId
      }
      return true
    })
  })

  const todoTasks = computed(() => filteredTasks.value.filter(task => task.status === 'todo'))
  const inProgressTasks = computed(() => filteredTasks.value.filter(task => task.status === 'in-progress'))
  const completedTasks = computed(() => filteredTasks.value.filter(task => task.status === 'completed'))
  const backlogTasks = computed(() => {
    const projectsStore = useProjectsStore()
    return tasks.value.filter(task => task.projectId === projectsStore.currentProjectId && !task.sprintId)
  })

  async function fetchTasks(params?: { projectId?: number; sprintId?: number }): Promise<void> {
    if (loading.value) return
    loading.value = true
    try {
      tasks.value = await api.tasks.getAll(params)
      loaded.value = true
    } catch (err) {
      tasks.value = []
      useNotificationsStore().showError(err instanceof Error ? err.message : 'Failed to load tasks')
    } finally {
      loading.value = false
    }
  }

  async function addTask(taskData: Omit<Task, 'id' | 'createdAt'>): Promise<Task> {
    const projectsStore = useProjectsStore()
    const sprintsStore = useSprintsStore()
    const payload = {
      ...taskData,
      projectId: taskData.projectId ?? projectsStore.currentProjectId,
      sprintId: taskData.sprintId ?? sprintsStore.currentSprintId ?? undefined,
      status: taskData.status ?? 'todo'
    }
    const newTask = await api.tasks.create(payload)
    tasks.value.push(newTask)
    return newTask
  }

  async function updateTask(taskId: number, updates: Partial<Task>): Promise<Task | null> {
    const updated = await api.tasks.update(taskId, updates)
    if (updated) {
      const idx = tasks.value.findIndex(t => t.id === taskId)
      if (idx !== -1) tasks.value[idx] = updated
      return updated
    }
    return null
  }

  async function deleteTask(taskId: number): Promise<void> {
    await api.tasks.delete(taskId)
    tasks.value = tasks.value.filter(t => t.id !== taskId)
  }

  function getTasksByAssignee(assigneeId: number): Task[] {
    return tasks.value.filter(task => task.assigneeId === assigneeId)
  }

  function getTasksByStatus(status: TaskStatus): Task[] {
    return tasks.value.filter(task => task.status === status)
  }

  function getTasksByProject(projectId: number): Task[] {
    return tasks.value.filter(task => task.projectId === projectId)
  }

  function getTasksBySprint(sprintId: number): Task[] {
    return tasks.value.filter(task => task.sprintId === sprintId)
  }

  function getTasksByProjectAndSprint(projectId: number, sprintId: number | null): Task[] {
    return tasks.value.filter(task => {
      if (task.projectId !== projectId) return false
      if (sprintId === null) return !task.sprintId
      return task.sprintId === sprintId
    })
  }

  async function moveTaskToSprint(taskId: number, sprintId: number | undefined): Promise<Task | null> {
    return updateTask(taskId, { sprintId })
  }

  async function moveTaskToProject(taskId: number, projectId: number): Promise<Task | null> {
    return updateTask(taskId, { projectId, sprintId: undefined })
  }

  /** Apply realtime TaskCreated (from another client). */
  function applyTaskCreated(task: Task): void {
    if (tasks.value.some(t => t.id === task.id)) return
    tasks.value.push(task)
  }

  /** Apply realtime TaskUpdated (from another client). */
  function applyTaskUpdated(task: Task): void {
    const idx = tasks.value.findIndex(t => t.id === task.id)
    if (idx !== -1) tasks.value[idx] = task
  }

  /** Apply realtime TaskDeleted (from another client). */
  function applyTaskDeleted(taskId: number): void {
    tasks.value = tasks.value.filter(t => t.id !== taskId)
  }

  return {
    tasks,
    filteredTasks,
    todoTasks,
    inProgressTasks,
    completedTasks,
    backlogTasks,
    loading,
    loaded,
    fetchTasks,
    addTask,
    updateTask,
    deleteTask,
    getTasksByAssignee,
    getTasksByStatus,
    getTasksByProject,
    getTasksBySprint,
    getTasksByProjectAndSprint,
    moveTaskToSprint,
    moveTaskToProject,
    applyTaskCreated,
    applyTaskUpdated,
    applyTaskDeleted
  }
})
