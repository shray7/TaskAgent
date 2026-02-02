<template>
  <div class="my-tasks-view">
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
            <router-link to="/dashboard" class="nav-link">
              <LayoutDashboard class="nav-icon" />
              Dashboard
            </router-link>
            <router-link to="/my-tasks" class="nav-link active">
              <User class="nav-icon" />
              My Tasks
            </router-link>
          </nav>
        </div>
        <div class="header-right">
          <ThemeToggle class="header-theme-toggle" />
          <div class="user-pill">
            <span class="user-avatar" :style="authStore.user ? { backgroundColor: getUserColor(authStore.user.id), color: '#fff' } : {}">
              {{ authStore.user ? getInitials(authStore.user) : '' }}
            </span>
            <span class="user-name">{{ authStore.user?.name }}</span>
          </div>
          <button @click="handleLogout" class="header-btn" title="Logout">
            <LogOut class="btn-icon" />
          </button>
        </div>
      </div>
    </header>

    <main class="main-content">
      <div class="page-header animate-fade-in">
        <h1 class="page-title">My Tasks</h1>
        <p class="page-subtitle">Tasks assigned to you across all projects</p>
      </div>

      <!-- Filters -->
      <div class="filters-bar animate-fade-in">
        <div class="filter-group">
          <label class="filter-label">Project</label>
          <select v-model="selectedProjectId" class="select">
            <option :value="null">All Projects</option>
            <option v-for="project in projectsStore.projects" :key="project.id" :value="project.id">
              {{ project.name }}
            </option>
          </select>
        </div>
        <div class="filter-group">
          <label class="filter-label">Status</label>
          <select v-model="selectedStatus" class="select">
            <option value="">All</option>
            <option value="todo">To Do</option>
            <option value="in-progress">In Progress</option>
            <option value="completed">Completed</option>
          </select>
        </div>
        <div class="filter-group">
          <label class="filter-label">Priority</label>
          <select v-model="selectedPriority" class="select">
            <option value="">All</option>
            <option value="high">High</option>
            <option value="medium">Medium</option>
            <option value="low">Low</option>
          </select>
        </div>
        <button v-if="hasActiveFilters" @click="clearFilters" class="btn btn-ghost">
          Clear Filters
        </button>
      </div>

      <!-- Task Stats -->
      <div class="stats-row animate-fade-in">
        <div class="stat-card card">
          <div class="stat-content">
            <div class="icon-container icon-blue">
              <Circle class="stat-icon" />
            </div>
            <div class="stat-info">
              <p class="stat-value">{{ todoTasks.length }}</p>
              <p class="stat-label">To Do</p>
            </div>
          </div>
        </div>
        <div class="stat-card card">
          <div class="stat-content">
            <div class="icon-container icon-amber">
              <Clock class="stat-icon" />
            </div>
            <div class="stat-info">
              <p class="stat-value">{{ inProgressTasks.length }}</p>
              <p class="stat-label">In Progress</p>
            </div>
          </div>
        </div>
        <div class="stat-card card">
          <div class="stat-content">
            <div class="icon-container icon-green">
              <CheckCircle2 class="stat-icon" />
            </div>
            <div class="stat-info">
              <p class="stat-value">{{ completedTasks.length }}</p>
              <p class="stat-label">Completed</p>
            </div>
          </div>
        </div>
        <div class="stat-card card">
          <div class="stat-content">
            <div class="icon-container icon-red">
              <AlertTriangle class="stat-icon" />
            </div>
            <div class="stat-info">
              <p class="stat-value">{{ overdueTasks.length }}</p>
              <p class="stat-label">Overdue</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Task List -->
      <div class="tasks-container animate-fade-in">
        <div v-if="filteredTasks.length === 0" class="empty-state">
          <Inbox class="empty-icon" />
          <p class="empty-title">No tasks found</p>
          <p class="empty-description">
            {{ hasActiveFilters ? 'Try adjusting your filters' : 'You don\'t have any tasks assigned yet' }}
          </p>
        </div>

        <div v-else class="tasks-list">
          <div 
            v-for="task in filteredTasks" 
            :key="task.id" 
            class="task-item card"
            @click="openTask(task)"
          >
            <div class="task-item-header">
              <div class="task-item-left">
                <span :class="['status-dot', `status-${task.status}`]"></span>
                <h3 class="task-item-title">{{ task.title }}</h3>
              </div>
              <span :class="['badge', priorityBadgeClass(task.priority)]">{{ task.priority }}</span>
            </div>
            
            <p v-if="task.description" class="task-item-description">{{ task.description }}</p>
            
            <div class="task-item-meta">
              <div class="meta-tag project-tag" :style="{ borderColor: getProjectColor(task.projectId) }">
                <Folder class="meta-icon" />
                {{ getProjectName(task.projectId) }}
              </div>
              <div v-if="task.dueDate" :class="['meta-tag', isOverdue(task) ? 'overdue' : '']">
                <Calendar class="meta-icon" />
                {{ formatDueDate(task.dueDate) }}
              </div>
              <div v-if="task.size" class="meta-tag">
                <Hash class="meta-icon" />
                {{ task.size }}{{ getTaskSizeUnit(task.projectId) === 'hours' ? 'h' : 'd' }}
              </div>
              <div v-if="task.tags && task.tags.length > 0" class="task-tags">
                <span v-for="tag in task.tags.slice(0, 3)" :key="tag" class="tag">{{ tag }}</span>
                <span v-if="task.tags.length > 3" class="tag tag-more">+{{ task.tags.length - 3 }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- Task Detail Modal -->
    <TaskForm
      v-if="showTaskForm"
      :task="selectedTask"
      :users="authStore.users"
      :sprints="getSprintsForTask(selectedTask)"
      :task-size-unit="selectedTask ? getTaskSizeUnit(selectedTask.projectId) : 'hours'"
      @close="closeTaskForm"
      @save="handleSaveTask"
      @delete="handleDeleteTask"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { 
  CheckSquare, LogOut, User, LayoutDashboard, Circle, Clock, CheckCircle2, 
  AlertTriangle, Calendar, Folder, Hash, Inbox 
} from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { useTasksStore } from '@/stores/tasks'
import { useProjectsStore } from '@/stores/projects'
import { useSprintsStore } from '@/stores/sprints'
import { getInitials, getUserColor } from '@/utils/initials'
import ThemeToggle from '@/components/ui/ThemeToggle.vue'
import TaskForm from '@/features/dashboard/components/TaskForm.vue'
import type { Task, TaskStatus, TaskPriority } from '@/types'

const router = useRouter()
const authStore = useAuthStore()
const tasksStore = useTasksStore()
const projectsStore = useProjectsStore()
const sprintsStore = useSprintsStore()

// Filters
const selectedProjectId = ref<number | null>(null)
const selectedStatus = ref<TaskStatus | ''>('')
const selectedPriority = ref<TaskPriority | ''>('')

// Task form
const showTaskForm = ref(false)
const selectedTask = ref<Task | null>(null)

const hasActiveFilters = computed(() => 
  selectedProjectId.value !== null || selectedStatus.value !== '' || selectedPriority.value !== ''
)

// Get tasks assigned to current user
const myTasks = computed(() => {
  if (!authStore.user) return []
  return tasksStore.tasks.filter(task => task.assigneeId === authStore.user!.id)
})

// Apply filters
const filteredTasks = computed(() => {
  let tasks = myTasks.value
  
  if (selectedProjectId.value !== null) {
    tasks = tasks.filter(t => t.projectId === selectedProjectId.value)
  }
  if (selectedStatus.value) {
    tasks = tasks.filter(t => t.status === selectedStatus.value)
  }
  if (selectedPriority.value) {
    tasks = tasks.filter(t => t.priority === selectedPriority.value)
  }
  
  // Sort by priority (high first), then by due date
  return tasks.sort((a, b) => {
    const priorityOrder = { high: 0, medium: 1, low: 2 }
    const pDiff = priorityOrder[a.priority] - priorityOrder[b.priority]
    if (pDiff !== 0) return pDiff
    
    if (a.dueDate && b.dueDate) {
      return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    }
    if (a.dueDate) return -1
    if (b.dueDate) return 1
    return 0
  })
})

const todoTasks = computed(() => myTasks.value.filter(t => t.status === 'todo'))
const inProgressTasks = computed(() => myTasks.value.filter(t => t.status === 'in-progress'))
const completedTasks = computed(() => myTasks.value.filter(t => t.status === 'completed'))
const overdueTasks = computed(() => {
  const now = new Date()
  now.setHours(0, 0, 0, 0)
  return myTasks.value.filter(t => {
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
  return sprintsStore.sprints.filter(s => s.projectId === task.projectId)
}

function isOverdue(task: Task): boolean {
  if (!task.dueDate || task.status === 'completed') return false
  const now = new Date()
  now.setHours(0, 0, 0, 0)
  const due = new Date(task.dueDate)
  due.setHours(0, 0, 0, 0)
  return due < now
}

function formatDueDate(date: Date | string): string {
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
  if (selectedTask.value) {
    await tasksStore.updateTask(selectedTask.value.id, taskData)
  }
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
</script>

<style scoped>
.my-tasks-view {
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

.header-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
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

.page-header {
  margin-bottom: 1.5rem;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 0.25rem;
}

.page-subtitle {
  font-size: 0.875rem;
  color: var(--text-muted);
  margin: 0;
}

.filters-bar {
  display: flex;
  align-items: flex-end;
  gap: 1rem;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.filter-label {
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--text-tertiary);
}

.select {
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
  background-color: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  color: var(--text-primary);
  min-width: 150px;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  font-weight: 500;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-ghost {
  background: transparent;
  color: var(--text-secondary);
}

.btn-ghost:hover {
  background-color: var(--bg-hover);
  color: var(--text-primary);
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  margin-bottom: 1.5rem;
}

@media (max-width: 768px) {
  .stats-row {
    grid-template-columns: repeat(2, 1fr);
  }
}

.stat-card {
  padding: 1rem;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.icon-container {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.5rem;
}

.icon-blue { background-color: rgba(59, 130, 246, 0.15); color: #3b82f6; }
.icon-amber { background-color: rgba(245, 158, 11, 0.15); color: #f59e0b; }
.icon-green { background-color: rgba(34, 197, 94, 0.15); color: #22c55e; }
.icon-red { background-color: rgba(239, 68, 68, 0.15); color: #ef4444; }

.stat-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  line-height: 1.2;
}

.stat-label {
  font-size: 0.75rem;
  color: var(--text-tertiary);
  margin: 0;
}

.tasks-container {
  background-color: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
  overflow: hidden;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
}

.empty-icon {
  width: 3rem;
  height: 3rem;
  color: var(--text-tertiary);
  margin-bottom: 1rem;
}

.empty-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 0.25rem;
}

.empty-description {
  font-size: 0.875rem;
  color: var(--text-muted);
  margin: 0;
}

.tasks-list {
  display: flex;
  flex-direction: column;
}

.task-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--border-color);
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.task-item:last-child {
  border-bottom: none;
}

.task-item:hover {
  background-color: var(--bg-hover);
}

.task-item-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.task-item-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  min-width: 0;
}

.status-dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  flex-shrink: 0;
}

.status-todo { background-color: var(--status-blue); }
.status-in-progress { background-color: var(--status-amber); }
.status-completed { background-color: var(--status-green); }

.task-item-title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.badge {
  font-size: 0.6875rem;
  font-weight: 600;
  padding: 0.125rem 0.5rem;
  border-radius: 9999px;
  text-transform: capitalize;
  flex-shrink: 0;
}

.badge-red {
  background-color: var(--badge-red-bg);
  color: var(--badge-red-text);
}

.badge-amber {
  background-color: var(--badge-amber-bg, rgba(217, 119, 6, 0.2));
  color: var(--badge-amber-text, #b45309);
}

.badge-gray {
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
}

.task-item-description {
  font-size: 0.8125rem;
  color: var(--text-muted);
  margin: 0;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.task-item-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.meta-tag {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.75rem;
  color: var(--text-secondary);
}

.meta-icon {
  width: 0.875rem;
  height: 0.875rem;
}

.project-tag {
  border-left: 2px solid;
  padding-left: 0.5rem;
}

.meta-tag.overdue {
  color: var(--badge-red-text);
}

.task-tags {
  display: flex;
  gap: 0.375rem;
}

.tag {
  font-size: 0.6875rem;
  padding: 0.125rem 0.5rem;
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: 0.25rem;
}

.tag-more {
  background-color: transparent;
  color: var(--text-muted);
}

.card {
  background-color: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
}

.animate-fade-in {
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
