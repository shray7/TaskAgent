<template>
  <div class="dashboard dashboard-tablet">
    <header class="header">
      <div class="header-content">
        <div class="header-left">
          <HamburgerNav :show-project-sprint="true" />
          <div class="header-divider"></div>
          <div class="view-toggle">
            <button :class="['view-btn', { active: viewMode === 'list' }]" @click="viewMode = 'list'" title="List">
              <List class="view-icon" />
            </button>
            <button :class="['view-btn', { active: viewMode === 'columns' }]" @click="viewMode = 'columns'" title="Board">
              <Columns3 class="view-icon" />
            </button>
            <button :class="['view-btn', { active: viewMode === 'analytics' }]" @click="viewMode = 'analytics'" title="Analytics">
              <BarChart2 class="view-icon" />
            </button>
          </div>
        </div>
        <div class="header-right">
          <button v-if="currentProject" class="btn btn-primary btn-sm" @click="showTaskForm = true">
            <Plus class="btn-icon" />
            New Task
          </button>
          <div class="user-menu" ref="userMenuRef">
            <button
              type="button"
              class="user-pill user-menu-trigger"
              @click="toggleUserMenu"
              :aria-expanded="userMenuOpen"
              aria-haspopup="menu"
            >
              <span class="user-avatar" :style="authStore.user ? { backgroundColor: getUserColor(authStore.user.id), color: '#fff' } : {}">
                {{ authStore.user ? getInitials(authStore.user) : '' }}
              </span>
              <span class="user-name">{{ authStore.user?.name }}</span>
              <ChevronDown class="user-menu-caret" />
            </button>
            <transition name="menu-fade">
              <div
                v-if="userMenuOpen"
                class="user-menu-dropdown"
                role="menu"
              >
                <button type="button" class="user-menu-item" @click="handleThemeToggle">
                  <Sun v-if="themeStore.theme === 'light'" class="menu-icon" />
                  <Moon v-else class="menu-icon" />
                  <span>Switch to {{ themeStore.theme === 'light' ? 'Dark' : 'Light' }} mode</span>
                </button>
                <button type="button" class="user-menu-item" @click="handleLogoutClick">
                  <LogOut class="menu-icon" />
                  <span>Log out</span>
                </button>
              </div>
            </transition>
          </div>
        </div>
      </div>
    </header>

    <main class="main-content">
      <div v-if="projectsStore.loaded && !currentProject" class="empty-state card animate-fade-in">
        <div class="empty-state-icon">
          <CheckSquare class="empty-icon" />
        </div>
        <h2 class="empty-state-title">Create your first project</h2>
        <p class="empty-state-text">You don't have any projects yet. Create a project to start organizing tasks, sprints, and team collaboration.</p>
        <button @click="projectsStore.openCreateProjectForm = true" class="btn btn-primary">
          <Plus class="btn-icon" />
          Create Project
        </button>
      </div>

      <template v-else>
      <div class="stats-grid stats-grid-tablet">
        <div v-for="col in visibleColumnConfigs" :key="col.status" class="stat-card card">
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

      <div v-if="viewMode !== 'analytics'" class="inline-filters filters-scroll">
        <div class="filter-row">
          <div class="filter-row-item">
            <label class="filter-row-label">Assignee</label>
            <select v-model="filters.assigneeId" class="select select-sm">
              <option value="">All</option>
              <option v-for="user in authStore.users" :key="user.id" :value="user.id">{{ user.name }}</option>
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
          <button v-if="filters.assigneeId || filters.priority" class="btn btn-ghost btn-sm" @click="clearFilters">Clear</button>
          <template v-if="viewMode === 'list'">
            <div class="filter-divider"></div>
            <div class="filter-row-item">
              <label class="filter-row-label">Sort</label>
              <select v-model="sortBy" class="select select-sm">
                <option value="">None</option>
                <option value="title">Title</option>
                <option value="priority">Priority</option>
                <option value="dueDate">Due Date</option>
              </select>
            </div>
          </template>
        </div>
      </div>

      <div class="content-grid no-sidebar">
        <AnalyticsTablet
          v-if="viewMode === 'analytics'"
          :todo-tasks="filteredTodoTasks"
          :in-progress-tasks="filteredInProgressTasks"
          :completed-tasks="filteredCompletedTasks"
          :all-filtered-tasks="allFilteredTasks"
          :all-project-tasks="tasksStore.getTasksByProject(projectsStore.currentProjectId)"
          :sprints="sprintsStore.getSprintsByProject(projectsStore.currentProjectId)"
          :users="authStore.users"
          :task-size-unit="taskSizeUnit"
          :sprint-progress="currentSprint ? sprintsStore.getSprintProgress(currentSprint.id) : null"
          @edit-task="handleEditTask"
        />
        <div v-else-if="viewMode === 'list'" class="task-table-container card">
          <table class="task-table">
            <thead>
              <tr>
                <th>Title</th>
                <th>Status</th>
                <th>Priority</th>
                <th>Due</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="task in sortedAllTasks" :key="task.id" class="task-row" @click="handleEditTask(task)">
                <td class="td-title">{{ task.title }}</td>
                <td><span :class="['status-badge', `status-${task.status}`]">{{ statusLabel(task.status) }}</span></td>
                <td><span :class="['priority-badge', `priority-${task.priority}`]">{{ task.priority }}</span></td>
                <td class="td-date">{{ formatDueDate(task.dueDate) }}</td>
              </tr>
              <tr v-if="sortedAllTasks.length === 0">
                <td colspan="4" class="empty-table">No tasks</td>
              </tr>
            </tbody>
          </table>
        </div>
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
              <h2 class="task-section-title"><span :class="['status-dot', col.dotClass]"></span>{{ col.label }}</h2>
              <span class="task-count">{{ col.tasks.length }}</span>
            </div>
            <div v-if="col.tasks.length === 0" class="empty-state">
              <component :is="col.icon" class="empty-icon" />
              <p>Drop here</p>
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

      <section v-if="viewMode === 'analytics' && currentProject" class="dashboard-team-section card">
        <div class="dashboard-team-header">
          <h3 class="dashboard-team-title"><Users class="dashboard-team-icon" /> Team</h3>
          <button v-if="currentProject.ownerId === (authStore.user?.id ?? 0)" class="btn btn-secondary btn-sm" @click="showAddMemberModal = true">
            <Plus class="btn-icon-sm" /> Add member
          </button>
        </div>
        <div class="dashboard-team-list">
          <div v-for="user in projectTeamMembers" :key="user.id" class="dashboard-team-member">
            <span class="team-member-avatar" :style="{ backgroundColor: getUserColor(user.id) }">{{ getInitials(user) }}</span>
            <span class="team-member-name">{{ user.name }}</span>
            <span class="team-member-badge badge badge-gray">{{ getUserTaskCount(user.id) }}</span>
          </div>
        </div>
      </section>

      <section v-if="viewMode === 'analytics' && currentProject && authStore.user" class="dashboard-comments-section card">
        <CommentList :project-id="currentProject.id" :author-id="authStore.user.id" />
      </section>
      </template>
    </main>

    <Teleport to="body">
      <div v-if="showAddMemberModal" class="modal-overlay" @click.self="showAddMemberModal = false">
        <div class="add-member-modal card">
          <h3 class="modal-title">Add members</h3>
          <div class="add-member-options">
            <label v-for="user in authStore.users" :key="user.id" class="add-member-option">
              <input type="checkbox" :checked="isProjectMember(user.id)" @change="toggleProjectMember(user.id)" />
              <span class="team-member-avatar" :style="{ backgroundColor: getUserColor(user.id), color: '#fff' }">{{ getInitials(user) }}</span>
              <span class="add-member-name">{{ user.name }}</span>
            </label>
          </div>
          <button class="btn btn-primary" @click="showAddMemberModal = false">Done</button>
        </div>
      </div>
    </Teleport>

    <TaskForm v-if="showTaskForm" :task="editingTask" @close="closeTaskForm" @submit="handleTaskSubmit" @delete="handleDeleteTaskFromForm" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { Plus, List, Columns3, BarChart2, LogOut, Users, CheckSquare, ChevronDown, Sun, Moon } from 'lucide-vue-next'
