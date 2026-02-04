<template>
  <div class="dashboard dashboard-mobile">
    <header class="header mobile-header">
      <div class="header-content">
        <div class="header-selectors">
          <ProjectSelector />
          <SprintSelector />
        </div>
      </div>
    </header>

    <button v-if="currentProject" class="fab-new-task" aria-label="New task" @click="showTaskForm = true">
      <Plus class="fab-icon" />
      <span class="fab-label">New</span>
    </button>
    <main class="main-content main-with-bottom-nav">
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
      <div class="view-tabs">
        <button :class="['tab', { active: viewMode === 'list' }]" @click="viewMode = 'list'">List</button>
        <button :class="['tab', { active: viewMode === 'columns' }]" @click="viewMode = 'columns'">Board</button>
        <button :class="['tab', { active: viewMode === 'analytics' }]" @click="viewMode = 'analytics'">Analytics</button>
      </div>

      <div class="stats-row">
        <div v-for="col in visibleColumnConfigs" :key="col.status" class="stat-mini">
          <component :is="col.icon" class="stat-mini-icon" />
          <div class="stat-mini-text">
            <span class="stat-value">{{ col.tasks.length }}</span>
            <span class="stat-label">{{ col.label }}</span>
          </div>
        </div>
      </div>

      <div v-if="viewMode !== 'analytics'" class="filters-mobile">
        <select v-model="filters.assigneeId" class="select select-sm">
          <option value="">All</option>
          <option v-for="user in authStore.users" :key="user.id" :value="user.id">{{ user.name }}</option>
        </select>
        <select v-model="filters.priority" class="select select-sm">
          <option value="">All priorities</option>
          <option value="high">High</option>
          <option value="medium">Medium</option>
          <option value="low">Low</option>
        </select>
      </div>

      <!-- List view: cards -->
      <div v-if="viewMode === 'list'" class="task-cards-mobile">
        <div
          v-for="task in sortedAllTasks"
          :key="task.id"
          class="task-card-mobile card"
          @click="handleEditTask(task)"
        >
          <div class="card-header">
            <span :class="['status-dot', `status-${task.status}`]"></span>
            <h3 class="card-title">{{ task.title }}</h3>
            <span :class="['badge', `priority-${task.priority}`]">{{ task.priority }}</span>
          </div>
          <div class="card-meta">
            <span>{{ statusLabel(task.status) }}</span>
            <span>{{ formatDueDate(task.dueDate) }}</span>
          </div>
        </div>
        <p v-if="sortedAllTasks.length === 0" class="empty-msg">No tasks</p>
      </div>

      <!-- Board view: swipeable columns -->
      <div v-else-if="viewMode === 'columns'" class="board-swipe">
        <div class="swipe-track" ref="swipeTrackRef">
          <section
            v-for="col in visibleColumnConfigs"
            :key="col.status"
            class="swipe-column"
            @dragover.prevent="handleDragOver($event, col.status)"
            @dragleave="handleDragLeave"
            @drop="handleDrop($event, col.status)"
            :class="{ 'drag-over': dragOverColumn === col.status }"
          >
            <div class="column-header">
              <component :is="col.icon" class="column-header-icon" />
              <span :class="['status-dot', col.dotClass]"></span>
              {{ col.label }} ({{ col.tasks.length }})
            </div>
            <div v-if="col.tasks.length === 0" class="empty-state">
              <component :is="col.icon" class="empty-icon" />
              Drop here
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
        <div class="swipe-dots">
          <span
            v-for="(col, i) in visibleColumnConfigs"
            :key="col.status"
            :class="['dot', { active: activeColumnIndex === i }]"
            @click="scrollToColumn(i)"
          />
        </div>
      </div>

      <!-- Analytics -->
      <AnalyticsMobile
        v-else
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

      <section v-if="viewMode === 'analytics' && currentProject" class="dashboard-team-section card">
        <div class="dashboard-team-header">
          <h3 class="dashboard-team-title"><Users class="dashboard-team-icon" /> Team</h3>
        </div>
        <div class="dashboard-team-list">
          <div v-for="user in projectTeamMembers" :key="user.id" class="dashboard-team-member">
            <span class="team-member-avatar" :style="{ backgroundColor: getUserColor(user.id) }">{{ getInitials(user) }}</span>
            <span class="team-member-name">{{ user.name }}</span>
            <span class="team-member-badge badge badge-gray">{{ getUserTaskCount(user.id) }}</span>
          </div>
        </div>
      </section>
      </template>
    </main>

    <TaskForm v-if="showTaskForm" :task="editingTask" @close="closeTaskForm" @submit="handleTaskSubmit" @delete="handleDeleteTaskFromForm" />
    <MobileBottomNav />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { Plus, Users, CheckSquare } from 'lucide-vue-next'
