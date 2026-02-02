<template>
  <div class="sprint-selector">
    <button @click="toggleDropdown" class="sprint-button">
      <div :class="['sprint-status-indicator', currentSprintVal ? `status-${currentSprintVal.status}` : 'status-none']"></div>
      <div class="sprint-button-content">
        <span class="sprint-name">
          {{ currentSprintVal ? `${currentSprintVal.name} · ${formatDateRange(currentSprintVal.startDate, currentSprintVal.endDate)}` : 'All Tasks' }}
        </span>
      </div>
      <ChevronDown class="chevron-icon" :class="{ 'rotate': isOpen }" />
    </button>

    <Transition name="dropdown">
      <div v-if="isOpen" class="dropdown-menu">
        <div class="dropdown-header">
          <span class="dropdown-title">Sprints (2-week cycles)</span>
          <button @click="showNewSprintForm = true" class="add-sprint-btn" title="New Sprint">
            <Plus class="add-icon" />
          </button>
        </div>

        <div class="sprint-list">
          <!-- All Tasks option -->
          <button
            @click="selectSprint(null)"
            :class="['sprint-item', { active: currentSprintIdVal === null }]"
          >
            <div class="sprint-status-dot status-none"></div>
            <div class="sprint-info">
              <span class="sprint-item-name">All Tasks</span>
              <span class="sprint-task-count">{{ getAllTasksCount() }} tasks</span>
            </div>
            <Check v-if="currentSprintIdVal === null" class="check-icon" />
          </button>

          <!-- Active Sprint -->
          <div v-if="activeSprint" class="sprint-group">
            <span class="group-label">Active Sprint</span>
            <button
              @click="selectSprint(activeSprint.id)"
              :class="['sprint-item', { active: activeSprint.id === currentSprintIdVal }]"
            >
              <div class="sprint-status-dot status-active"></div>
              <div class="sprint-info">
                <span class="sprint-item-name">{{ activeSprint.name }}</span>
                <span class="sprint-dates-small">{{ formatDateRange(activeSprint.startDate, activeSprint.endDate) }}</span>
                <div class="sprint-progress">
                  <div class="progress-bar">
                    <div 
                      class="progress-fill" 
                      :style="{ width: getSprintProgress(activeSprint.id).percentComplete + '%' }"
                    ></div>
                  </div>
                  <span class="progress-text">{{ getSprintProgress(activeSprint.id).daysRemaining }} days left</span>
                </div>
              </div>
              <Check v-if="activeSprint.id === currentSprintIdVal" class="check-icon" />
            </button>
          </div>

          <!-- Planning Sprints -->
          <div v-if="planningSprints.length > 0" class="sprint-group">
            <span class="group-label">Planning</span>
            <button
              v-for="sprint in planningSprints"
              :key="sprint.id"
              @click="selectSprint(sprint.id)"
              :class="['sprint-item', { active: sprint.id === currentSprintIdVal }]"
            >
              <div class="sprint-status-dot status-planning"></div>
              <div class="sprint-info">
                <span class="sprint-item-name">{{ sprint.name }}</span>
                <span class="sprint-dates-small">{{ formatDateRange(sprint.startDate, sprint.endDate) }}</span>
                <span class="sprint-task-count">{{ getSprintTaskCount(sprint.id) }} tasks planned</span>
              </div>
              <div class="sprint-actions">
                <button 
                  @click.stop="startSprintAction(sprint.id)" 
                  class="action-btn start-btn"
                  title="Start Sprint"
                >
                  <Play class="action-icon" />
                </button>
                <Check v-if="sprint.id === currentSprintIdVal" class="check-icon" />
              </div>
            </button>
          </div>

          <!-- Completed Sprints -->
          <div v-if="completedSprints.length > 0" class="sprint-group">
            <span class="group-label">Completed</span>
            <button
              v-for="sprint in completedSprints"
              :key="sprint.id"
              @click="selectSprint(sprint.id)"
              :class="['sprint-item', { active: sprint.id === currentSprintIdVal }]"
            >
              <div class="sprint-status-dot status-completed"></div>
              <div class="sprint-info">
                <span class="sprint-item-name">{{ sprint.name }}</span>
                <span class="sprint-dates-small">{{ formatDateRange(sprint.startDate, sprint.endDate) }}</span>
                <span class="sprint-task-count">{{ getSprintTaskCount(sprint.id) }} tasks</span>
              </div>
              <Check v-if="sprint.id === currentSprintIdVal" class="check-icon" />
            </button>
          </div>

          <!-- Backlog -->
          <div class="sprint-group">
            <span class="group-label">Backlog</span>
            <button
              @click="selectBacklog"
              :class="['sprint-item backlog-item', { active: isBacklogSelected }]"
            >
              <Archive class="backlog-icon" />
              <div class="sprint-info">
                <span class="sprint-item-name">Product Backlog</span>
                <span class="sprint-task-count">{{ backlogTaskCount }} unassigned tasks</span>
              </div>
              <Check v-if="isBacklogSelected" class="check-icon" />
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- New Sprint Modal -->
    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showNewSprintForm" class="modal-overlay" @click.self="closeNewSprintForm">
          <div class="modal-content">
            <div class="modal-header">
              <h3 class="modal-title">
                <Calendar class="modal-title-icon" />
                Create New Sprint
              </h3>
              <button @click="closeNewSprintForm" class="modal-close">
                <X class="close-icon" />
              </button>
            </div>

            <form @submit.prevent="createSprint" class="sprint-form">
              <div class="form-group">
                <label class="form-label">Sprint Name</label>
                <input
                  v-model="newSprint.name"
                  type="text"
                  class="input"
                  :placeholder="`Sprint ${nextSprintNumber}`"
                  required
                />
              </div>

              <div class="form-group">
                <label class="form-label">Sprint Goal</label>
                <textarea
                  v-model="newSprint.goal"
                  class="textarea"
                  placeholder="What do you want to achieve in this sprint?"
                  rows="3"
                ></textarea>
              </div>

              <div class="form-group">
                <label class="form-label">Start Date</label>
                <input
                  v-model="newSprint.startDate"
                  type="date"
                  class="input"
                  required
                />
              </div>

              <div class="sprint-duration-info">
                <Clock class="duration-icon" />
                <span>Sprint duration: {{ projectsStore.getSprintDurationDays(projectsStore.currentProjectId) }} days</span>
              </div>

              <div class="form-group">
                <label class="form-label">End Date (auto-calculated)</label>
                <input
                  :value="calculatedEndDate"
                  type="date"
                  class="input"
                  disabled
                />
              </div>

              <div class="form-actions">
                <button type="button" @click="closeNewSprintForm" class="btn btn-secondary">
                  Cancel
                </button>
                <button type="submit" class="btn btn-primary">
                  Create Sprint
                </button>
              </div>
            </form>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useSprintsStore } from '@/stores/sprints'
