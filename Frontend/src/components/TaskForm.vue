<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal card animate-slide-in">
      <div class="modal-header">
        <div class="modal-title-group">
          <div class="modal-icon">
            <Plus v-if="!isEditing" class="modal-icon-svg" />
            <Edit2 v-else class="modal-icon-svg" />
          </div>
          <h2 class="modal-title">
            {{ isEditing ? 'Edit Task' : 'Create Task' }}
          </h2>
        </div>
        <button @click="$emit('close')" class="close-btn" title="Close">
          <X class="close-icon" />
        </button>
      </div>

      <form @submit.prevent="handleSubmit" class="modal-form">
        <div class="form-section">
          <div class="form-group">
            <label class="form-label">Task Title <span class="required">*</span></label>
            <input
              v-model="form.title"
              type="text"
              required
              class="input"
              placeholder="What needs to be done?"
            />
          </div>

          <div class="form-group">
            <label class="form-label">Description</label>
            <textarea
              v-model="form.description"
              rows="3"
              class="input textarea"
              placeholder="Add more details about this task..."
            />
          </div>
        </div>

        <div class="form-section">
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Priority</label>
              <select v-model="form.priority" class="select">
                <option value="low">Low</option>
                <option value="medium">Medium</option>
                <option value="high">High</option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label">Status</label>
              <select v-model="form.status" class="select">
                <option value="todo">To Do</option>
                <option value="in-progress">In Progress</option>
                <option value="completed">Completed</option>
              </select>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Assignee</label>
              <select v-model="form.assigneeId" class="select">
                <option value="0">Unassigned</option>
                <option
                  v-for="user in authStore.users"
                  :key="user.id"
                  :value="user.id"
                >
                  {{ getInitials(user) }} {{ user.name }}
                </option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label">Due Date</label>
              <input v-model="form.dueDate" type="date" class="input" />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Size ({{ taskSizeUnit }})</label>
              <input
                v-model.number="form.size"
                type="number"
                min="0"
                step="0.5"
                class="input"
                :placeholder="taskSizeUnit === 'hours' ? 'e.g. 2' : 'e.g. 0.5'"
              />
            </div>
            <div class="form-group">
              <label class="form-label">Project</label>
              <select v-model="form.projectId" class="select">
                <option
                  v-for="project in projectsStore.projects"
                  :key="project.id"
                  :value="project.id"
                >
                  {{ project.name }}
                </option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label">Sprint</label>
              <select v-model="form.sprintId" class="select">
                <option value="">Backlog (no sprint)</option>
                <option
                  v-for="sprint in availableSprints"
                  :key="sprint.id"
                  :value="sprint.id"
                >
                  {{ sprint.name }} ({{ sprint.status }})
                </option>
              </select>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Tags</label>
            <input
              v-model="tagsInput"
              type="text"
              class="input"
              placeholder="design, frontend, urgent (comma separated)"
            />
          </div>

          <CommentList
            v-if="isEditing && props.task?.id && authStore.user"
            :task-id="props.task.id"
            :author-id="(authStore.user as { id: number }).id"
          />
        </div>

        <div class="modal-footer">
          <button type="button" @click="$emit('close')" class="btn btn-secondary">
            Cancel
          </button>
          <button type="submit" :disabled="loading" class="btn btn-primary">
            <span v-if="loading" class="loading-spinner"></span>
            {{ loading ? 'Saving...' : (isEditing ? 'Update Task' : 'Create Task') }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { X, Plus, Edit2 } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import CommentList from './CommentList.vue'
import { getInitials, getUserColor } from '@/utils/initials'
import { useProjectsStore } from '@/stores/projects'
import { useSprintsStore } from '@/stores/sprints'

interface Task {
  id?: number
  title: string
  description: string
  priority: 'low' | 'medium' | 'high'
  status: 'todo' | 'in-progress' | 'completed'
  assigneeId: number | string
  dueDate?: Date | string
  tags?: string[]
  projectId?: number
  sprintId?: number
  size?: number
}

const props = defineProps<{
  task?: Task | null
}>()

const emit = defineEmits<{
  close: []
  submit: [taskData: any]
}>()

const authStore = useAuthStore()
const projectsStore = useProjectsStore()
const sprintsStore = useSprintsStore()
const loading = ref(false)

const isEditing = computed(() => !!props.task)

const form = ref<{
  title: string
  description: string
  priority: 'low' | 'medium' | 'high'
  status: 'todo' | 'in-progress' | 'completed'
  assigneeId: number | string
  dueDate: string
  tags: string[]
  projectId: number
  sprintId: number | string | null
  size: number | undefined
}>({
  title: '',
  description: '',
  priority: 'medium',
  status: 'todo',
  assigneeId: '',
  dueDate: '',
  tags: [],
  projectId: projectsStore.currentProjectId,
  sprintId: sprintsStore.currentSprintId as number | string | null,
  size: undefined
})

const tagsInput = ref('')

const taskSizeUnit = computed(() =>
  projectsStore.getTaskSizeUnit(form.value.projectId)
)

// Get sprints for the selected project
const availableSprints = computed(() => {
  return sprintsStore.getSprintsByProject(form.value.projectId)
})

// Watch for project changes to reset sprint selection
watch(() => form.value.projectId, (newProjectId) => {
  const sprints = sprintsStore.getSprintsByProject(newProjectId)
  const activeSprint = sprints.find(s => s.status === 'active')
  form.value.sprintId = activeSprint?.id || ''
})

watch(() => props.task, (task) => {
  if (task) {
    form.value = {
      title: task.title || '',
      description: task.description || '',
      priority: task.priority || 'medium',
      status: task.status || 'todo',
      assigneeId: task.assigneeId || '',
      dueDate: (task.dueDate ? new Date(task.dueDate).toISOString().split('T')[0] : '') || '',
      tags: task.tags || [],
      projectId: task.projectId || projectsStore.currentProjectId,
      sprintId: task.sprintId ?? '',
      size: task.size
    }
    tagsInput.value = task.tags ? task.tags.join(', ') : ''
  } else {
    form.value = {
      title: '',
      description: '',
      priority: 'medium',
      status: 'todo',
      assigneeId: '',
      dueDate: '',
      tags: [],
      projectId: projectsStore.currentProjectId,
      sprintId: sprintsStore.currentSprintId || '',
      size: undefined
    }
    tagsInput.value = ''
  }
}, { immediate: true })

const handleSubmit = async () => {
  loading.value = true
  
  const taskData = {
    ...form.value,
    assigneeId: form.value.assigneeId && String(form.value.assigneeId) !== '0' ? Number(form.value.assigneeId) : 0,
    tags: tagsInput.value
      .split(',')
      .map(tag => tag.trim())
      .filter(tag => tag.length > 0),
    dueDate: form.value.dueDate ? new Date(form.value.dueDate) : null,
    createdBy: authStore.user?.id || 1,
    projectId: form.value.projectId,
    sprintId: form.value.sprintId ? Number(form.value.sprintId) : undefined,
    size: form.value.size != null && form.value.size > 0 ? form.value.size : undefined
  }

  try {
    emit('submit', taskData)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background-color: var(--overlay-bg);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  z-index: 50;
}

.modal {
  max-width: 32rem;
  width: 100%;
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

.modal-title-group {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.modal-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  background-color: var(--btn-primary-bg);
  border-radius: 0.5rem;
}

.modal-icon-svg {
  width: 1.125rem;
  height: 1.125rem;
  color: var(--btn-primary-text);
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: -0.02em;
}

.close-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  border: none;
  background: transparent;
  border-radius: 0.5rem;
  cursor: pointer;
  color: var(--text-muted);
  transition: all 0.15s ease;
}

.close-btn:hover {
  background-color: var(--bg-hover);
  color: var(--text-primary);
}

.close-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.modal-form {
  padding: 1.5rem;
}

.form-section {
  margin-bottom: 1.5rem;
}

.form-section:last-of-type {
  margin-bottom: 0;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  margin-bottom: 1rem;
}

.form-group:last-child {
  margin-bottom: 0;
}

.form-label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--text-secondary);
}

.required {
  color: var(--badge-red-text);
}

.textarea {
  resize: vertical;
  min-height: 5rem;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--border-primary);
}

.loading-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid transparent;
  border-top-color: currentColor;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@media (max-width: 480px) {
  .form-row {
    grid-template-columns: 1fr;
  }
}
</style>
