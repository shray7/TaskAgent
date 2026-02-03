<template>
  <div class="dashboard project-settings-page">
    <header class="header">
      <div class="header-content settings-header">
        <router-link to="/dashboard" class="back-link">
          <ArrowLeft class="back-icon" />
          <span class="back-text">{{ isMobile() ? 'Back' : 'Dashboard' }}</span>
        </router-link>
        <h1 class="settings-title">Project settings</h1>
      </div>
    </header>

    <main class="main-content settings-main" :class="{ 'main-with-bottom-nav': isMobile() }">
      <div v-if="!currentProject" class="card empty-state-card">
        <p>No project selected.</p>
        <router-link to="/dashboard" class="btn btn-primary">Go to Dashboard</router-link>
      </div>

      <div v-else class="card settings-card">
        <form @submit.prevent="save" class="settings-form">
          <div class="form-section">
            <h2 class="form-section-title">General</h2>
            <div class="form-group">
              <label class="form-label">Project name</label>
              <input v-model="form.name" type="text" class="input" required />
            </div>
            <div class="form-group">
              <label class="form-label">Description</label>
              <textarea v-model="form.description" class="input textarea" rows="3" />
            </div>
            <div class="form-group">
              <label class="form-label">Color</label>
              <div class="color-options">
                <button
                  v-for="color in projectColors"
                  :key="color"
                  type="button"
                  @click="form.color = color"
                  :class="['color-option', { selected: form.color === color }]"
                  :style="{ backgroundColor: color }"
                >
                  <Check v-if="form.color === color" class="color-check" />
                </button>
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Start date</label>
                <input v-model="form.startDate" type="date" class="input" />
              </div>
              <div class="form-group">
                <label class="form-label">End date</label>
                <input v-model="form.endDate" type="date" class="input" />
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Owner</label>
              <select v-model.number="form.ownerId" class="select">
                <option v-for="user in authStore.users" :key="user.id" :value="user.id">
                  {{ user.name }}
                </option>
              </select>
            </div>
          </div>

          <div class="form-section">
            <h2 class="form-section-title">Board</h2>
            <div class="form-group">
              <label class="form-label">Sprint duration</label>
              <select v-model.number="form.sprintDurationDays" class="select">
                <option v-for="days in sprintDurationOptions" :key="days" :value="days">
                  {{ days }} days
                </option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Task size unit</label>
              <div class="task-size-options">
                <label class="unit-option">
                  <input type="radio" v-model="form.taskSizeUnit" value="hours" />
                  <span>Hours</span>
                </label>
                <label class="unit-option">
                  <input type="radio" v-model="form.taskSizeUnit" value="days" />
                  <span>Days</span>
                </label>
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Columns to show</label>
              <p class="form-hint">Which status columns appear on the board.</p>
              <div class="column-checkboxes">
                <label v-for="col in columnOptions" :key="col.value" class="checkbox-option">
                  <input type="checkbox" v-model="form.visibleColumns" :value="col.value" />
                  <span>{{ col.label }}</span>
                </label>
              </div>
            </div>
          </div>

          <div class="form-section">
            <h2 class="form-section-title">Visibility</h2>
            <p class="form-hint">Who can see this project. Leave all unchecked for everyone.</p>
            <div class="visibility-options">
              <label
                v-for="user in authStore.users"
                :key="user.id"
                class="visibility-option"
              >
                <input type="checkbox" v-model="form.visibleToUserIds" :value="user.id" />
                <span class="visibility-avatar" :style="{ backgroundColor: getUserColor(user.id), color: '#fff' }">
                  {{ getInitials(user) }}
                </span>
                <span>{{ user.name }}</span>
              </label>
            </div>
          </div>

          <div class="form-actions">
            <router-link to="/dashboard" class="btn btn-secondary">Cancel</router-link>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? 'Saving...' : 'Save changes' }}
            </button>
          </div>
        </form>

        <div v-if="isOwner" class="danger-zone card">
          <h2 class="danger-zone-title">Danger zone</h2>
          <p class="danger-zone-desc">Deleting this project will soft-delete all its tasks and sprints. This cannot be undone.</p>
          <button type="button" @click="confirmDelete" class="btn btn-danger">
            Delete project
          </button>
        </div>
      </div>
    </main>

    <Teleport to="body">
      <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="showDeleteConfirm = false">
        <div class="modal-content confirm-modal">
          <h3>Delete project?</h3>
          <p>This will remove "{{ currentProject?.name }}" and soft-delete all its tasks and sprints. You can't undo this.</p>
          <div class="confirm-actions">
            <button type="button" @click="showDeleteConfirm = false" class="btn btn-secondary">Cancel</button>
            <button type="button" @click="executeDelete" class="btn btn-danger" :disabled="deleting">
              {{ deleting ? 'Deleting...' : 'Delete project' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
    <MobileBottomNav v-if="isMobile()" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowLeft, Check } from 'lucide-vue-next'
import { useViewport } from '@/composables/useViewport'
import MobileBottomNav from '@/components/layout/MobileBottomNav.vue'
import { useAuthStore } from '@/stores/auth'
import { useNotificationsStore } from '@/stores/notifications'
import { useProjectsStore } from '@/stores/projects'
import { useTasksStore } from '@/stores/tasks'
import { useSprintsStore } from '@/stores/sprints'
import { getInitials, getUserColor } from '@/utils/initials'
import type { Project, TaskStatus } from '@/types'
import { SPRINT_DURATION_OPTIONS, COLUMN_OPTIONS } from '@/types'

const router = useRouter()
const { isMobile } = useViewport()
const authStore = useAuthStore()
const notificationsStore = useNotificationsStore()
const projectsStore = useProjectsStore()
const tasksStore = useTasksStore()
const sprintsStore = useSprintsStore()

const projectColors = [
  '#B05A36', '#4F46E5', '#059669', '#DC2626',
  '#D97706', '#7C3AED', '#0891B2', '#DB2777'
]
const sprintDurationOptions = [...SPRINT_DURATION_OPTIONS]
const columnOptions = COLUMN_OPTIONS

const currentProject = computed(() => projectsStore.currentProject)
const currentUserId = computed(() => (authStore.user as { id: number } | null)?.id ?? 0)
const isOwner = computed(() => currentProject.value?.ownerId === currentUserId.value)

const saving = ref(false)
const deleting = ref(false)
const showDeleteConfirm = ref(false)

const form = ref({
  name: '',
  description: '',
  color: '#B05A36',
  startDate: '',
  endDate: '',
  ownerId: 0,
  sprintDurationDays: 14,
  taskSizeUnit: 'hours' as 'hours' | 'days',
  visibleColumns: ['todo', 'in-progress', 'completed'] as TaskStatus[],
  visibleToUserIds: [] as number[]
})

watch(
  currentProject,
  (p) => {
    if (!p) return
    form.value = {
      name: p.name,
      description: p.description ?? '',
      color: p.color ?? projectColors[0],
      startDate: p.startDate ? (new Date(p.startDate).toISOString().split('T')[0] ?? '') : '',
      endDate: p.endDate ? (new Date(p.endDate).toISOString().split('T')[0] ?? '') : '',
      ownerId: p.ownerId,
      sprintDurationDays: p.sprintDurationDays ?? 14,
      taskSizeUnit: p.taskSizeUnit ?? 'hours',
      visibleColumns: p.visibleColumns?.length ? [...p.visibleColumns] : ['todo', 'in-progress', 'completed'],
      visibleToUserIds: p.visibleToUserIds ? [...p.visibleToUserIds] : []
    }
  },
  { immediate: true }
)

async function save() {
  if (!currentProject.value) return
  saving.value = true
  try {
    const unitChanged = form.value.taskSizeUnit !== (currentProject.value.taskSizeUnit ?? 'hours')
    const updates: Partial<Project> = {
      name: form.value.name.trim(),
      description: form.value.description?.trim() ?? '',
      color: form.value.color,
      startDate: form.value.startDate ? new Date(form.value.startDate) : undefined,
      endDate: form.value.endDate ? new Date(form.value.endDate) : undefined,
      ownerId: form.value.ownerId,
      sprintDurationDays: form.value.sprintDurationDays,
      taskSizeUnit: form.value.taskSizeUnit,
      visibleColumns: form.value.visibleColumns.length ? form.value.visibleColumns : undefined,
      visibleToUserIds: form.value.visibleToUserIds.length ? form.value.visibleToUserIds : undefined
    }
    await projectsStore.updateProject(currentProject.value.id, updates)
    if (unitChanged) {
      await tasksStore.fetchTasks({
        projectId: currentProject.value.id,
        sprintId: sprintsStore.currentSprintId ?? undefined
      })
    }
    router.push('/dashboard')
  } finally {
    saving.value = false
  }
}

function confirmDelete() {
  showDeleteConfirm.value = true
}

async function executeDelete() {
  if (!currentProject.value) return
  deleting.value = true
  try {
    const result = await projectsStore.deleteProject(currentProject.value.id, currentUserId.value)
    if (result.success) {
      showDeleteConfirm.value = false
      router.push('/dashboard')
    } else {
      notificationsStore.showError(result.message ?? 'Failed to delete project')
    }
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
@import './dashboard-shared.css';

.project-settings-page .header {
  position: sticky;
  top: 0;
  z-index: 40;
  background-color: var(--header-bg);
  border-bottom: 1px solid var(--header-border);
}

.settings-header {
  max-width: 42rem;
  margin: 0 auto;
  padding: 1rem 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  min-width: 0;
}

.back-link {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--text-secondary);
  text-decoration: none;
  font-size: 0.875rem;
  font-weight: 500;
  flex-shrink: 0;
}

.back-link:hover {
  color: var(--text-primary);
}

.back-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.settings-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.settings-main {
  max-width: 42rem;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

.settings-main.main-with-bottom-nav {
  padding-bottom: calc(3.5rem + env(safe-area-inset-bottom, 0px) + 2rem);
}

.empty-state-card {
  padding: 2rem;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.settings-card {
  padding: 0;
  overflow: hidden;
}

.settings-form {
  padding: 1.5rem;
}

.form-section {
  margin-bottom: 2rem;
}

.form-section:last-of-type {
  margin-bottom: 1.5rem;
}

.form-section-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 1rem 0;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid var(--border-primary);
}

.form-group {
  margin-bottom: 1rem;
}

.form-label {
  display: block;
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--text-secondary);
  margin-bottom: 0.375rem;
}

.form-hint {
  font-size: 0.75rem;
  color: var(--text-muted);
  margin: 0 0 0.5rem 0;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.input, .select, .textarea {
  width: 100%;
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
  border: 1px solid var(--border-primary);
  border-radius: 0.5rem;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.textarea {
  resize: vertical;
  min-height: 4rem;
}

.color-options {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.color-option {
  width: 2rem;
  height: 2rem;
  border-radius: 0.5rem;
  border: 2px solid transparent;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
}

.color-option.selected {
  border-color: var(--text-primary);
}

.color-check {
  width: 1rem;
  height: 1rem;
  color: white;
  filter: drop-shadow(0 0 1px rgba(0,0,0,0.5));
}

.task-size-options, .column-checkboxes {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
}

.unit-option, .checkbox-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.875rem;
}

.visibility-options {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.visibility-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.875rem;
}

.visibility-avatar {
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 0.375rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 600;
}

.form-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 0.5rem;
}

.danger-zone {
  margin-top: 2rem;
  padding: 1.5rem;
  border-color: var(--badge-red-bg, #fef2f2);
  background: var(--badge-red-bg, #fef2f2);
}

.danger-zone-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--badge-red-text, #b91c1c);
  margin: 0 0 0.5rem 0;
}

.danger-zone-desc {
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin: 0 0 1rem 0;
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: var(--overlay-bg);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  z-index: 50;
}

.confirm-modal {
  max-width: 24rem;
  padding: 1.5rem;
}

.confirm-modal h3 {
  margin: 0 0 0.5rem 0;
  font-size: 1.125rem;
}

.confirm-modal p {
  margin: 0 0 1.25rem 0;
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.confirm-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
}

.btn-danger {
  background-color: var(--badge-red-bg, #fef2f2);
  color: var(--badge-red-text, #b91c1c);
  border: 1px solid var(--badge-red-text, #b91c1c);
}

.btn-danger:hover:not(:disabled) {
  background-color: var(--badge-red-text, #b91c1c);
  color: white;
}

@media (max-width: 480px) {
  .form-row {
    grid-template-columns: 1fr;
  }
}
</style>
