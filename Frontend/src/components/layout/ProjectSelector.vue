<template>
  <div class="project-selector">
    <button 
      @click="toggleDropdown" 
      class="project-button"
      :style="{ '--project-color': currentProject?.color || '#B05A36' }"
    >
      <div class="project-indicator" :style="{ backgroundColor: currentProject?.color }"></div>
      <span class="project-name">{{ currentProject?.name || 'Select Project' }}</span>
      <ChevronDown class="chevron-icon" :class="{ 'rotate': isOpen }" />
    </button>

    <Transition name="dropdown">
      <div v-if="isOpen" class="dropdown-menu">
        <div class="dropdown-header">
          <span class="dropdown-title">Projects</span>
          <button @click="showNewProjectForm = true" class="add-project-btn" title="New Project">
            <Plus class="add-icon" />
          </button>
        </div>

        <div class="project-list">
          <div
            v-for="project in visibleProjects"
            :key="project.id"
            class="project-row"
          >
            <button
              @click="selectProject(project.id)"
              :class="['project-item', { active: project.id === currentProjectId }]"
            >
              <div class="project-color" :style="{ backgroundColor: project.color }"></div>
              <div class="project-info">
                <span class="project-item-name">{{ project.name }}</span>
                <span class="project-task-count">{{ getProjectTaskCount(project.id) }} tasks</span>
              </div>
              <Check v-if="project.id === currentProjectId" class="check-icon" />
            </button>
            <button
              v-if="canEditVisibility(project)"
              type="button"
              @click.stop="openVisibilityPopover(project)"
              class="visibility-btn"
              :title="visibilityButtonTitle(project)"
            >
              <Users class="visibility-icon" />
            </button>
            <button
              v-if="isProjectOwner(project)"
              type="button"
              @click.stop="confirmDeleteProject(project)"
              class="visibility-btn delete-btn"
              title="Delete project"
            >
              <Trash2 class="visibility-icon" />
            </button>
            <div
              v-if="visibilityPopoverProjectId === project.id"
              class="visibility-popover"
            >
              <div class="visibility-popover-header">
                <span>Who can see this board</span>
                <button type="button" @click="closeVisibilityPopover" class="popover-close">
                  <X class="popover-close-icon" />
                </button>
              </div>
              <div class="visibility-options">
                <label
                  v-for="user in authStore.users"
                  :key="user.id"
                  class="visibility-option"
                >
                  <input
                    type="checkbox"
                    :checked="isUserVisible(project, user.id)"
                    @change="toggleUserVisibility(project, user.id)"
                  />
                  <span class="visibility-option-avatar" :style="{ backgroundColor: getUserColor(user.id), color: '#fff' }">{{ getInitials(user) }}</span>
                  <span class="visibility-option-name">{{ user.name }}</span>
                </label>
              </div>
              <p class="visibility-hint">Uncheck all = visible to everyone</p>
            </div>
          </div>
        </div>
      </div>
    </Transition>

    <!-- New Project Modal -->
    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showNewProjectForm" class="modal-overlay" @click.self="closeNewProjectForm">
          <div class="modal-content">
            <div class="modal-header">
              <h3 class="modal-title">
                <FolderPlus class="modal-title-icon" />
                Create New Project
              </h3>
              <button @click="closeNewProjectForm" class="modal-close">
                <X class="close-icon" />
              </button>
            </div>

            <form @submit.prevent="createProject" class="project-form">
              <div class="form-group">
                <label class="form-label">Project Name</label>
                <input
                  v-model="newProject.name"
                  type="text"
                  class="input"
                  placeholder="e.g., Mobile App Development"
                  required
                />
              </div>

              <div class="form-group">
                <label class="form-label">Description</label>
                <textarea
                  v-model="newProject.description"
                  class="textarea"
                  placeholder="Brief description of the project..."
                  rows="3"
                ></textarea>
              </div>

              <div class="form-group">
                <label class="form-label">Project Color</label>
                <div class="color-options">
                  <button
                    v-for="color in projectColors"
                    :key="color"
                    type="button"
                    @click="newProject.color = color"
                    :class="['color-option', { selected: newProject.color === color }]"
                    :style="{ backgroundColor: color }"
                  >
                    <Check v-if="newProject.color === color" class="color-check" />
                  </button>
                </div>
              </div>

              <div class="form-group">
                <label class="form-label">Who can see this board</label>
                <p class="form-hint">Leave all unchecked for everyone. Check specific users to restrict.</p>
                <div class="visibility-create-options">
                  <label
                    v-for="user in authStore.users"
                    :key="user.id"
                    class="visibility-create-option"
                  >
                    <input type="checkbox" v-model="newProject.visibleToUserIds" :value="user.id" />
                    <span class="visibility-option-avatar" :style="{ backgroundColor: getUserColor(user.id), color: '#fff' }">{{ getInitials(user) }}</span>
                    <span class="visibility-option-name">{{ user.name }}</span>
                  </label>
                </div>
              </div>

              <div class="form-row">
                <div class="form-group">
                  <label class="form-label">Start date</label>
                  <input v-model="newProject.startDate" type="date" class="input" />
                </div>
                <div class="form-group">
                  <label class="form-label">End date</label>
                  <input v-model="newProject.endDate" type="date" class="input" />
                </div>
              </div>

              <div class="form-group">
                <label class="form-label">Sprint duration</label>
                <select v-model="newProject.sprintDurationDays" class="select">
                  <option v-for="days in sprintDurationOptions" :key="days" :value="days">
                    {{ days }} days ({{ days === 7 ? '1 week' : days === 14 ? '2 weeks' : days === 21 ? '3 weeks' : '4 weeks' }})
                  </option>
                </select>
              </div>

              <div class="form-group">
                <label class="form-label">Columns to show</label>
                <p class="form-hint">Choose which status columns appear on the board.</p>
                <div class="visibility-create-options column-options">
                  <label
                    v-for="col in columnOptions"
                    :key="col.value"
                    class="visibility-create-option"
                  >
                    <input type="checkbox" v-model="newProject.visibleColumns" :value="col.value" />
                    <span class="visibility-option-name">{{ col.label }}</span>
                  </label>
                </div>
              </div>

              <div class="form-group">
                <label class="form-label">Task size unit</label>
                <p class="form-hint">Estimate tasks in hours or days.</p>
                <div class="task-size-unit-options">
                  <label class="unit-option">
                    <input type="radio" v-model="newProject.taskSizeUnit" value="hours" />
                    <span>Hours</span>
                  </label>
                  <label class="unit-option">
                    <input type="radio" v-model="newProject.taskSizeUnit" value="days" />
                    <span>Days</span>
                  </label>
                </div>
              </div>

              <div class="form-actions">
                <button type="button" @click="closeNewProjectForm" class="btn btn-secondary">
                  Cancel
                </button>
                <button type="submit" class="btn btn-primary">
                  Create Project
                </button>
              </div>
            </form>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Project Details & Comments Modal -->
    <Teleport to="body">
      <Transition name="modal">
        <div v-if="projectDetailsProject" class="modal-overlay" @click.self="closeProjectDetails">
          <div class="modal-content project-details-modal">
            <div class="modal-header">
              <div class="project-details-title-row">
                <div class="project-details-color" :style="{ backgroundColor: projectDetailsProject.color }"></div>
                <h3 class="modal-title">{{ projectDetailsProject.name }}</h3>
              </div>
              <button @click="closeProjectDetails" class="modal-close">
                <X class="close-icon" />
              </button>
            </div>
            <div class="project-details-body">
              <p v-if="projectDetailsProject.description" class="project-details-desc">{{ projectDetailsProject.description }}</p>
              <div class="project-details-dates">
                <span v-if="projectDetailsProject.startDate">
                  Start: {{ formatProjectDate(projectDetailsProject.startDate) }}
                </span>
                <span v-if="projectDetailsProject.endDate">
                  End: {{ formatProjectDate(projectDetailsProject.endDate) }}
                </span>
              </div>
              <CommentList
                v-if="authStore.user"
                :project-id="projectDetailsProject.id"
                :author-id="(authStore.user as { id: number }).id"
              />
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Delete Project Confirmation Dialog -->
    <ConfirmDialog
      :is-open="deleteDialogOpen"
      title="Delete Project"
      :message="`Are you sure you want to delete '${projectToDelete?.name}'?`"
      description="This will permanently delete all tasks and sprints in this project. This action cannot be undone."
      confirm-text="Delete Project"
      cancel-text="Cancel"
      variant="danger"
      :loading="deleteDialogLoading"
      @confirm="executeDeleteProject"
      @cancel="cancelDeleteProject"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useProjectsStore } from '@/stores/projects'