import { useDashboard } from '../../composables/useDashboard'
import HamburgerNav from '@/components/layout/HamburgerNav.vue'
import TaskCard from '../../components/TaskCard.vue'
import TaskForm from '../../components/TaskForm.vue'
import CommentList from '../../components/CommentList.vue'
import AnalyticsTablet from '@/features/analytics/components/AnalyticsTablet.vue'
import { useThemeStore } from '@/stores/theme'

const d = useDashboard()
const {
  authStore, tasksStore, projectsStore, sprintsStore,
  showTaskForm, editingTask, showAddMemberModal, viewMode,
  dragOverColumn, filters, sortBy, visibleColumnConfigs,
  filteredTodoTasks, filteredInProgressTasks, filteredCompletedTasks, allFilteredTasks,
  sortedAllTasks, currentProject, currentSprint, taskSizeUnit,
  projectTeamMembers, statusLabel, formatDueDate, getUserTaskCount, getInitials, getUserColor,
  isProjectMember, toggleProjectMember, clearFilters,
  handleLogout, handleDeleteTask, handleStatusChange, handleInlineUpdate,
  handleDragStart, handleDragEnd, handleDragOver, handleDragLeave, handleDrop,
  handleEditTask, handleTaskSubmit, handleDeleteTaskFromForm, closeTaskForm
} = d

const themeStore = useThemeStore()

const userMenuOpen = ref(false)
const userMenuRef = ref<HTMLElement | null>(null)

const toggleUserMenu = () => {
  userMenuOpen.value = !userMenuOpen.value
}

const closeUserMenu = () => {
  userMenuOpen.value = false
}

const handleThemeToggle = () => {
  themeStore.toggleTheme()
  closeUserMenu()
}

const handleLogoutClick = () => {
  closeUserMenu()
  handleLogout()
}

const handleDocumentClick = (event: MouseEvent) => {
  const target = event.target as Node | null
  if (!userMenuRef.value || !target) return
  if (!userMenuRef.value.contains(target)) {
    userMenuOpen.value = false
  }
}

