<template>
  <div class="dashboard">
    <header class="header">
      <div class="header-content">
        <div class="header-left">
          <div class="brand">
            <div class="brand-icon">
              <CheckSquare class="brand-icon-svg" />
            </div>
            <span class="brand-name">TaskAgent</span>
          </div>
          <div class="header-divider"></div>
          <nav class="header-nav">
            <router-link to="/dashboard" class="nav-link" active-class="active" exact-active-class="active">
              <LayoutDashboard class="nav-icon" />
              Dashboard
            </router-link>
            <router-link to="/my-tasks" class="nav-link" active-class="active" exact-active-class="active">
              <UserIcon class="nav-icon" />
              My Tasks
            </router-link>
            <router-link to="/project-settings" class="nav-link" active-class="active" exact-active-class="active">
              <Settings class="nav-icon" />
              Project settings
            </router-link>
          </nav>
          <div class="header-divider"></div>
          <ProjectSelector />
          <SprintSelector />
        </div>
        <div class="header-right">
          <ThemeToggle class="header-theme-toggle" />
          <div class="user-pill">
            <span class="user-avatar" :style="authStore.user ? { backgroundColor: getUserColor(authStore.user.id), color: '#fff' } : {}">{{ authStore.user ? getInitials(authStore.user) : '' }}</span>
            <span class="user-name">{{ authStore.user?.name }}</span>
          </div>
          <button @click="handleLogout" class="header-btn" title="Logout">
            <LogOut class="btn-icon" />
          </button>
        </div>
      </div>
    </header>

    <main class="main-content">
      <div class="content-header animate-fade-in">
        <div class="view-toggle">
          <button 
            @click="viewMode = 'list'" 
            :class="['view-btn', { active: viewMode === 'list' }]"
            title="List view"
          >
            <List class="view-icon" />
            <span class="view-label">List</span>
          </button>
          <button 
            @click="viewMode = 'columns'" 
            :class="['view-btn', { active: viewMode === 'columns' }]"
            title="Board view"
          >
            <Columns3 class="view-icon" />
            <span class="view-label">Board</span>
          </button>
          <button 
            @click="viewMode = 'analytics'" 
            :class="['view-btn', { active: viewMode === 'analytics' }]"
            title="Analytics"
          >
            <BarChart2 class="view-icon" />
            <span class="view-label">Analytics</span>
          </button>
        </div>
        <button @click="showTaskForm = true" class="btn btn-primary">
          <Plus class="btn-icon" />
          <span>New Task</span>
        </button>
      </div>

      <div class="stats-grid animate-fade-in">
        <div
          v-for="col in visibleColumnConfigs"
          :key="col.status"
          class="stat-card card"
        >
          <div class="stat-content">
            <div :class="['icon-container', col.status === 'todo' ? 'icon-blue' : col.status === 'in-progress' ? 'icon-amber' : 'icon-green']">
              <component :is="col.icon" class="stat-icon" />
            </div>
            <div class="stat-info">
              <p class="stat-value">{{ col.tasks.length }}</p>
              <p class="stat-label">{{ col.label }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Inline filters for list/board view -->
      <div v-if="viewMode !== 'analytics'" class="inline-filters">
        <div class="filter-row">
          <div class="filter-row-item">
            <label class="filter-row-label">Assignee</label>
            <select v-model="filters.assigneeId" class="select select-sm">
              <option value="">All</option>
              <option
                v-for="user in authStore.users"
                :key="user.id"
                :value="user.id"
              >
                {{ user.name }}
              </option>
            </select>
          </div>
          <div class="filter-row-item">
            <label class="filter-row-label">Priority</label>
            <select v-model="filters.priority" class="select select-sm">
              <option value="">All</option>
              <option value="high">High</option>
              <option value="medium">Medium</option>
              <option value="low">Low</option>
            </select>
          </div>
          <button 
            v-if="filters.assigneeId || filters.priority" 
            @click="clearFilters" 
            class="btn btn-ghost btn-sm"
          >
            Clear Filters
          </button>

          <!-- Sorting controls (list view only) -->
          <div v-if="viewMode === 'list'" class="filter-divider"></div>
          <div v-if="viewMode === 'list'" class="filter-row-item">
            <label class="filter-row-label">Sort by</label>
            <select v-model="sortBy" class="select select-sm">
              <option value="">None</option>
              <option value="title">Title</option>
              <option value="status">Status</option>
              <option value="priority">Priority</option>
              <option value="assigneeId">Assignee</option>
              <option value="dueDate">Due Date</option>
              <option value="size">Size</option>
            </select>
          </div>
          <button 
            v-if="viewMode === 'list' && sortBy" 
            @click="toggleSortDirection"
            class="btn btn-ghost btn-sm sort-dir-btn"
            :title="sortDir === 'asc' ? 'Ascending' : 'Descending'"
          >
            <ArrowUp v-if="sortDir === 'asc'" class="sort-dir-icon" />
            <ArrowDown v-else class="sort-dir-icon" />
          </button>
          <button 
            v-if="viewMode === 'list' && sortBy" 
            @click="clearSort" 
            class="btn btn-ghost btn-sm"
          >
            Clear Sort
          </button>
        </div>
      </div>

      <div class="content-grid no-sidebar">
        <AnalyticsView
          v-if="viewMode === 'analytics'"
          :todo-tasks="filteredTodoTasks"
          :in-progress-tasks="filteredInProgressTasks"
          :completed-tasks="filteredCompletedTasks"
          :all-filtered-tasks="[
            ...filteredTodoTasks,
            ...filteredInProgressTasks,
            ...filteredCompletedTasks
          ]"
          :all-project-tasks="tasksStore.getTasksByProject(projectsStore.currentProjectId)"
          :sprints="sprintsStore.getSprintsByProject(projectsStore.currentProjectId)"
          :users="authStore.users"
          :task-size-unit="taskSizeUnit"
          :sprint-progress="currentSprint ? sprintsStore.getSprintProgress(currentSprint.id) : null"
          @edit-task="handleEditTask"
        />

        <!-- List view as table -->
        <div v-else-if="viewMode === 'list'" class="task-table-container card">
          <table class="task-table">
            <thead>
              <tr>
                <th @click="toggleSort('title')" class="sortable">
                  Title
                  <ChevronUp v-if="sortBy === 'title' && sortDir === 'asc'" class="sort-icon" />
                  <ChevronDown v-else-if="sortBy === 'title' && sortDir === 'desc'" class="sort-icon" />
                </th>
                <th @click="toggleSort('status')" class="sortable">
                  Status
                  <ChevronUp v-if="sortBy === 'status' && sortDir === 'asc'" class="sort-icon" />
                  <ChevronDown v-else-if="sortBy === 'status' && sortDir === 'desc'" class="sort-icon" />
                </th>
                <th @click="toggleSort('priority')" class="sortable">
                  Priority
                  <ChevronUp v-if="sortBy === 'priority' && sortDir === 'asc'" class="sort-icon" />
                  <ChevronDown v-else-if="sortBy === 'priority' && sortDir === 'desc'" class="sort-icon" />
                </th>
                <th @click="toggleSort('assigneeId')" class="sortable">
                  Assignee
                  <ChevronUp v-if="sortBy === 'assigneeId' && sortDir === 'asc'" class="sort-icon" />
                  <ChevronDown v-else-if="sortBy === 'assigneeId' && sortDir === 'desc'" class="sort-icon" />
                </th>
                <th @click="toggleSort('dueDate')" class="sortable">
                  Due Date
                  <ChevronUp v-if="sortBy === 'dueDate' && sortDir === 'asc'" class="sort-icon" />
                  <ChevronDown v-else-if="sortBy === 'dueDate' && sortDir === 'desc'" class="sort-icon" />
                </th>
                <th @click="toggleSort('size')" class="sortable th-size">
                  Size
                  <ChevronUp v-if="sortBy === 'size' && sortDir === 'asc'" class="sort-icon" />
                  <ChevronDown v-else-if="sortBy === 'size' && sortDir === 'desc'" class="sort-icon" />
                </th>
                <th class="th-actions"></th>
              </tr>
            </thead>
            <tbody>
              <tr 
                v-for="task in sortedAllTasks" 
                :key="task.id"
                @click="handleEditTask(task)"
                class="task-row"
              >
                <td class="td-title">{{ task.title }}</td>
                <td>
                  <span :class="['status-badge', `status-${task.status}`]">
                    {{ statusLabel(task.status) }}
                  </span>
                </td>
                <td>
                  <span :class="['priority-badge', `priority-${task.priority}`]">
                    {{ task.priority }}
                  </span>
                </td>
                <td>
                  <div class="assignee-cell">
                    <span 
                      class="assignee-avatar" 
                      :style="{ backgroundColor: getUserColor(task.assigneeId) }"
                    >{{ getAssigneeInitials(task.assigneeId) }}</span>
                    <span class="assignee-name">{{ getAssigneeName(task.assigneeId) }}</span>
                  </div>
                </td>
                <td class="td-date">{{ formatDueDate(task.dueDate) }}</td>
                <td class="td-size">{{ task.size ? `${task.size}${taskSizeUnit === 'hours' ? 'h' : 'd'}` : '-' }}</td>
                <td class="td-actions" @click.stop>
                  <button @click="handleDeleteTask(task.id)" class="btn-icon-only" title="Delete">
                    <Trash2 class="action-icon" />
                  </button>
                </td>
              </tr>
              <tr v-if="sortedAllTasks.length === 0">
                <td colspan="7" class="empty-table">No tasks found</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Column/Board view -->
        <div v-else class="task-columns view-columns">
          <section
            v-for="col in visibleColumnConfigs"
            :key="col.status"
            class="task-section"
            @dragover.prevent="handleDragOver($event, col.status)"
            @dragleave="handleDragLeave"
            @drop="handleDrop($event, col.status)"
            :class="{ 'drag-over': dragOverColumn === col.status }"
          >
            <div class="task-section-header">
              <h2 class="task-section-title">
                <span :class="['status-dot', col.dotClass]"></span>
                {{ col.label }}
              </h2>
              <span class="task-count">{{ col.tasks.length }}</span>
            </div>
            <div v-if="col.tasks.length === 0" class="empty-state">
              <component :is="col.icon" class="empty-icon" />
              <p>Drop tasks here</p>
            </div>
            <div v-else class="task-list">
              <TaskCard
                v-for="task in col.tasks"
                :key="task.id"
                :task="task"
                :draggable="true"
                :task-size-unit="taskSizeUnit"
                @dragstart="handleDragStart($event, task)"
                @dragend="handleDragEnd"
                @delete="handleDeleteTask"
                @status-change="handleStatusChange"
                @edit="handleEditTask"
                @update="handleInlineUpdate"
              />
            </div>
          </section>
        </div>
      </div>

      <!-- Team at bottom (dashboard/analytics view) -->
      <section v-if="viewMode === 'analytics' && currentProject" class="dashboard-team-section card">
        <div class="dashboard-team-header">
          <h3 class="dashboard-team-title">
            <Users class="dashboard-team-icon" />
            Team
          </h3>
          <button
            v-if="currentProject.ownerId === (authStore.user?.id ?? 0)"
            @click="showAddMemberModal = true"
            class="btn btn-secondary btn-sm"
          >
            <Plus class="btn-icon-sm" />
            Add member
          </button>
        </div>
        <div class="dashboard-team-list">
          <div
            v-for="user in projectTeamMembers"
            :key="user.id"
            class="dashboard-team-member"
          >
            <span class="team-member-avatar" :style="{ backgroundColor: getUserColor(user.id) }">{{ getInitials(user) }}</span>
            <span class="team-member-name">{{ user.name }}</span>
            <span class="team-member-badge badge badge-gray">{{ getUserTaskCount(user.id) }}</span>
          </div>
        </div>
      </section>

      <!-- Comments section at bottom (only in dashboard/analytics view) -->
      <section v-if="viewMode === 'analytics' && currentProject && authStore.user" class="dashboard-comments-section card">
        <CommentList
          :project-id="currentProject.id"
          :author-id="authStore.user.id"
        />
      </section>
    </main>

    <!-- Add member modal -->
    <Teleport to="body">
      <div v-if="showAddMemberModal" class="modal-overlay" @click.self="showAddMemberModal = false">
        <div class="add-member-modal card">
          <h3 class="modal-title">Add members to project</h3>
          <p class="modal-hint">Select who can see this board. Uncheck all = visible to everyone.</p>
          <div class="add-member-options">
            <label
              v-for="user in authStore.users"
              :key="user.id"
              class="add-member-option"
            >
              <input
                type="checkbox"
                :checked="isProjectMember(user.id)"
                @change="toggleProjectMember(user.id)"
              />
              <span class="team-member-avatar" :style="{ backgroundColor: getUserColor(user.id), color: '#fff' }">{{ getInitials(user) }}</span>
              <span class="add-member-name">{{ user.name }}</span>
            </label>
          </div>
          <button @click="showAddMemberModal = false" class="btn btn-primary">Done</button>
        </div>
      </div>
    </Teleport>

    <TaskForm
      v-if="showTaskForm"
      :task="editingTask"
      @close="closeTaskForm"
      @submit="handleTaskSubmit"
      @delete="handleDeleteTaskFromForm"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTasksStore } from '@/stores/tasks'