import { useTasksStore } from '@/stores/tasks'
import { useSprintsStore } from '@/stores/sprints'
import { useAuthStore } from '@/stores/auth'
import { ChevronDown, Plus, Check, X, FolderPlus, Users, Trash2 } from 'lucide-vue-next'
import ConfirmDialog from '@/components/ui/ConfirmDialog.vue'
import { getInitials, getUserColor } from '@/utils/initials'
import CommentList from '@/features/dashboard/components/CommentList.vue'
import { SPRINT_DURATION_OPTIONS, COLUMN_OPTIONS } from '@/types'
import type { TaskStatus } from '@/types'

const projectsStore = useProjectsStore()
const tasksStore = useTasksStore()
const sprintsStore = useSprintsStore()
const authStore = useAuthStore()

const isOpen = ref(false)
const showNewProjectForm = ref(false)
const visibilityPopoverProjectId = ref<number | null>(null)
const projectDetailsProject = ref<{ id: number; name: string; description?: string; color: string; startDate?: Date; endDate?: Date } | null>(null)

// Delete confirmation dialog state
const deleteDialogOpen = ref(false)
const deleteDialogLoading = ref(false)
const projectToDelete = ref<{ id: number; name: string } | null>(null)

const projectColors = [
  '#B05A36', '#4F46E5', '#059669', '#DC2626',
  '#D97706', '#7C3AED', '#0891B2', '#DB2777'
]