import { useDashboard } from '../../composables/useDashboard'
import ProjectSelector from '@/components/layout/ProjectSelector.vue'
import SprintSelector from '@/components/layout/SprintSelector.vue'
import MobileBottomNav from '@/components/layout/MobileBottomNav.vue'
import TaskCard from '../../components/TaskCard.vue'
import TaskForm from '../../components/TaskForm.vue'
import AnalyticsMobile from '@/features/analytics/components/AnalyticsMobile.vue'

const d = useDashboard()
const {
  authStore, tasksStore, projectsStore, sprintsStore,
  showTaskForm, editingTask, viewMode, dragOverColumn,
  filters, visibleColumnConfigs, filteredTodoTasks, filteredInProgressTasks, filteredCompletedTasks,
  allFilteredTasks, sortedAllTasks, currentProject, currentSprint, taskSizeUnit,
  projectTeamMembers, statusLabel, formatDueDate, getUserTaskCount, getInitials, getUserColor,
  handleDeleteTask, handleStatusChange, handleInlineUpdate,
  handleDragStart, handleDragEnd, handleDragOver, handleDragLeave, handleDrop,
  handleEditTask, handleTaskSubmit, handleDeleteTaskFromForm, closeTaskForm
} = d

const swipeTrackRef = ref<HTMLElement | null>(null)
const activeColumnIndex = ref(0)

function scrollToColumn(i: number) {
  const el = swipeTrackRef.value
  if (!el) return
  const col = el.children[i] as HTMLElement
  col?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'start' })
  activeColumnIndex.value = i
}

watch(swipeTrackRef, (el) => {
  if (!el) return
  el.addEventListener('scroll', () => {
    const scrollLeft = el.scrollLeft
    const colWidth = el.clientWidth
    const i = Math.round(scrollLeft / colWidth)
    activeColumnIndex.value = Math.min(i, visibleColumnConfigs.value.length - 1)
  })
})
</script>

<style scoped>
@import '../dashboard-shared.css';

.dashboard-mobile .mobile-header .header {
  position: sticky;
  top: 0;
  left: 0;
  right: 0;
  z-index: 40;
  background-color: var(--header-bg);
  border-bottom: 1px solid var(--header-border);
}

.dashboard-mobile .mobile-header .header-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.625rem 1rem;
  min-height: 3rem;
  max-width: none;
}

.dashboard-mobile .header-selectors {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  flex: 1;
  width: 100%;
}

.dashboard-mobile .header-selectors :deep(.project-selector),
.dashboard-mobile .header-selectors :deep(.sprint-selector) {
  min-width: 0;
  flex: 1 1 0;
}

.dashboard-mobile .header-selectors :deep(.project-button),
.dashboard-mobile .header-selectors :deep(.sprint-button) {
  min-width: 0;
  min-height: 2.5rem;
  padding: 0.5rem 0.625rem;
  font-size: 0.8125rem;
  width: 100%;
  max-width: 100%;
}