import { useProjectsStore } from '@/stores/projects'
import { useSprintsStore } from '@/stores/sprints'
import TaskCard from '@/features/dashboard/components/TaskCard.vue'
import TaskForm from '@/features/dashboard/components/TaskForm.vue'
import ThemeToggle from '@/components/ui/ThemeToggle.vue'
import ProjectSelector from '@/components/layout/ProjectSelector.vue'
import SprintSelector from '@/components/layout/SprintSelector.vue'
import CommentList from '@/features/dashboard/components/CommentList.vue'
import { Plus, ListTodo, Clock, CheckCircle, Filter, Users, LogOut, CheckSquare, List, Columns3, BarChart2, ChevronUp, ChevronDown, Trash2, ArrowUp, ArrowDown, LayoutDashboard, User as UserIcon, Settings } from 'lucide-vue-next'
import AnalyticsView from '@/features/analytics/components/AnalyticsView.vue'
import { getInitials, getUserColor } from '@/utils/initials'

const router = useRouter()
const authStore = useAuthStore()
const tasksStore = useTasksStore()
const projectsStore = useProjectsStore()
const sprintsStore = useSprintsStore()

const showTaskForm = ref(false)
const editingTask = ref(null)
const showAddMemberModal = ref(false)
const viewMode = ref<'list' | 'columns' | 'analytics'>(
  (localStorage.getItem('taskViewMode') as 'list' | 'columns' | 'analytics') || 'list'
)