const sprintDurationOptions = SPRINT_DURATION_OPTIONS
const columnOptions = COLUMN_OPTIONS

const defaultVisibleColumns: TaskStatus[] = ['todo', 'in-progress', 'completed']

const newProject = ref<{
  name: string
  description: string
  color: string
  startDate: string
  endDate: string
  visibleToUserIds: number[]
  sprintDurationDays: number
  visibleColumns: TaskStatus[]
  taskSizeUnit: 'hours' | 'days'
}>({
  name: '',
  description: '',
  color: projectColors[0] as string,
  startDate: '',
  endDate: '',
  visibleToUserIds: [],
  sprintDurationDays: 14,
  visibleColumns: [...defaultVisibleColumns],
  taskSizeUnit: 'hours'
})

const currentUserId = computed(() => (authStore.user as { id: number } | null)?.id ?? 0)
const visibleProjects = computed(() => projectsStore.getVisibleProjects(currentUserId.value))
const currentProjectId = computed(() => projectsStore.currentProjectId)
const currentProject = computed(() => projectsStore.currentProject)

function canEditVisibility(project: { ownerId: number }): boolean {
  return project.ownerId === currentUserId.value
}

function isProjectOwner(project: { ownerId: number }): boolean {
  return project.ownerId === currentUserId.value
}

function confirmDeleteProject(project: { id: number; name: string }): void {
  projectToDelete.value = project
  deleteDialogOpen.value = true
}

function cancelDeleteProject(): void {
  deleteDialogOpen.value = false
  projectToDelete.value = null
}

