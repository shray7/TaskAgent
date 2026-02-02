<template>
  <div class="my-tasks-view my-tasks-mobile">
    <header class="header">
      <div class="header-content">
        <span class="page-title">My Tasks</span>
        <div class="user-avatar-sm" :style="authStore.user ? { backgroundColor: getUserColor(authStore.user.id), color: '#fff' } : {}">
          {{ authStore.user ? getInitials(authStore.user) : '' }}
        </div>
      </div>
    </header>

    <main class="main-content main-with-bottom-nav">
      <div class="filters-mobile">
        <select v-model="selectedProjectId" class="select">
          <option :value="null">All Projects</option>
          <option v-for="project in projectsStore.projects" :key="project.id" :value="project.id">{{ project.name }}</option>
        </select>
        <select v-model="selectedStatus" class="select">
          <option value="">Status</option>
          <option value="todo">To Do</option>
          <option value="in-progress">In Progress</option>
          <option value="completed">Done</option>
        </select>
        <select v-model="selectedPriority" class="select">
          <option value="">Priority</option>
          <option value="high">High</option>
          <option value="medium">Medium</option>
          <option value="low">Low</option>
        </select>
        <button v-if="hasActiveFilters" class="btn btn-ghost btn-sm" @click="clearFilters">Clear</button>
      </div>

      <div class="stats-row">
        <div class="stat-mini"><span class="stat-value">{{ todoTasks.length }}</span><span class="stat-label">To Do</span></div>
        <div class="stat-mini"><span class="stat-value">{{ inProgressTasks.length }}</span><span class="stat-label">Active</span></div>
        <div class="stat-mini"><span class="stat-value">{{ completedTasks.length }}</span><span class="stat-label">Done</span></div>
        <div class="stat-mini"><span class="stat-value">{{ overdueTasks.length }}</span><span class="stat-label">Overdue</span></div>
      </div>

      <div class="tasks-list">
        <div v-if="filteredTasks.length === 0" class="empty-state">
          <Inbox class="empty-icon" />
          <p>{{ hasActiveFilters ? 'No tasks match' : 'No tasks assigned' }}</p>
        </div>
        <div v-else class="tasks-stack">
          <div v-for="task in filteredTasks" :key="task.id" class="task-card card" @click="openTask(task)">
            <div class="card-top">
              <span :class="['status-dot', `status-${task.status}`]"></span>
              <h3 class="card-title">{{ task.title }}</h3>
              <span :class="['badge', priorityBadgeClass(task.priority)]">{{ task.priority }}</span>
            </div>
            <div class="card-meta">
              <span class="project" :style="{ color: getProjectColor(task.projectId) }">{{ getProjectName(task.projectId) }}</span>
              <span class="due">{{ formatDueDate(task.dueDate) }}</span>
            </div>
          </div>
        </div>
      </div>
    </main>

    <MobileBottomNav />
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
import { Inbox } from 'lucide-vue-next'
import { useMyTasks } from '../../composables/useMyTasks'
import MobileBottomNav from '@/components/layout/MobileBottomNav.vue'
import TaskForm from '@/features/dashboard/components/TaskForm.vue'

const t = useMyTasks()
const {
  authStore, projectsStore, selectedProjectId, selectedStatus, selectedPriority,
  hasActiveFilters, filteredTasks, todoTasks, inProgressTasks, completedTasks, overdueTasks,
  clearFilters, priorityBadgeClass, getProjectName, getProjectColor, getTaskSizeUnit, getSprintsForTask, formatDueDate,
  showTaskForm, selectedTask, openTask, closeTaskForm, handleSaveTask, handleDeleteTask,
  getInitials, getUserColor
} = t
</script>

<style scoped>
@import '../my-tasks-shared.css';

.my-tasks-mobile .header-content {
  padding: 0.75rem 1rem;
  min-height: 3.5rem;
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.my-tasks-mobile .page-title {
  flex: 1;
  min-width: 0;
  font-size: 1.125rem;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.my-tasks-mobile .user-avatar-sm {
  width: 2rem;
  height: 2rem;
  font-size: 0.75rem;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.5rem;
}

.my-tasks-mobile .main-content { padding: 1rem; }
.my-tasks-mobile .main-with-bottom-nav {
  padding-bottom: calc(3.5rem + env(safe-area-inset-bottom, 0px) + 1rem);
}

.my-tasks-mobile .filters-mobile {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.my-tasks-mobile .filters-mobile .select { flex: 1; min-width: 0; }

.my-tasks-mobile .stats-row {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  overflow-x: auto;
  padding-bottom: 0.25rem;
}

.my-tasks-mobile .stat-mini {
  flex-shrink: 0;
  padding: 0.5rem 0.75rem;
  background: var(--card-bg);
  border: 1px solid var(--card-border);
  border-radius: 0.5rem;
  text-align: center;
}

.my-tasks-mobile .stat-mini .stat-value { display: block; font-size: 1rem; font-weight: 700; }
.my-tasks-mobile .stat-mini .stat-label { font-size: 0.625rem; color: var(--text-muted); }

.my-tasks-mobile .tasks-stack { display: flex; flex-direction: column; gap: 0.75rem; }

.my-tasks-mobile .task-card {
  padding: 1rem;
  cursor: pointer;
}

.my-tasks-mobile .card-top {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.my-tasks-mobile .card-title {
  flex: 1;
  font-size: 0.9375rem;
  font-weight: 600;
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.my-tasks-mobile .card-meta {
  font-size: 0.75rem;
  color: var(--text-muted);
  display: flex;
  justify-content: space-between;
}
</style>