const handleEscapeKey = (event: KeyboardEvent) => {
  if (event.key === 'Escape') {
    closeUserMenu()
  }
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
  document.addEventListener('keydown', handleEscapeKey)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick)
  document.removeEventListener('keydown', handleEscapeKey)
})
</script>

<style scoped>
@import '../dashboard-shared.css';

/* Tablet header: more padding and gap so items aren't crunched */
.dashboard-tablet .header-content {
  padding-left: 1.75rem;
  padding-right: 1.75rem;
  min-height: 4rem;
  height: auto;
  gap: 1rem;
}

.dashboard-tablet .header-left {
  gap: 1rem;
  flex-wrap: wrap;
  min-width: 0;
}

.dashboard-tablet .header-right {
  gap: 1rem;
  flex-wrap: wrap;
  min-width: 0;
}

.dashboard-tablet .user-menu {
  position: relative;
}

.dashboard-tablet .user-pill {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem 0.375rem 0.375rem;
  background-color: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.18);
  border-radius: 9999px;
}

.dashboard-tablet .user-menu-trigger {
  color: var(--header-text);
  cursor: pointer;
  transition: all 0.15s ease;
}

.dashboard-tablet .user-menu-trigger:hover,
.dashboard-tablet .user-menu-trigger:focus-visible {
  background-color: rgba(255, 255, 255, 0.2);
  border-color: rgba(255, 255, 255, 0.25);
}

.dashboard-tablet .user-menu-trigger:focus-visible {
  outline: 2px solid rgba(255, 255, 255, 0.35);
  outline-offset: 2px;
}

.dashboard-tablet .user-avatar {
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

.dashboard-tablet .user-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--header-text);
}

.dashboard-tablet .user-menu-caret {
  width: 0.75rem;
  height: 0.75rem;
  color: rgba(255, 255, 255, 0.7);
}

.dashboard-tablet .user-menu-dropdown {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  min-width: 12rem;
  padding: 0.75rem;
  border-radius: 0.75rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  box-shadow: 0 20px 40px -24px rgba(15, 23, 42, 0.45);
  z-index: 50;
}

.dashboard-tablet .user-menu-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: none;
  background: transparent;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-weight: 500;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.dashboard-tablet .user-menu-item:hover {
  background-color: var(--bg-hover);
}

.dashboard-tablet .user-menu-item:focus-visible {
  outline: 2px solid var(--btn-primary-bg);
  outline-offset: 2px;
}

.dashboard-tablet .menu-icon {
  width: 1rem;
  height: 1rem;
  color: var(--text-secondary);
}

.dashboard-tablet .user-menu-item:hover .menu-icon {
  color: var(--btn-primary-bg);
}

.dashboard-tablet .menu-fade-enter-active,
.dashboard-tablet .menu-fade-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.dashboard-tablet .menu-fade-enter-from,
.dashboard-tablet .menu-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}

.dashboard-tablet .header-divider {
  margin: 0 0.5rem;
}

.dashboard-tablet .view-toggle {
  padding: 0.375rem;
  gap: 0.125rem;
}

.dashboard-tablet .view-btn {
  padding: 0.5rem 0.625rem;
}

.dashboard-tablet .stats-grid-tablet {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.25rem;
  align-items: stretch;
}

.dashboard-tablet .stats-grid-tablet .stat-card {
  position: relative;
  overflow: hidden;
  display: flex;
  padding: 1.25rem 1.5rem;
  border-radius: 1.25rem;
  background: linear-gradient(135deg, var(--bg-tertiary), var(--bg-secondary));
  border: 1px solid var(--border-primary);
  box-shadow: 0 16px 30px -18px rgba(15, 23, 42, 0.3);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.dashboard-tablet .stats-grid-tablet .stat-card::before {
  content: '';
  position: absolute;
  inset: 0;
  background: radial-gradient(circle at top right, rgba(255, 255, 255, 0.16), transparent 58%);
  opacity: 0.65;
  pointer-events: none;
}

.dashboard-tablet .stats-grid-tablet .stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 22px 40px -20px rgba(15, 23, 42, 0.35);
}

.dashboard-tablet .stats-grid-tablet .stat-content {
  position: relative;
  z-index: 1;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.dashboard-tablet .stats-grid-tablet .icon-container {
  width: 3rem;
  height: 3rem;
  padding: 0.75rem;
  border-radius: 1rem;
  box-shadow: 0 12px 22px -12px rgba(15, 23, 42, 0.35);
}

.dashboard-tablet .stats-grid-tablet .stat-icon {
  width: 1.5rem;
  height: 1.5rem;
}

.dashboard-tablet .stats-grid-tablet .stat-info {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.25rem;
  text-align: right;
}

.dashboard-tablet .stats-grid-tablet .stat-value {
  font-size: 1.75rem;
  font-weight: 700;
  letter-spacing: -0.015em;
  color: var(--text-primary);
}

.dashboard-tablet .stats-grid-tablet .stat-label {
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--text-tertiary);
}

.task-columns.view-columns {
  grid-template-columns: repeat(3, 1fr);
}

.filters-scroll {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
}
</style>