async function executeDeleteProject(): Promise<void> {
  if (!projectToDelete.value) return

  deleteDialogLoading.value = true
  try {
    const result = await projectsStore.deleteProject(projectToDelete.value.id, currentUserId.value)
    if (!result.success) {
      alert(result.message || 'Failed to delete project')
      return
    }

    // Close the dialog
    deleteDialogOpen.value = false
    projectToDelete.value = null

    // If we deleted the current project, the store will switch to another one
    // Refresh the view
    if (projectsStore.currentProject) {
      await sprintsStore.fetchSprints(projectsStore.currentProjectId)
      sprintsStore.initializeSprints(projectsStore.currentProjectId)
      await tasksStore.fetchTasks({
        projectId: projectsStore.currentProjectId,
        sprintId: sprintsStore.currentSprintId ?? undefined
      })
    }
  } finally {
    deleteDialogLoading.value = false
  }
}

function visibilityButtonTitle(project: { visibleToUserIds?: number[] }): string {
  if (!project.visibleToUserIds || project.visibleToUserIds.length === 0) {
    return 'Visible to everyone – click to restrict'
  }
  return `Visible to ${project.visibleToUserIds.length} user(s) – click to edit`
}

function isUserVisible(project: { visibleToUserIds?: number[] }, userId: number): boolean {
  const ids = project.visibleToUserIds
  if (!ids || ids.length === 0) return true
  return ids.includes(userId)
}

async function toggleUserVisibility(project: { id: number; visibleToUserIds?: number[] }, userId: number): Promise<void> {
  const ids = project.visibleToUserIds ?? [...authStore.users.map((u: { id: number }) => u.id)]
  const next = ids.includes(userId)
    ? ids.filter(id => id !== userId)
    : [...ids, userId]
  await projectsStore.updateProject(project.id, {
    visibleToUserIds: next.length === 0 ? undefined : next
  })
}

function openVisibilityPopover(project: { id: number }): void {
  visibilityPopoverProjectId.value = visibilityPopoverProjectId.value === project.id ? null : project.id
}

function openProjectDetails(project: { id: number; name: string; description?: string; color: string; startDate?: Date; endDate?: Date }): void {
  projectDetailsProject.value = project
}

function closeProjectDetails(): void {
  projectDetailsProject.value = null
}

function formatProjectDate(d: Date | string): string {
  const date = typeof d === 'string' ? new Date(d) : d
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function closeVisibilityPopover(): void {
  visibilityPopoverProjectId.value = null
}

function getProjectTaskCount(projectId: number): number {
  return tasksStore.getTasksByProject(projectId).length
}

async function toggleDropdown(): Promise<void> {
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    if (!projectsStore.loaded) await projectsStore.fetchProjects()
    const visible = projectsStore.getVisibleProjects(currentUserId.value)
    const currentVisible = visible.some(p => p.id === projectsStore.currentProjectId)
    const first = visible[0]
    if (!currentVisible && first) {
      await selectProject(first.id)
    }
  } else {
    closeVisibilityPopover()
  }
}

async function selectProject(projectId: number): Promise<void> {
  projectsStore.setCurrentProject(projectId)
  projectsStore.setSwitchToDashboard(true)
  await sprintsStore.fetchSprints(projectId)
  sprintsStore.initializeSprints(projectId)
  await tasksStore.fetchTasks({
    projectId,
    sprintId: sprintsStore.currentSprintId ?? undefined
  })
  isOpen.value = false
}

function closeNewProjectForm(): void {
  showNewProjectForm.value = false
  newProject.value = {
    name: '',
    description: '',
    color: projectColors[0] as string,
    startDate: '',
    endDate: '',
    visibleToUserIds: [],
    sprintDurationDays: 14,
    visibleColumns: [...defaultVisibleColumns],
    taskSizeUnit: 'hours'
  }
}

async function createProject(): Promise<void> {
  if (!newProject.value.name.trim()) return
  const project = await projectsStore.addProject({
    name: newProject.value.name.trim(),
    description: newProject.value.description.trim(),
    color: newProject.value.color,
    ownerId: authStore.user?.id || 1,
    startDate: newProject.value.startDate ? new Date(newProject.value.startDate) : undefined,
    endDate: newProject.value.endDate ? new Date(newProject.value.endDate) : undefined,
    visibleToUserIds: newProject.value.visibleToUserIds.length > 0
      ? newProject.value.visibleToUserIds
      : undefined,
    sprintDurationDays: newProject.value.sprintDurationDays,
    visibleColumns: newProject.value.visibleColumns.length > 0
      ? newProject.value.visibleColumns
      : undefined,
    taskSizeUnit: newProject.value.taskSizeUnit
  })
  await selectProject(project.id)
  closeNewProjectForm()
}

