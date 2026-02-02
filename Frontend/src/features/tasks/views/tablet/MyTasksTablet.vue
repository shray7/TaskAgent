<template>
  <div class="my-tasks-view my-tasks-tablet">
    <header class="header">
      <div class="header-content">
        <div class="header-left">
          <HamburgerNav :show-project-sprint="false" />
          <span class="page-title">My Tasks</span>
        </div>
        <div class="header-right">
          <ThemeToggle class="header-theme-toggle" />
          <div class="user-pill">
            <span class="user-avatar" :style="authStore.user ? { backgroundColor: getUserColor(authStore.user.id), color: '#fff' } : {}">
              {{ authStore.user ? getInitials(authStore.user) : '' }}
            </span>
            <span class="user-name">{{ authStore.user?.name }}</span>
          </div>
          <button class="header-btn" title="Logout" @click="handleLogout">
            <LogOut class="btn-icon" />
          </button>
        </div>
      </div>
    </header>

    <main class="main-content">
      <div class="filters-row">
        <select v-model="selectedProjectId" class="select">
          <option :value="null">All Projects</option>
          <option v-for="project in projectsStore.projects" :key="project.id" :value="project.id">{{ project.name }}</option>
        </select>
        <select v-model="selectedStatus" class="select">
          <option value="">All Status</option>
          <option value="todo">To Do</option>
          <option value="in-progress">In Progress</option>
          <option value="completed">Completed</option>
        </select>
        <select v-model="selectedPriority" class="select">
          <option value="">All Priority</option>
          <option value="high">High</option>
          <option value="medium">Medium</option>
          <option value="low">Low</option>
        </select>
        <button v-if="hasActiveFilters" class="btn btn-ghost btn-sm" @click="clearFilters">Clear</button>
      </div>

      <div class="stats-grid">
        <div class="stat-card card"><div class="stat-content"><div class="icon-container icon-blue"><Circle class="stat-icon" /></div><div class="stat-info"><p class="stat-value">{{ todoTasks.length }}</p><p class="stat-label">To Do</p></div></div></div>
        <div class="stat-card card"><div class="stat-content"><div class="icon-container icon-amber"><Clock class="stat-icon" /></div><div class="stat-info"><p class="stat-value">{{ inProgressTasks.length }}</p><p class="stat-label">In Progress</p></div></div></div>
        <div class="stat-card card"><div class="stat-content"><div class="icon-container icon-green"><CheckCircle2 class="stat-icon" /></div><div class="stat-info"><p class="stat-value">{{ completedTasks.length }}</p><p class="stat-label">Done</p></div></div></div>
        <div class="stat-card card"><div class="stat-content"><div class="icon-container icon-red"><AlertTriangle class="stat-icon" /></div><div class="stat-info"><p class="stat-value">{{ overdueTasks.length }}</p><p class="stat-label">Overdue</p></div></div></div>
      </div>

      <div class="tasks-list">
        <div v-if="filteredTasks.length === 0" class="empty-state">
          <Inbox class="empty-icon" />
          <p>{{ hasActiveFilters ? 'No tasks match filters' : 'No tasks assigned' }}</p>
        </div>
        <div v-else class="tasks-grid">
          <div v-for="task in filteredTasks" :key="task.id" class="task-item card" @click="openTask(task)">
            <div class="task-header">
              <span :class="['status-dot', `status-${task.status}`]"></span>
              <h3 class="task-title">{{ task.title }}</h3>
              <span :class="['badge', priorityBadgeClass(task.priority)]">{{ task.priority }}</span>
            </div>
            <p v-if="task.description" class="task-desc">{{ task.description }}</p>
            <div class="task-meta">
              <span class="meta-tag project-tag" :style="{ borderColor: getProjectColor(task.projectId) }">{{ getProjectName(task.projectId) }}</span>
              <span class="meta-tag">{{ formatDueDate(task.dueDate) }}</span>
            </div>
          </div>
        </div>
      </div>
    </main>

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
import { Circle, Clock, CheckCircle2, AlertTriangle, LogOut, Inbox } from 'lucide-vue-next'
import { useMyTasks } from '../../composables/useMyTasks'
import HamburgerNav from '@/components/layout/HamburgerNav.vue'
import ThemeToggle from '@/components/ui/ThemeToggle.vue'
import TaskForm from '@/features/dashboard/components/TaskForm.vue'

const t = useMyTasks()
const {
  authStore, projectsStore, selectedProjectId, selectedStatus, selectedPriority,
  hasActiveFilters, filteredTasks, todoTasks, inProgressTasks, completedTasks, overdueTasks,
  clearFilters, priorityBadgeClass, getProjectName, getProjectColor, getTaskSizeUnit, getSprintsForTask, formatDueDate,
  showTaskForm, selectedTask, openTask, closeTaskForm, handleSaveTask, handleDeleteTask, handleLogout,
  getInitials, getUserColor
} = t
</script>

<style scoped>
@import '../my-tasks-shared.css';

.my-tasks-tablet .stats-grid {
  grid-template-columns: repeat(4, 1fr);
}

.my-tasks-tablet .tasks-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}
</style>
