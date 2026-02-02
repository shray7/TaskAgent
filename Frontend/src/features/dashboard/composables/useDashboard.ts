import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTasksStore } from '@/stores/tasks'
import { useProjectsStore } from '@/stores/projects'
import { useSprintsStore } from '@/stores/sprints'
import { getInitials, getUserColor } from '@/utils/initials'
import { ListTodo, Clock, CheckCircle } from 'lucide-vue-next'

export function useDashboard() {
  const router = useRouter()
  const authStore = useAuthStore()
  const tasksStore = useTasksStore()
  const projectsStore = useProjectsStore()
  const sprintsStore = useSprintsStore()

  const showTaskForm = ref(false)
  const editingTask = ref<any>(null)
  const showAddMemberModal = ref(false)
  const viewMode = ref<'list' | 'columns' | 'analytics'>(
    (localStorage.getItem('taskViewMode') as 'list' | 'columns' | 'analytics') || 'list'
  )
  const draggedTask = ref<any>(null)
  const dragOverColumn = ref<string | null>(null)
  const sortBy = ref<'' | 'title' | 'status' | 'priority' | 'assigneeId' | 'dueDate' | 'size'>('status')
  const sortDir = ref<'asc' | 'desc'>('asc')
  const filters = ref({ assigneeId: '', priority: '' })

  watch(viewMode, (newMode) => {
    localStorage.setItem('taskViewMode', newMode)
  })

  watch(
    () => projectsStore.switchToDashboard,
    (val) => {
      if (val) {
        viewMode.value = 'analytics'
        projectsStore.setSwitchToDashboard(false)
      }
    },
    { flush: 'sync' }
  )

  const todoTasks = computed(() => tasksStore.todoTasks)
  const inProgressTasks = computed(() => tasksStore.inProgressTasks)
  const completedTasks = computed(() => tasksStore.completedTasks)
  const currentProject = computed(() => projectsStore.currentProject)
  const currentSprint = computed(() => sprintsStore.currentSprint)
  const taskSizeUnit = computed(() =>
    projectsStore.getTaskSizeUnit(projectsStore.currentProjectId)
  )

  const columnConfig = {
    todo: {
      label: 'To Do',
      dotClass: 'status-dot-blue',
      emptyMsg: 'No tasks to do',
      emptyDropMsg: 'Drop tasks here',
      icon: ListTodo
    },
    'in-progress': {
      label: 'In Progress',
      dotClass: 'status-dot-amber',
      emptyMsg: 'No tasks in progress',
      emptyDropMsg: 'Drop tasks here',
      icon: Clock
    },
    completed: {
      label: 'Completed',
      dotClass: 'status-dot-green',
      emptyMsg: 'No completed tasks',
      emptyDropMsg: 'Drop tasks here',
      icon: CheckCircle
    }
  } as const

  const visibleColumnConfigs = computed(() => {
    const statuses = projectsStore.getVisibleColumns(projectsStore.currentProjectId)
    const taskLists = {
      todo: filteredTodoTasks.value,
      'in-progress': filteredInProgressTasks.value,
      completed: filteredCompletedTasks.value
    }
    return statuses.map((status) => ({
      status,
      ...columnConfig[status],
      tasks: taskLists[status]
    }))
  })

  function filterTasks(tasks: any[]) {
    return tasks.filter((task) => {
      if (filters.value.assigneeId && task.assigneeId !== Number(filters.value.assigneeId))
        return false
      if (filters.value.priority && task.priority !== filters.value.priority) return false
      return true
    })
  }

  const filteredTodoTasks = computed(() => filterTasks(todoTasks.value))
  const filteredInProgressTasks = computed(() => filterTasks(inProgressTasks.value))
  const filteredCompletedTasks = computed(() => filterTasks(completedTasks.value))
  const allFilteredTasks = computed(() => [
    ...filteredTodoTasks.value,
    ...filteredInProgressTasks.value,
    ...filteredCompletedTasks.value
  ])

  const sortedAllTasks = computed(() => {
    const tasks = [...allFilteredTasks.value]
    if (!sortBy.value) return tasks
    const statusOrder = { todo: 0, 'in-progress': 1, completed: 2 }
    const priorityOrder = { high: 0, medium: 1, low: 2 }
    const getAssigneeName = (id: number) =>
      authStore.users.find((u) => u.id === id)?.name ?? 'Unassigned'

    tasks.sort((a, b) => {
      let aVal: any, bVal: any
      switch (sortBy.value) {
        case 'title':
          aVal = a.title.toLowerCase()
          bVal = b.title.toLowerCase()
          break
        case 'status':
          aVal = statusOrder[a.status as keyof typeof statusOrder] ?? 99
          bVal = statusOrder[b.status as keyof typeof statusOrder] ?? 99
          break
        case 'priority':
          aVal = priorityOrder[a.priority as keyof typeof priorityOrder] ?? 99
          bVal = priorityOrder[b.priority as keyof typeof priorityOrder] ?? 99
          break
        case 'assigneeId':
          aVal = getAssigneeName(a.assigneeId).toLowerCase()
          bVal = getAssigneeName(b.assigneeId).toLowerCase()
          break
        case 'dueDate':
          aVal = a.dueDate ? new Date(a.dueDate).getTime() : Infinity
          bVal = b.dueDate ? new Date(b.dueDate).getTime() : Infinity
          break
        case 'size':
          aVal = a.size ?? Infinity
          bVal = b.size ?? Infinity
          break
        default:
          return 0
      }
      if (aVal < bVal) return sortDir.value === 'asc' ? -1 : 1
      if (aVal > bVal) return sortDir.value === 'asc' ? 1 : -1
      return 0
    })
    return tasks
  })

  const projectTeamMembers = computed(() => {
    const project = currentProject.value
    if (!project) return authStore.users
    const ids = project.visibleToUserIds
    if (!ids || ids.length === 0) return authStore.users
    return authStore.users.filter((u) => ids.includes(u.id))
  })

  function statusLabel(status: string): string {
    switch (status) {
      case 'todo':
        return 'To Do'
      case 'in-progress':
        return 'In Progress'
      case 'completed':
        return 'Done'
      default:
        return status
    }
  }

  function getAssigneeName(assigneeId: number): string {
    return authStore.users.find((u) => u.id === assigneeId)?.name ?? 'Unassigned'
  }

  function getAssigneeInitials(assigneeId: number): string {
    const user = authStore.users.find((u) => u.id === assigneeId)
    return user ? getInitials(user) : '?'
  }

  function formatDueDate(dueDate?: Date | string): string {
    if (!dueDate) return '-'
    const date = typeof dueDate === 'string' ? new Date(dueDate) : dueDate
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  }

  function getUserTaskCount(userId: number) {
    return tasksStore.tasks.filter((t: { assigneeId: number }) => t.assigneeId === userId).length
  }

  function isProjectMember(userId: number): boolean {
    const project = currentProject.value
    if (!project) return true
    const ids = project.visibleToUserIds
    if (!ids || ids.length === 0) return true
    return ids.includes(userId)
  }

  async function toggleProjectMember(userId: number) {
    const project = currentProject.value
    if (!project || project.ownerId !== (authStore.user?.id ?? 0)) return
    const ids = project.visibleToUserIds ?? authStore.users.map((u) => u.id)
    const next = ids.includes(userId) ? ids.filter((id) => id !== userId) : [...ids, userId]
    await projectsStore.updateProject(project.id, {
      visibleToUserIds: next.length === 0 ? undefined : next
    })
  }

  function toggleSort(column: typeof sortBy.value) {
    if (sortBy.value === column) {
      sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
    } else {
      sortBy.value = column
      sortDir.value = 'asc'
    }
  }

  function toggleSortDirection() {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  }

  function clearSort() {
    sortBy.value = ''
    sortDir.value = 'asc'
  }

  function clearFilters() {
    filters.value = { assigneeId: '', priority: '' }
  }

  const handleLogout = () => {
    authStore.logout()
    router.push('/login')
  }

  async function handleDeleteTask(taskId: number) {
    if (confirm('Are you sure you want to delete this task?')) {
      await tasksStore.deleteTask(taskId)
    }
  }

  async function handleStatusChange(taskId: number, newStatus: string) {
    await tasksStore.updateTask(taskId, { status: newStatus as 'todo' | 'in-progress' | 'completed' })
  }

  async function handleInlineUpdate(taskId: number, updates: Record<string, unknown>) {
    await tasksStore.updateTask(taskId, updates)
  }

  function handleDragStart(event: DragEvent, task: any) {
    draggedTask.value = task
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move'
      event.dataTransfer.setData('text/plain', task.id.toString())
    }
    setTimeout(() => {
      (event.target as HTMLElement).classList.add('dragging')
    }, 0)
  }

  function handleDragEnd(event: DragEvent) {
    (event.target as HTMLElement).classList.remove('dragging')
    draggedTask.value = null
    dragOverColumn.value = null
  }

  function handleDragOver(event: DragEvent, status: string) {
    if (viewMode.value !== 'columns') return
    event.preventDefault()
    dragOverColumn.value = status
  }

  function handleDragLeave(event: DragEvent) {
    const relatedTarget = event.relatedTarget as HTMLElement
    const currentTarget = event.currentTarget as HTMLElement
    if (!currentTarget.contains(relatedTarget)) {
      dragOverColumn.value = null
    }
  }

  async function handleDrop(event: DragEvent, newStatus: string) {
    event.preventDefault()
    dragOverColumn.value = null
    if (draggedTask.value && draggedTask.value.status !== newStatus) {
      await tasksStore.updateTask(draggedTask.value.id, {
        status: newStatus as 'todo' | 'in-progress' | 'completed'
      })
    }
    draggedTask.value = null
  }

  function handleEditTask(task: any) {
    editingTask.value = task
    showTaskForm.value = true
  }

  async function handleTaskSubmit(taskData: Record<string, unknown>) {
    if (editingTask.value) {
      await tasksStore.updateTask((editingTask.value as { id: number }).id, taskData as Partial<import('@/types').Task>)
    } else {
      await tasksStore.addTask(taskData as Omit<import('@/types').Task, 'id' | 'createdAt'>)
    }
    closeTaskForm()
  }

  function closeTaskForm() {
    showTaskForm.value = false
    editingTask.value = null
  }

  async function handleDeleteTaskFromForm(taskId: number) {
    await tasksStore.deleteTask(taskId)
    closeTaskForm()
  }

  async function init() {
    if (!authStore.isAuthenticated) {
      router.push('/login')
      return
    }
    await authStore.loadUsers()
    await projectsStore.fetchProjects()
    projectsStore.initializeProjects()
    const userId = authStore.user?.id
    if (userId) {
      const current = projectsStore.currentProject
      if (current && !projectsStore.canUserSeeProject(current, userId)) {
        const visible = projectsStore.getVisibleProjects(userId)
        if (visible[0]) projectsStore.setCurrentProject(visible[0].id)
      }
    }
    await sprintsStore.fetchSprints(projectsStore.currentProjectId)
    sprintsStore.initializeSprints(projectsStore.currentProjectId)
    await tasksStore.fetchTasks({
      projectId: projectsStore.currentProjectId,
      sprintId: sprintsStore.currentSprintId ?? undefined
    })
  }

  onMounted(init)

  return {
    router,
    authStore,
    tasksStore,
    projectsStore,
    sprintsStore,
    showTaskForm,
    editingTask,
    showAddMemberModal,
    viewMode,
    draggedTask,
    dragOverColumn,
    sortBy,
    sortDir,
    filters,
    todoTasks,
    inProgressTasks,
    completedTasks,
    currentProject,
    currentSprint,
    taskSizeUnit,
    visibleColumnConfigs,
    filteredTodoTasks,
    filteredInProgressTasks,
    filteredCompletedTasks,
    allFilteredTasks,
    sortedAllTasks,
    projectTeamMembers,
    statusLabel,
    getAssigneeName,
    getAssigneeInitials,
    formatDueDate,
    getUserTaskCount,
    isProjectMember,
    toggleProjectMember,
    toggleSort,
    toggleSortDirection,
    clearSort,
    clearFilters,
    handleLogout,
    handleDeleteTask,
    handleStatusChange,
    handleInlineUpdate,
    handleDragStart,
    handleDragEnd,
    handleDragOver,
    handleDragLeave,
    handleDrop,
    handleEditTask,
    handleTaskSubmit,
    handleDeleteTaskFromForm,
    closeTaskForm,
    getInitials,
    getUserColor
  }
}