// Close dropdown when clicking outside
function handleClickOutside(event: MouseEvent): void {
  const target = event.target as HTMLElement
  if (!target.closest('.project-selector')) {
    isOpen.value = false
    closeVisibilityPopover()
  }
  // Close visibility popover when clicking outside it (but inside project-selector)
  if (visibilityPopoverProjectId.value !== null && !target.closest('.visibility-popover') && !target.closest('.visibility-btn')) {
    closeVisibilityPopover()
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
.project-selector {
  position: relative;
}

.project-button {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  padding: 0.5rem 0.75rem;
  min-height: 2.25rem;
  background-color: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 0.5rem;
  color: var(--header-text);
  cursor: pointer;
  transition: all 0.15s ease;
  min-width: 180px;
}

.project-button:hover {
  background-color: rgba(255, 255, 255, 0.15);
}

.project-indicator {
  width: 0.625rem;
  height: 0.625rem;
  border-radius: 50%;
  flex-shrink: 0;
}

.project-name {
  flex: 1;
  text-align: left;
  font-size: 0.875rem;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.chevron-icon {
  width: 1rem;
  height: 1rem;
  opacity: 0.7;
  transition: transform 0.2s ease;
  flex-shrink: 0;
}

.chevron-icon.rotate {
  transform: rotate(180deg);
}

.dropdown-menu {
  position: absolute;
  top: calc(100% + 0.5rem);
  left: 0;
  min-width: 280px;
  background-color: var(--card-bg);
  border: 1px solid var(--border-primary);
  border-radius: 0.75rem;
  box-shadow: var(--shadow-lg);
  z-index: 100;
  overflow: hidden;
}

.dropdown-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--border-primary);
}

.dropdown-title {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.add-project-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.5rem;
  height: 1.5rem;
  background-color: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: 0.375rem;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all 0.15s ease;
}

.add-project-btn:hover {
  background-color: var(--color-brown-500);
  border-color: var(--color-brown-500);
  color: white;
}

.add-icon {
  width: 0.875rem;
  height: 0.875rem;
}

.project-list {
  padding: 0.5rem;
  max-height: 300px;
  overflow-y: auto;
}

.project-row {
  display: flex;
  align-items: center;
  gap: 0;
  position: relative;
}

.project-row .project-item {
  flex: 1;
  min-width: 0;
}

.project-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.625rem 0.75rem;
  background: transparent;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.15s ease;
  text-align: left;
}

.visibility-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: 0.375rem;
  color: var(--text-tertiary);
  cursor: pointer;
  transition: all 0.15s ease;
  flex-shrink: 0;
}

.visibility-btn:hover {
  background-color: var(--bg-hover);
  color: var(--color-brown-500);
}

.visibility-btn.delete-btn:hover {
  background-color: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.visibility-icon {
  width: 1rem;
  height: 1rem;
}

.visibility-popover {
  position: absolute;
  right: 0;
  top: 100%;
  margin-top: 0.25rem;
  min-width: 220px;
  background-color: var(--card-bg);
  border: 1px solid var(--border-primary);
  border-radius: 0.5rem;
  box-shadow: var(--shadow-lg);
  z-index: 110;
  overflow: hidden;
}

.visibility-popover-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem 0.75rem;
  border-bottom: 1px solid var(--border-primary);
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.popover-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.25rem;
  height: 1.25rem;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: 0.25rem;
  color: var(--text-muted);
  cursor: pointer;
  transition: all 0.15s ease;
}

.popover-close:hover {
  background-color: var(--bg-hover);
  color: var(--text-primary);
}

.popover-close-icon {
  width: 0.875rem;
  height: 0.875rem;
}