// Drag and drop state
const draggedTask = ref<any>(null)
const dragOverColumn = ref<string | null>(null)

// Sorting state for list view
const sortBy = ref<'' | 'title' | 'status' | 'priority' | 'assigneeId' | 'dueDate' | 'size'>('status')
const sortDir = ref<'asc' | 'desc'>('asc')

// Persist view mode preference
watch(viewMode, (newMode) => {
  localStorage.setItem('taskViewMode', newMode)
})

// When project switches, show dashboard (analytics) view first
watch(() => projectsStore.switchToDashboard, (val) => {
  if (val) {
    viewMode.value = 'analytics'
    projectsStore.setSwitchToDashboard(false)
  }
}, { flush: 'sync' })

const filters = ref({
  assigneeId: '',
  priority: ''
})

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
  return statuses.map(status => ({
    status,
    ...columnConfig[status],
    tasks: taskLists[status]
  }))
})

const filteredTodoTasks = computed(() => {
  return filterTasks(todoTasks.value)
})

const filteredInProgressTasks = computed(() => {
  return filterTasks(inProgressTasks.value)
})

const filteredCompletedTasks = computed(() => {
  return filterTasks(completedTasks.value)
})

// All filtered tasks combined for list view
const allFilteredTasks = computed(() => [
  ...filteredTodoTasks.value,
  ...filteredInProgressTasks.value,
  ...filteredCompletedTasks.value
])