.dashboard-mobile .header-selectors :deep(.project-name),
.dashboard-mobile .header-selectors :deep(.sprint-name) {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dashboard-mobile .header-selectors :deep(.sprint-button-content) {
  min-width: 0;
  overflow: hidden;
}

.dashboard-mobile .main-with-bottom-nav {
  padding-bottom: calc(3.5rem + env(safe-area-inset-bottom, 0px) + 4rem);
}

.fab-new-task {
  position: fixed;
  bottom: calc(3.5rem + env(safe-area-inset-bottom, 0px) + 0.5rem);
  right: 1rem;
  z-index: 35;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.375rem;
  padding: 0.625rem 1rem;
  background: var(--btn-primary-bg);
  color: var(--btn-primary-text);
  border: none;
  border-radius: 9999px;
  font-size: 0.875rem;
  font-weight: 600;
  box-shadow: var(--shadow-lg);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.fab-new-task:hover {
  transform: scale(1.05);
  box-shadow: var(--shadow-xl);
}

.fab-new-task:active {
  transform: scale(0.98);
}

.fab-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.fab-label {
  white-space: nowrap;
}

.dashboard-mobile .main-content { padding: 1rem; }

.view-tabs {
  display: flex;
  gap: 0.25rem;
  margin-bottom: 1rem;
  background: var(--bg-tertiary);
  padding: 0.25rem;
  border-radius: 0.5rem;
}

.tab {
  flex: 1;
  padding: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 500;
  background: transparent;
  border: none;
  border-radius: 0.375rem;
  color: var(--text-muted);
  cursor: pointer;
}

.tab.active {
  background: var(--bg-primary);
  color: var(--text-primary);
}

.stats-row {
  display: flex;
  justify-content: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-bottom: 1rem;
  padding-bottom: 0.25rem;
}

.stat-mini {
  flex-shrink: 0;
  padding: 0.5rem 0.75rem;
  background: var(--card-bg);
  border: 1px solid var(--card-border);
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.stat-mini .stat-mini-icon {
  width: 1.25rem;
  height: 1.25rem;
  color: var(--text-muted);
  flex-shrink: 0;
}

.stat-mini .stat-mini-text {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.125rem;
}

.stat-mini .stat-value { font-size: 1.125rem; font-weight: 700; line-height: 1; }
.stat-mini .stat-label { font-size: 0.6875rem; color: var(--text-muted); line-height: 1; }

.filters-mobile {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.filters-mobile .select { flex: 1; }

.task-cards-mobile { display: flex; flex-direction: column; gap: 0.75rem; }

.task-card-mobile {
  padding: 1rem;
  cursor: pointer;
}

.card-header {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.card-title {
  flex: 1;
  font-size: 0.9375rem;
  font-weight: 600;
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-meta {
  font-size: 0.75rem;
  color: var(--text-muted);
  display: flex;
  gap: 1rem;
}

.empty-msg { text-align: center; color: var(--text-muted); padding: 2rem; }

/* Swipe board */
.board-swipe { margin-top: 0.5rem; }
.swipe-track {
  display: flex;
  overflow-x: auto;
  scroll-snap-type: x mandatory;
  -webkit-overflow-scrolling: touch;
  gap: 1rem;
  padding-bottom: 1rem;
}

.swipe-column {
  flex: 0 0 calc(100% - 2rem);
  min-width: calc(100% - 2rem);
  scroll-snap-align: start;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  padding: 1rem;
}

.swipe-column .column-header {
  font-size: 0.875rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.swipe-column .column-header-icon {
  width: 1.125rem;
  height: 1.125rem;
  flex-shrink: 0;
  color: var(--text-secondary);
}

.swipe-column .empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem;
  color: var(--text-muted);
  font-size: 0.875rem;
}

.swipe-column .empty-state .empty-icon {
  width: 2rem;
  height: 2rem;
  color: var(--text-muted);
  opacity: 0.7;
}

.swipe-dots {
  display: flex;
  justify-content: center;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.swipe-dots .dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--text-muted);
  opacity: 0.5;
  cursor: pointer;
}

.swipe-dots .dot.active {
  opacity: 1;
  background: var(--color-brown-500);
}
</style>