import { useProjectsStore } from '@/stores/projects'
import { useTasksStore } from '@/stores/tasks'
import { sprintDurationMs } from '@/types'
import { ChevronDown, Plus, Check, X, Calendar, Clock, Play, Archive } from 'lucide-vue-next'

const sprintsStore = useSprintsStore()
const projectsStore = useProjectsStore()
const tasksStore = useTasksStore()

const isOpen = ref(false)
const showNewSprintForm = ref(false)
const isBacklogSelected = ref(false)

const today = new Date()
const todayStr = today.toISOString().split('T')[0]

const newSprint = ref({
  name: '',
  goal: '',
  startDate: todayStr
})

const projectSprints = computed(() => 
  sprintsStore.getSprintsByProject(projectsStore.currentProjectId)
)

const activeSprint = computed(() => 
  projectSprints.value.find(s => s.status === 'active')
)

const planningSprints = computed(() => 
  projectSprints.value.filter(s => s.status === 'planning')
)

const completedSprints = computed(() => 
  projectSprints.value.filter(s => s.status === 'completed').slice(-3) // Show last 3
)

const currentSprintIdVal = computed(() => sprintsStore.currentSprintId)
const currentSprintVal = computed(() => sprintsStore.currentSprint)

const nextSprintNumber = computed(() => {
  const sprints = projectSprints.value
  return sprints.length + 1
})

const calculatedEndDate = computed(() => {
  if (!newSprint.value.startDate) return ''
  const start = new Date(newSprint.value.startDate)
  const durationDays = projectsStore.getSprintDurationDays(projectsStore.currentProjectId)
  const end = new Date(start.getTime() + sprintDurationMs(durationDays))
  return end.toISOString().split('T')[0]
})

const backlogTaskCount = computed(() => tasksStore.backlogTasks.length)

function formatDateRange(start: Date, end: Date): string {
  const startDate = new Date(start)
  const endDate = new Date(end)
  const options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' }
  return `${startDate.toLocaleDateString('en-US', options)} - ${endDate.toLocaleDateString('en-US', options)}`
}

function getSprintTaskCount(sprintId: number): number {
  return tasksStore.getTasksBySprint(sprintId).length
}

function getAllTasksCount(): number {
  return tasksStore.getTasksByProject(projectsStore.currentProjectId).length
}

function getSprintProgress(sprintId: number) {
  return sprintsStore.getSprintProgress(sprintId)
}

function toggleDropdown(): void {
  isOpen.value = !isOpen.value
}

async function selectSprint(sprintId: number | null): Promise<void> {
  isBacklogSelected.value = false
  sprintsStore.setCurrentSprint(sprintId)
  await tasksStore.fetchTasks({
    projectId: projectsStore.currentProjectId,
    sprintId: sprintId ?? undefined
  })
  isOpen.value = false
}

function selectBacklog(): void {
  isBacklogSelected.value = true
  sprintsStore.setCurrentSprint(null)
  tasksStore.fetchTasks({
    projectId: projectsStore.currentProjectId,
    sprintId: undefined
  })
  isOpen.value = false
}

