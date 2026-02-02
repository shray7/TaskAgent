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
          <button class="btn btn-primary btn-sm" @click="showTaskForm = true">
            <Plus class="btn-icon" />
            New Task
          </button>
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

    <TaskForm v-if="showTaskForm" :task="editingTask" @close="closeTaskForm" @submit="handleTaskSubmit" />
  </div>
</template>

<script setup lang="ts">
import { Plus, List, Columns3, BarChart2, LogOut, Users } from 'lucide-vue-next'
import { useDashboard } from '../../composables/useDashboard'
import HamburgerNav from '@/components/layout/HamburgerNav.vue'
import ThemeToggle from '@/components/ui/ThemeToggle.vue'
import TaskCard from '../../components/TaskCard.vue'
import TaskForm from '../../components/TaskForm.vue'
import CommentList from '../../components/CommentList.vue'
import AnalyticsTablet from '@/features/analytics/components/AnalyticsTablet.vue'

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
  handleEditTask, handleTaskSubmit, closeTaskForm
} = d
</script>

<style scoped>
@import '../dashboard-shared.css';

.dashboard-tablet .stats-grid-tablet {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}

.task-columns.view-columns {
  grid-template-columns: repeat(3, 1fr);
}

.filters-scroll {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
}
</style>