// Sorted tasks for list view
const sortedAllTasks = computed(() => {
  const tasks = [...allFilteredTasks.value]
  
  // If no sort selected, return unsorted
  if (!sortBy.value) return tasks
  
  const statusOrder = { 'todo': 0, 'in-progress': 1, 'completed': 2 }
  const priorityOrder = { 'high': 0, 'medium': 1, 'low': 2 }
  
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

function statusLabel(status: string): string {
  switch (status) {
    case 'todo': return 'To Do'
    case 'in-progress': return 'In Progress'
    case 'completed': return 'Done'
    default: return status
  }
}

function getAssigneeName(assigneeId: number): string {
  const user = authStore.users.find(u => u.id === assigneeId)
  return user?.name ?? 'Unassigned'
}

function getAssigneeInitials(assigneeId: number): string {
  const user = authStore.users.find(u => u.id === assigneeId)
  return user ? getInitials(user) : '?'
}

function formatDueDate(dueDate?: Date | string): string {
  if (!dueDate) return '-'
  const date = typeof dueDate === 'string' ? new Date(dueDate) : dueDate
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

const filterTasks = (tasks: any[]) => {
  return tasks.filter(task => {
    if (filters.value.assigneeId && task.assigneeId !== Number(filters.value.assigneeId)) {
      return false
    }
    if (filters.value.priority && task.priority !== filters.value.priority) {
      return false
    }
    return true
  })
}

const getUserTaskCount = (userId: number) => {
  return tasksStore.tasks.filter((task: { assigneeId: number }) => task.assigneeId === userId).length
}

const projectTeamMembers = computed(() => {
  const project = currentProject.value
  if (!project) return authStore.users
  const ids = project.visibleToUserIds
  if (!ids || ids.length === 0) return authStore.users
  return authStore.users.filter(u => ids.includes(u.id))
})

function isProjectMember(userId: number): boolean {
  const project = currentProject.value
  if (!project) return true
  const ids = project.visibleToUserIds
  if (!ids || ids.length === 0) return true
  return ids.includes(userId)
}

async function toggleProjectMember(userId: number): Promise<void> {
  const project = currentProject.value
  if (!project || project.ownerId !== (authStore.user?.id ?? 0)) return
  const ids = project.visibleToUserIds ?? authStore.users.map(u => u.id)
  const next = ids.includes(userId) ? ids.filter(id => id !== userId) : [...ids, userId]
  await projectsStore.updateProject(project.id, {
    visibleToUserIds: next.length === 0 ? undefined : next
  })
}

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

const handleDeleteTask = async (taskId: number) => {
  if (confirm('Are you sure you want to delete this task?')) {
    await tasksStore.deleteTask(taskId)
  }
}

const handleStatusChange = async (taskId: number, newStatus: string) => {
  await tasksStore.updateTask(taskId, { status: newStatus as 'todo' | 'in-progress' | 'completed' })
}

// Handle inline updates from TaskCard
const handleInlineUpdate = async (taskId: number, updates: Record<string, unknown>) => {
  await tasksStore.updateTask(taskId, updates)
}

// Drag and drop handlers
const handleDragStart = (event: DragEvent, task: any) => {
  draggedTask.value = task
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('text/plain', task.id.toString())
  }
  // Add a slight delay to allow the drag image to be created
  setTimeout(() => {
    const target = event.target as HTMLElement
    target.classList.add('dragging')
  }, 0)
}

const handleDragEnd = (event: DragEvent) => {
  const target = event.target as HTMLElement
  target.classList.remove('dragging')
  draggedTask.value = null
  dragOverColumn.value = null
}

const handleDragOver = (event: DragEvent, status: string) => {
  if (viewMode.value !== 'columns') return
  event.preventDefault()
  dragOverColumn.value = status
}

const handleDragLeave = (event: DragEvent) => {
  // Only clear if we're leaving the section entirely
  const relatedTarget = event.relatedTarget as HTMLElement
  const currentTarget = event.currentTarget as HTMLElement
  if (!currentTarget.contains(relatedTarget)) {
    dragOverColumn.value = null
  }
}

const handleDrop = async (event: DragEvent, newStatus: string) => {
  if (viewMode.value !== 'columns') return
  event.preventDefault()
  dragOverColumn.value = null
  if (draggedTask.value && draggedTask.value.status !== newStatus) {
    await tasksStore.updateTask(draggedTask.value.id, {
      status: newStatus as 'todo' | 'in-progress' | 'completed'
    })
  }
  draggedTask.value = null
}

const handleEditTask = (task: any) => {
  editingTask.value = task
  showTaskForm.value = true
}

const handleTaskSubmit = async (taskData: Record<string, unknown>) => {
  if (editingTask.value) {
    await tasksStore.updateTask((editingTask.value as { id: number }).id, taskData as Partial<import('@/types').Task>)
  } else {
    await tasksStore.addTask(taskData as Omit<import('@/types').Task, 'id' | 'createdAt'>)
  }
  closeTaskForm()
}

const closeTaskForm = () => {
  showTaskForm.value = false
  editingTask.value = null
}

const handleDeleteTaskFromForm = async (taskId: number) => {
  await tasksStore.deleteTask(taskId)
  closeTaskForm()
}

const clearFilters = () => {
  filters.value = {
    assigneeId: '',
    priority: ''
  }
}

onMounted(async () => {
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
      const first = visible[0]
      if (first) {
        projectsStore.setCurrentProject(first.id)
      }
    }
  }
  await sprintsStore.fetchSprints(projectsStore.currentProjectId)
  sprintsStore.initializeSprints(projectsStore.currentProjectId)
  await tasksStore.fetchTasks({
    projectId: projectsStore.currentProjectId,
    sprintId: sprintsStore.currentSprintId ?? undefined
  })
})
</script>