async function startSprintAction(sprintId: number): Promise<void> {
  await sprintsStore.startSprint(sprintId)
  await selectSprint(sprintId)
}

function closeNewSprintForm(): void {
  showNewSprintForm.value = false
  newSprint.value = {
    name: '',
    goal: '',
    startDate: todayStr
  }
}

async function createSprint(): Promise<void> {
  const name = newSprint.value.name.trim() || `Sprint ${nextSprintNumber.value}`
  const startDateStr = newSprint.value.startDate
  if (!startDateStr) return
  const startDate = new Date(startDateStr)
  const sprint = await sprintsStore.createSprint(
    projectsStore.currentProjectId,
    name,
    newSprint.value.goal.trim(),
    startDate
  )
  await selectSprint(sprint.id)
  closeNewSprintForm()
}

// Close dropdown when clicking outside
function handleClickOutside(event: MouseEvent): void {
  const target = event.target as HTMLElement
  if (!target.closest('.sprint-selector')) {
    isOpen.value = false
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
.sprint-selector {
  position: relative;
}

.sprint-button {
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
  min-width: 200px;
}

.sprint-button:hover {
  background-color: rgba(255, 255, 255, 0.15);
}

.sprint-status-indicator {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  flex-shrink: 0;
}

.sprint-status-indicator.status-active,
.sprint-status-dot.status-active {
  background-color: #10B981;
  box-shadow: 0 0 6px rgba(16, 185, 129, 0.5);
}

.sprint-status-indicator.status-planning,
.sprint-status-dot.status-planning {
  background-color: #F59E0B;
}

.sprint-status-indicator.status-completed,
.sprint-status-dot.status-completed {
  background-color: #6B7280;
}

.sprint-status-indicator.status-none,
.sprint-status-dot.status-none {
  background-color: #9CA3AF;
}

.sprint-button-content {
  flex: 1;
  text-align: left;
  min-width: 0;
  /* Match project selector height: reserve space for name + dates so height doesn't change when toggling */
  min-height: 1.25rem;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.sprint-name {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  line-height: 1.25rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sprint-dates {
  display: block;
  font-size: 0.6875rem;
  line-height: 1rem;
  opacity: 0.7;
  margin-top: 0.125rem;
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
  min-width: 320px;
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

.add-sprint-btn {
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

.add-sprint-btn:hover {
  background-color: var(--color-brown-500);
  border-color: var(--color-brown-500);
  color: white;
}

.add-icon {
  width: 0.875rem;
  height: 0.875rem;
}

.sprint-list {
  padding: 0.5rem;
  max-height: 400px;
  overflow-y: auto;
}

.sprint-group {
  margin-top: 0.75rem;
}

.sprint-group:first-child {
  margin-top: 0;
}

.group-label {
  display: block;
  font-size: 0.6875rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 0.375rem 0.75rem;
}

.sprint-item {
  display: flex;
  align-items: flex-start;
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

.sprint-item:hover {
  background-color: var(--bg-hover);
}

.sprint-item.active {
  background-color: var(--bg-tertiary);
}

.sprint-status-dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  flex-shrink: 0;
  margin-top: 0.375rem;
}

.sprint-info {
  flex: 1;
  min-width: 0;
}

.sprint-item-name {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-primary);
}

.sprint-dates-small {
  display: block;
  font-size: 0.75rem;
  color: var(--text-tertiary);
  margin-top: 0.125rem;
}

.sprint-task-count {
  display: block;
  font-size: 0.6875rem;
  color: var(--text-muted);
  margin-top: 0.25rem;
}

.sprint-progress {
  margin-top: 0.375rem;
}

.progress-bar {
  height: 4px;
  background-color: var(--bg-tertiary);
  border-radius: 2px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background-color: #10B981;
  border-radius: 2px;
  transition: width 0.3s ease;
}

.progress-text {
  font-size: 0.6875rem;
  color: var(--text-muted);
  margin-top: 0.25rem;
  display: block;
}

.sprint-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.action-btn {
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

.action-btn:hover {
  background-color: #10B981;
  border-color: #10B981;
  color: white;
}

.action-icon {
  width: 0.75rem;
  height: 0.75rem;
}

.check-icon {
  width: 1rem;
  height: 1rem;
  color: var(--color-brown-500);
  flex-shrink: 0;
  margin-top: 0.25rem;
}

.backlog-item {
  border-top: 1px dashed var(--border-primary);
  margin-top: 0.5rem;
  padding-top: 0.75rem;
}

.backlog-icon {
  width: 1rem;
  height: 1rem;
  color: var(--text-tertiary);
  flex-shrink: 0;
  margin-top: 0.25rem;
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

.sprint-form {
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

.sprint-duration-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background-color: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: 0.5rem;
  margin-bottom: 1.25rem;
  font-size: 0.8125rem;
  color: var(--text-secondary);
}

.duration-icon {
  width: 1rem;
  height: 1rem;
  color: var(--color-brown-500);
}

.form-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 0.5rem;
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