.visibility-options {
  padding: 0.5rem 0.75rem;
  max-height: 200px;
  overflow-y: auto;
}

.visibility-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0;
  cursor: pointer;
  font-size: 0.8125rem;
  color: var(--text-primary);
}

.visibility-option input {
  accent-color: var(--color-brown-500);
}

.visibility-option-avatar {
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

.visibility-option-name {
  flex: 1;
}

.visibility-hint {
  padding: 0.5rem 0.75rem;
  font-size: 0.6875rem;
  color: var(--text-muted);
  border-top: 1px solid var(--border-primary);
  margin: 0;
}

.project-item:hover {
  background-color: var(--bg-hover);
}

.project-item.active {
  background-color: var(--bg-tertiary);
}

.project-color {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 0.25rem;
  flex-shrink: 0;
}

.project-info {
  flex: 1;
  min-width: 0;
}

.project-item-name {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.project-task-count {
  font-size: 0.75rem;
  color: var(--text-tertiary);
}

.check-icon {
  width: 1rem;
  height: 1rem;
  color: var(--color-brown-500);
  flex-shrink: 0;
}

/* Modal styles */
.modal-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background-color: var(--card-bg);
  border-radius: 0.75rem;
  box-shadow: var(--shadow-xl);
  width: 100%;
  max-width: 480px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid var(--border-primary);
}

.modal-title {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
}

.modal-title-icon {
  width: 1.25rem;
  height: 1.25rem;
  color: var(--color-brown-500);
}

.modal-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background: transparent;
  border: none;
  border-radius: 0.5rem;
  color: var(--text-tertiary);
  cursor: pointer;
  transition: all 0.15s ease;
}

.modal-close:hover {
  background-color: var(--bg-hover);
  color: var(--text-primary);
}

.close-icon {
  width: 1.125rem;
  height: 1.125rem;
}

.project-form {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.25rem;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-secondary);
  margin-bottom: 0.5rem;
}

.form-hint {
  font-size: 0.75rem;
  color: var(--text-muted);
  margin: -0.25rem 0 0.5rem;
}

.visibility-create-options {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.visibility-create-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem;
  border-radius: 0.5rem;
  cursor: pointer;
  font-size: 0.875rem;
  color: var(--text-primary);
  transition: background-color 0.15s ease;
}

.visibility-create-option:hover {
  background-color: var(--bg-tertiary);
}

.visibility-create-option input {
  accent-color: var(--color-brown-500);
}

.column-options {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.task-size-unit-options {
  display: flex;
  gap: 1.5rem;
}

.unit-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.875rem;
  color: var(--text-primary);
}

.unit-option input {
  accent-color: var(--color-brown-500);
}

.color-options {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.color-option {
  width: 2rem;
  height: 2rem;
  border: 2px solid transparent;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s ease;
}

.color-option:hover {
  transform: scale(1.1);
}

.color-option.selected {
  border-color: var(--text-primary);
  box-shadow: 0 0 0 2px var(--card-bg);
}

.color-check {
  width: 1rem;
  height: 1rem;
  color: white;
}

.form-row {
  display: flex;
  gap: 1rem;
}

.form-row .form-group {
  flex: 1;
}

.form-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 0.5rem;
}

/* Project details modal */
.project-details-modal {
  max-width: 520px;
}

.project-details-title-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.project-details-color {
  width: 1rem;
  height: 1rem;
  border-radius: 0.25rem;
  flex-shrink: 0;
}

.project-details-body {
  padding: 1.25rem 1.5rem;
}

.project-details-desc {
  font-size: 0.9375rem;
  color: var(--text-secondary);
  line-height: 1.5;
  margin: 0 0 1rem;
}

.project-details-dates {
  display: flex;
  gap: 1.5rem;
  font-size: 0.8125rem;
  color: var(--text-tertiary);
  margin-bottom: 0.5rem;
}

/* Transitions */
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

.modal-enter-active,
.modal-leave-active {
  transition: all 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .modal-content,
.modal-leave-to .modal-content {
  transform: scale(0.95);
}
</style>