<style scoped>
.dashboard {
  min-height: 100vh;
  background-color: var(--bg-secondary);
}

.header {
  position: sticky;
  top: 0;
  z-index: 40;
  background-color: var(--header-bg);
  border-bottom: 1px solid var(--header-border);
}

.header-content {
  max-width: 84rem;
  margin: 0 auto;
  padding: 0 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 4rem;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.625rem;
}

.brand-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background-color: rgba(255, 255, 255, 0.15);
  border-radius: 0.5rem;
}

.brand-icon-svg {
  width: 1.125rem;
  height: 1.125rem;
  color: var(--header-text);
}

.brand-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--header-text);
  letter-spacing: -0.02em;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.header-divider {
  width: 1px;
  height: 1.5rem;
  background-color: rgba(255, 255, 255, 0.2);
  margin: 0 0.25rem;
}

.header-nav {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: rgba(255, 255, 255, 0.7);
  text-decoration: none;
  border-radius: 0.375rem;
  transition: all 0.15s ease;
}

.nav-link:hover {
  color: white;
  background-color: rgba(255, 255, 255, 0.1);
}

.nav-link.active {
  color: white;
  background-color: rgba(255, 255, 255, 0.15);
}

.nav-icon {
  width: 1rem;
  height: 1rem;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.user-pill {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem 0.375rem 0.375rem;
  background-color: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 9999px;
}

.user-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 2rem;
  height: 2rem;
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1;
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: 0.375rem;
}

