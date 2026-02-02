<template>
  <div class="dashboard dashboard-mobile">
    <header class="header">
      <div class="header-content">
        <HamburgerNav :show-project-sprint="true" />
        <button class="btn btn-primary btn-sm new-task-btn" @click="showTaskForm = true">
          <Plus class="btn-icon" />
          New
        </button>
        <ThemeToggle class="header-theme-toggle" />
        <div class="user-avatar-sm" :style="authStore.user ? { backgroundColor: getUserColor(authStore.user.id), color: '#fff' } : {}">
          {{ authStore.user ? getInitials(authStore.user) : '' }}
        </div>
      </div>
    </header>

    <main class="main-content">
      <div class="view-tabs">
        <button :class="['tab', { active: viewMode === 'list' }]" @click="viewMode = 'list'">List</button>
        <button :class="['tab', { active: viewMode === 'columns' }]" @click="viewMode = 'columns'">Board</button>
        <button :class="['tab', { active: viewMode === 'analytics' }]" @click="viewMode = 'analytics'">Analytics</button>
      </div>

      <div class="stats-row">
        <div v-for="col in visibleColumnConfigs" :key="col.status" class="stat-mini">
          <span class="stat-value">{{ col.tasks.length }}</span>
          <span class="stat-label">{{ col.label }}</span>
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
              <span :class="['status-dot', col.dotClass]"></span>
              {{ col.label }} ({{ col.tasks.length }})
            </div>
            <div v-if="col.tasks.length === 0" class="empty-state">Drop here</div>
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
    </main>

    <TaskForm v-if="showTaskForm" :task="editingTask" @close="closeTaskForm" @submit="handleTaskSubmit" />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { Plus, Users } from 'lucide-vue-next'
import { useDashboard } from '../../composables/useDashboard'
import HamburgerNav from '@/components/layout/HamburgerNav.vue'
import ThemeToggle from '@/components/ui/ThemeToggle.vue'
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
  handleEditTask, handleTaskSubmit, closeTaskForm
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

.dashboard-mobile .header-content {
  padding: 0 1rem;
  height: 3.5rem;
}

.dashboard-mobile .header-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.dashboard-mobile .new-task-btn { margin-left: auto; }
.dashboard-mobile .user-avatar-sm {
  width: 2rem;
  height: 2rem;
  font-size: 0.75rem;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.5rem;
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
  gap: 0.75rem;
  margin-bottom: 1rem;
  overflow-x: auto;
  padding-bottom: 0.25rem;
}

.stat-mini {
  flex-shrink: 0;
  padding: 0.5rem 0.75rem;
  background: var(--card-bg);
  border: 1px solid var(--card-border);
  border-radius: 0.5rem;
  text-align: center;
}

.stat-mini .stat-value { display: block; font-size: 1.125rem; font-weight: 700; }
.stat-mini .stat-label { font-size: 0.6875rem; color: var(--text-muted); }

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
