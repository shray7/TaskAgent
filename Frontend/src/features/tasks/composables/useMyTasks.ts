import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTasksStore } from '@/stores/tasks'
import { useProjectsStore } from '@/stores/projects'
import { useSprintsStore } from '@/stores/sprints'
import { getInitials, getUserColor } from '@/utils/initials'
import type { Task, TaskStatus, TaskPriority } from '@/types'

export function useMyTasks() {
  const router = useRouter()
  const authStore = useAuthStore()
  const tasksStore = useTasksStore()
  const projectsStore = useProjectsStore()
  const sprintsStore = useSprintsStore()

  const selectedProjectId = ref<number | null>(null)
  const selectedStatus = ref<TaskStatus | ''>('')
  const selectedPriority = ref<TaskPriority | ''>('')
  const showTaskForm = ref(false)
  const selectedTask = ref<Task | null>(null)

  const hasActiveFilters = computed(
    () => selectedProjectId.value !== null || selectedStatus.value !== '' || selectedPriority.value !== ''
  )

  const myTasks = computed(() => {
    if (!authStore.user) return []
    return tasksStore.tasks.filter((task) => task.assigneeId === authStore.user!.id)
  })

  const filteredTasks = computed(() => {
    let tasks = myTasks.value
    if (selectedProjectId.value !== null) {
      tasks = tasks.filter((t) => t.projectId === selectedProjectId.value)
    }
    if (selectedStatus.value) {
      tasks = tasks.filter((t) => t.status === selectedStatus.value)
    }
    if (selectedPriority.value) {
      tasks = tasks.filter((t) => t.priority === selectedPriority.value)
    }
    return tasks.sort((a, b) => {
      const priorityOrder = { high: 0, medium: 1, low: 2 }
      const pDiff = priorityOrder[a.priority] - priorityOrder[b.priority]
      if (pDiff !== 0) return pDiff
      if (a.dueDate && b.dueDate) return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
      if (a.dueDate) return -1
      if (b.dueDate) return 1
      return 0
    })
  })

  const todoTasks = computed(() => myTasks.value.filter((t) => t.status === 'todo'))
  const inProgressTasks = computed(() => myTasks.value.filter((t) => t.status === 'in-progress'))
  const completedTasks = computed(() => myTasks.value.filter((t) => t.status === 'completed'))
  const overdueTasks = computed(() => {
    const now = new Date()
    now.setHours(0, 0, 0, 0)
    return myTasks.value.filter((t) => {
      if (!t.dueDate || t.status === 'completed') return false
      const due = new Date(t.dueDate)
      due.setHours(0, 0, 0, 0)
      return due < now
    })
  })

  function clearFilters() {
    selectedProjectId.value = null
    selectedStatus.value = ''
    selectedPriority.value = ''
  }

  function priorityBadgeClass(priority: TaskPriority): string {
    return priority === 'high' ? 'badge-red' : priority === 'medium' ? 'badge-amber' : 'badge-gray'
  }

  function getProjectName(projectId: number): string {
    return projectsStore.getProjectById(projectId)?.name ?? 'Unknown'
  }

  function getProjectColor(projectId: number): string {
    return projectsStore.getProjectById(projectId)?.color ?? '#6366f1'
  }

  function getTaskSizeUnit(projectId: number): 'hours' | 'days' {
    return projectsStore.getTaskSizeUnit(projectId)
  }

  function getSprintsForTask(task: Task | null) {
    if (!task) return []
    return sprintsStore.sprints.filter((s) => s.projectId === task.projectId)
  }

  function isOverdue(task: Task): boolean {
    if (!task.dueDate || task.status === 'completed') return false
    const now = new Date()
    now.setHours(0, 0, 0, 0)
    const due = new Date(task.dueDate)
    due.setHours(0, 0, 0, 0)
    return due < now
  }

  function formatDueDate(date: Date | string | undefined): string {
    if (!date) return '-'
    const d = typeof date === 'string' ? new Date(date) : date
    const now = new Date()
    now.setHours(0, 0, 0, 0)
    const due = new Date(d)
    due.setHours(0, 0, 0, 0)
    const diff = Math.ceil((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))
    if (diff < 0) return `${Math.abs(diff)}d overdue`
    if (diff === 0) return 'Today'
    if (diff === 1) return 'Tomorrow'
    if (diff <= 7) return `${diff}d`
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  }

  function openTask(task: Task) {
    selectedTask.value = task
    showTaskForm.value = true
  }

  function closeTaskForm() {
    showTaskForm.value = false
    selectedTask.value = null
  }

  async function handleSaveTask(taskData: Partial<Task>) {
    if (selectedTask.value) await tasksStore.updateTask(selectedTask.value.id, taskData)
    closeTaskForm()
  }

  async function handleDeleteTask(taskId: number) {
    await tasksStore.deleteTask(taskId)
    closeTaskForm()
  }

  function handleLogout() {
    authStore.logout()
    router.push('/login')
  }

  onMounted(async () => {
    await authStore.loadUsers()
    await projectsStore.fetchProjects()
    await sprintsStore.fetchSprints()
    await tasksStore.fetchTasks()
  })

  return {
    authStore,
    tasksStore,
    projectsStore,
    sprintsStore,
    selectedProjectId,
    selectedStatus,
    selectedPriority,
    showTaskForm,
    selectedTask,
    hasActiveFilters,
    myTasks,
    filteredTasks,
    todoTasks,
    inProgressTasks,
    completedTasks,
    overdueTasks,
    clearFilters,
    priorityBadgeClass,
    getProjectName,
    getProjectColor,
    getTaskSizeUnit,
    getSprintsForTask,
    isOverdue,
    formatDueDate,
    openTask,
    closeTaskForm,
    handleSaveTask,
    handleDeleteTask,
    handleLogout,
    getInitials,
    getUserColor
  }
}