.user-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--header-text);
}

.header-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  background-color: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 9999px;
  color: var(--header-text);
  cursor: pointer;
  transition: all 0.15s ease;
}

.header-btn:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

:deep(.header-theme-toggle) {
  background-color: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.15);
  color: var(--header-text);
}

:deep(.header-theme-toggle:hover) {
  background-color: rgba(255, 255, 255, 0.2);
  border-color: rgba(255, 255, 255, 0.25);
  color: var(--header-text);
}

.btn-icon {
  width: 1rem;
  height: 1rem;
}

.main-content {
  max-width: 84rem;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

.content-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.view-toggle {
  display: flex;
  align-items: center;
  background-color: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: 0.5rem;
  padding: 0.25rem;
  gap: 0.125rem;
}

.view-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.375rem;
  padding: 0.5rem 0.75rem;
  border: none;
  background: transparent;
  border-radius: 0.375rem;
  cursor: pointer;
  color: var(--text-muted);
  font-size: 0.875rem;
  font-weight: 500;
  transition: all 0.15s ease;
}

.view-btn:hover {
  color: var(--text-secondary);
}

.view-btn.active {
  background-color: var(--card-bg);
  color: var(--text-primary);
  box-shadow: var(--shadow-xs);
}

.view-icon {
  width: 1.125rem;
  height: 1.125rem;
  flex-shrink: 0;
}

.view-label {
  white-space: nowrap;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(1, 1fr);
  gap: 1rem;
  margin-bottom: 2rem;
}

@media (min-width: 640px) {
  .stats-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

.stat-card {
  padding: 1.25rem;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.stat-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: -0.02em;
  line-height: 1;
}

.stat-label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--text-tertiary);
  margin-top: 0.25rem;
}

.inline-filters {
  margin-bottom: 1.5rem;
}

.filter-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.filter-row-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.filter-row-label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--text-secondary);
  white-space: nowrap;
}

.select-sm {
  padding: 0.375rem 2rem 0.375rem 0.625rem;
  font-size: 0.8125rem;
  min-width: 120px;
}

.btn-ghost {
  background: transparent;
  border: none;
  color: var(--text-tertiary);
  font-size: 0.8125rem;
  padding: 0.375rem 0.625rem;
  cursor: pointer;
  transition: color 0.15s ease;
}

.btn-ghost:hover {
  color: var(--text-primary);
}

.filter-divider {
  width: 1px;
  height: 1.5rem;
  background-color: var(--border-primary);
  margin: 0 0.5rem;
}

.sort-dir-btn {
  padding: 0.375rem;
}

.sort-dir-icon {
  width: 0.875rem;
  height: 0.875rem;
}

.content-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
}

.content-grid.no-sidebar {
  grid-template-columns: 1fr;
}

.team-member-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.team-member-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.75rem;
  height: 1.75rem;
  font-size: 0.625rem;
  font-weight: 600;
  line-height: 1;
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: 0.25rem;
}

.team-member-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-primary);
}

.team-member-badge {
  font-size: 0.6875rem;
  min-width: 1.25rem;
  text-align: center;
}

/* List view (default) */
.task-columns {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* Column/Kanban view */
.task-columns.view-columns {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
}

@media (max-width: 1024px) {
  .task-columns.view-columns {
    grid-template-columns: 1fr;
  }
}

.task-columns.view-columns .task-section {
  background-color: var(--bg-tertiary);
  border-radius: 0.75rem;
  padding: 1rem;
  min-height: 300px;
}

.task-columns.view-columns .task-section-header {
  position: sticky;
  top: 0;
  background-color: var(--bg-tertiary);
  padding-bottom: 0.75rem;
  margin-bottom: 0.75rem;
  border-bottom: 1px solid var(--border-primary);
}

.task-columns.view-columns .task-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.task-columns.view-columns .empty-state {
  background-color: transparent;
  border: 1px dashed var(--border-secondary);
}

/* Drag and drop styles */
.task-columns.view-columns .task-section.drag-over {
  background-color: var(--bg-hover);
  outline: 2px dashed var(--color-brown-400);
  outline-offset: -2px;
}

.task-columns.view-columns .task-section.drag-over .empty-state {
  border-color: var(--color-brown-400);
  background-color: rgba(176, 90, 54, 0.05);
}

:deep(.task-card.dragging) {
  opacity: 0.5;
  transform: rotate(2deg);
}

:deep(.task-card[draggable="true"]) {
  cursor: grab;
}

:deep(.task-card[draggable="true"]:active) {
  cursor: grabbing;
}

.task-section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.task-section-title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.status-dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
}

.status-dot-blue {
  background-color: var(--status-blue);
}

.status-dot-amber {
  background-color: var(--status-amber);
}

.status-dot-green {
  background-color: var(--status-green);
}

.task-count {
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--text-muted);
  background-color: var(--bg-tertiary);
  padding: 0.125rem 0.5rem;
  border-radius: 9999px;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  color: var(--text-muted);
  background-color: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px dashed var(--border-primary);
}

.empty-icon {
  width: 1.5rem;
  height: 1.5rem;
  margin-bottom: 0.5rem;
  opacity: 0.5;
}

.empty-state p {
  font-size: 0.875rem;
}

.task-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.dashboard-team-section {
  margin-top: 2rem;
  padding: 1.25rem;
}

.dashboard-team-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.dashboard-team-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.dashboard-team-icon {
  width: 1.125rem;
  height: 1.125rem;
  color: var(--text-tertiary);
}

.btn-icon-sm {
  width: 0.875rem;
  height: 0.875rem;
}

.dashboard-team-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.dashboard-team-member {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  background-color: var(--bg-tertiary);
  border-radius: 0.5rem;
}

.dashboard-team-member .team-member-avatar {
  min-width: 1.75rem;
  height: 1.75rem;
  font-size: 0.625rem;
  color: #fff;
}

.dashboard-team-member .team-member-name {
  font-size: 0.875rem;
  color: var(--text-primary);
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.add-member-modal {
  max-width: 24rem;
  padding: 1.5rem;
}

.add-member-modal .modal-title {
  margin: 0 0 0.5rem;
  font-size: 1rem;
}

.modal-hint {
  font-size: 0.8125rem;
  color: var(--text-muted);
  margin: 0 0 1rem;
}

.add-member-options {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.add-member-option {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 0.5rem;
}

.add-member-option:hover {
  background-color: var(--bg-hover);
}

.add-member-option .team-member-avatar {
  min-width: 2rem;
  height: 2rem;
  font-size: 0.75rem;
  color: #fff;
}

.add-member-name {
  font-size: 0.875rem;
  color: var(--text-primary);
}

.dashboard-comments-section {
  margin-top: 2rem;
  padding: 1.25rem;
}

/* Table styles for list view */
.task-table-container {
  overflow-x: auto;
  padding: 0;
}

.task-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.8125rem;
}

.task-table th {
  text-align: left;
  padding: 0.75rem 1rem;
  font-weight: 600;
  color: var(--text-secondary);
  background-color: var(--bg-tertiary);
  border-bottom: 1px solid var(--border-primary);
  white-space: nowrap;
  user-select: none;
}

.task-table th.sortable {
  cursor: pointer;
  transition: color 0.15s ease;
}

.task-table th.sortable:hover {
  color: var(--text-primary);
}

.sort-icon {
  width: 0.875rem;
  height: 0.875rem;
  vertical-align: middle;
  margin-left: 0.25rem;
  color: var(--color-brown-500);
}

.task-table td {
  padding: 0.625rem 1rem;
  border-bottom: 1px solid var(--border-primary);
  color: var(--text-primary);
}

.task-row {
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.task-row:hover {
  background-color: var(--bg-hover);
}

.td-title {
  font-weight: 500;
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.td-date,
.td-size {
  color: var(--text-tertiary);
  white-space: nowrap;
}

.th-size,
.td-size {
  text-align: right;
}

.th-actions {
  width: 3rem;
}

.td-actions {
  text-align: center;
}

.btn-icon-only {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: 0.375rem;
  color: var(--text-muted);
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-icon-only:hover {
  background-color: var(--bg-tertiary);
  color: var(--color-red-500, #dc2626);
}

.action-icon {
  width: 0.875rem;
  height: 0.875rem;
}

.status-badge {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  border-radius: 9999px;
  font-size: 0.6875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.status-todo {
  background-color: var(--status-blue-bg, rgba(59, 130, 246, 0.1));
  color: var(--status-blue, #3b82f6);
}

.status-in-progress {
  background-color: var(--status-amber-bg, rgba(245, 158, 11, 0.1));
  color: var(--status-amber, #f59e0b);
}

.status-completed {
  background-color: var(--status-green-bg, rgba(34, 197, 94, 0.1));
  color: var(--status-green, #22c55e);
}

.priority-badge {
  display: inline-block;
  padding: 0.125rem 0.375rem;
  border-radius: 0.25rem;
  font-size: 0.6875rem;
  font-weight: 500;
  text-transform: capitalize;
}

.priority-high {
  background-color: var(--badge-red-bg);
  color: var(--badge-red-text);
}

.priority-medium {
  background-color: var(--badge-amber-bg);
  color: var(--badge-amber-text);
}

.priority-low {
  background-color: var(--badge-gray-bg);
  color: var(--badge-gray-text);
}

.assignee-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.assignee-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.5rem;
  height: 1.5rem;
  font-size: 0.5625rem;
  font-weight: 600;
  color: #fff;
  border-radius: 0.25rem;
  flex-shrink: 0;
}

.assignee-name {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.empty-table {
  text-align: center;
  padding: 2rem 1rem;
  color: var(--text-muted);
}
</style>
