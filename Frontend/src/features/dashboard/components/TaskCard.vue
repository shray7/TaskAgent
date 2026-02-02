<template>
  <div 
    class="task-card card"
    :draggable="draggable && !editingField"
    @dragstart="$emit('dragstart', $event)"
    @dragend="$emit('dragend', $event)"
  >
    <div class="task-header">
      <!-- Editable Title -->
      <h3 
        v-if="editingField !== 'title'" 
        @click="startEditing('title')"
        class="task-title editable"
        title="Click to edit"
      >
        {{ task.title }}
      </h3>
      <input 
        v-else
        ref="titleInputRef"
        v-model="editValues.title"
        @blur="saveField('title')"
        @keydown.enter="saveField('title')"
        @keydown.escape="cancelEditing"
        class="inline-input title-input"
        type="text"
        placeholder="Task title"
      />
      
      <span :class="['badge', priorityBadgeClass]">
        {{ task.priority }}
      </span>
    </div>

    <p class="task-description">{{ task.description }}</p>

    <div class="task-meta">
      <div class="meta-item">
        <User class="meta-icon" />
        <!-- Editable Assignee -->
        <span 
          v-if="editingField !== 'assignee'"
          @click="startEditing('assignee')"
          class="editable"
          title="Click to change assignee"
        >
          {{ assigneeName }}
        </span>
        <select 
          v-else
          ref="assigneeSelectRef"
          v-model="editValues.assigneeId"
          @change="saveField('assignee')"
          @blur="cancelEditing"
          class="inline-select"
        >
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
      <div class="meta-item">
        <Layers class="meta-icon" />
        <!-- Editable Sprint -->
        <span
          v-if="editingField !== 'sprint'"
          @click="startEditing('sprint')"
          class="editable"
          title="Click to move to another sprint"
        >
          {{ sprintName }}
        </span>
        <select
          v-else
          ref="sprintSelectRef"
          v-model="editValues.sprintId"
          @change="saveField('sprint')"
          @blur="cancelEditing"
          class="inline-select"
        >
          <option value="">Backlog</option>
          <option
            v-for="sprint in availableSprints"
            :key="sprint.id"
            :value="sprint.id"
          >
            {{ sprint.name }} ({{ sprint.status }})
          </option>
        </select>
      </div>
      <div class="meta-item">
        <Calendar class="meta-icon" />
        <!-- Editable Due Date -->
        <span 
          v-if="editingField !== 'dueDate'"
          @click="startEditing('dueDate')"
          class="editable"
          title="Click to change due date"
        >
          {{ formattedDueDate }}
        </span>
        <input 
          v-else
          ref="dateInputRef"
          v-model="editValues.dueDate"
          @change="saveField('dueDate')"
          @blur="cancelEditing"
          @keydown.escape="cancelEditing"
          type="date"
          class="inline-input date-input"
        />
      </div>
      <div v-if="formattedSize" class="meta-item meta-size">
        <Clock class="meta-icon" />
        <span class="task-size-badge">{{ formattedSize }}</span>
      </div>
    </div>

    <div v-if="task.tags && task.tags.length" class="task-tags">
      <span v-for="tag in task.tags" :key="tag" class="tag">
        {{ tag }}
      </span>
    </div>

    <div class="task-footer">
      <select
        :value="task.status"
        @change="$emit('status-change', task.id, ($event.target as HTMLSelectElement).value)"
        class="select status-select"
      >
        <option value="todo">To Do</option>
        <option value="in-progress">In Progress</option>
        <option value="completed">Completed</option>
      </select>

      <div class="task-actions">
        <button
          @click="$emit('edit', task)"
          class="action-btn"
          title="Edit all fields"
        >
          <Edit2 class="action-icon" />
        </button>
        <button
          @click="$emit('delete', task.id)"
          class="action-btn action-btn-danger"
          title="Delete task"
        >
          <Trash2 class="action-icon" />
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue'
import { Trash2, User, Calendar, Edit2, Clock, Layers } from 'lucide-vue-next'
import { getInitials, getUserColor } from '@/utils/initials'
import { useAuthStore } from '@/stores/auth'
import { useSprintsStore } from '@/stores/sprints'

interface Task {
  id: number
  title: string
  description: string
  status: 'todo' | 'in-progress' | 'completed'
  priority: 'low' | 'medium' | 'high'
  assigneeId: number
  projectId: number
  sprintId?: number
  dueDate?: Date | string
  tags?: string[]
  size?: number
}

const props = defineProps<{
  task: Task
  draggable?: boolean
  /** Unit for task size (hours or days). When set, size is shown on the card. */
  taskSizeUnit?: 'hours' | 'days'
}>()

const formattedSize = computed(() => {
  const size = props.task.size
  if (size == null || size <= 0) return ''
  const unit = props.taskSizeUnit ?? 'hours'
  return unit === 'hours' ? `${size}h` : `${size}d`
})

const emit = defineEmits<{
  delete: [id: number]
  'status-change': [id: number, status: string]
  edit: [task: Task]
  update: [id: number, updates: Partial<Task>]
  dragstart: [event: DragEvent]
  dragend: [event: DragEvent]
}>()

const authStore = useAuthStore()
const sprintsStore = useSprintsStore()

// Inline editing state
type EditableField = 'title' | 'assignee' | 'dueDate' | 'sprint'
const editingField = ref<EditableField | null>(null)
const editValues = ref({
  title: '',
  assigneeId: 0,
  dueDate: '',
  sprintId: '' as number | string
})

// Template refs for auto-focus
const titleInputRef = ref<HTMLInputElement | null>(null)
const assigneeSelectRef = ref<HTMLSelectElement | null>(null)
const dateInputRef = ref<HTMLInputElement | null>(null)
const sprintSelectRef = ref<HTMLSelectElement | null>(null)

// Sprints for the task's project
const availableSprints = computed(() =>
  sprintsStore.getSprintsByProject(props.task.projectId)
)

const sprintName = computed(() => {
  const sid = props.task.sprintId
  if (!sid) return 'Backlog'
  const s = sprintsStore.getSprintById(sid)
  return s ? s.name : 'Backlog'
})

// Computed properties
const assigneeName = computed(() => {
  const list = Array.isArray(authStore.users) ? authStore.users : (authStore.users as { value: { id: number; name: string }[] }).value
  const assignee = list.find((u) => u.id === props.task.assigneeId)
  return assignee ? assignee.name : 'Unassigned'
})

const formattedDueDate = computed(() => {
  if (!props.task.dueDate) return 'No due date'
  return new Date(props.task.dueDate).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric'
  })
})

const priorityBadgeClass = computed(() => {
  switch (props.task.priority) {
    case 'high':
      return 'badge-red'
    case 'medium':
      return 'badge-amber'
    case 'low':
      return 'badge-green'
    default:
      return 'badge-gray'
  }
})

// Format date for input[type="date"]
function formatDateForInput(date: Date | string | undefined): string {
  if (!date) return ''
  const d = new Date(date)
  const iso = d.toISOString().split('T')[0]
  return iso ?? ''
}

// Inline editing functions
function startEditing(field: EditableField) {
  editingField.value = field
  
  // Initialize edit values with current task values
  editValues.value = {
    title: props.task.title,
    assigneeId: props.task.assigneeId,
    dueDate: formatDateForInput(props.task.dueDate),
    sprintId: props.task.sprintId ?? ''
  }
  
  // Auto-focus the input after DOM update
  nextTick(() => {
    switch (field) {
      case 'title':
        titleInputRef.value?.focus()
        titleInputRef.value?.select()
        break
      case 'assignee':
        assigneeSelectRef.value?.focus()
        break
      case 'dueDate':
        dateInputRef.value?.focus()
        break
      case 'sprint':
        sprintSelectRef.value?.focus()
        break
    }
  })
}

function saveField(field: EditableField) {
  if (!editingField.value) return
  
  const updates: Partial<Task> = {}
  
  switch (field) {
    case 'title':
      if (editValues.value.title.trim() && editValues.value.title !== props.task.title) {
        updates.title = editValues.value.title.trim()
      }
      break
    case 'assignee': {
      const raw = String(editValues.value.assigneeId ?? '')
      const assigneeId = raw === '' || raw === '0' ? 0 : Number(raw)
      if (!Number.isNaN(assigneeId) && assigneeId !== (props.task.assigneeId ?? 0)) {
        updates.assigneeId = assigneeId
      }
      break
    }
    case 'dueDate':
      const newDate = editValues.value.dueDate ? new Date(editValues.value.dueDate) : undefined
      const oldDate = props.task.dueDate ? formatDateForInput(props.task.dueDate) : ''
      if (editValues.value.dueDate !== oldDate) {
        updates.dueDate = newDate
      }
      break
    case 'sprint': {
      // Backlog = '' → send 0 so backend sets sprintId to null
      const raw = editValues.value.sprintId
      const newSprintId = raw === '' ? 0 : Number(raw)
      const oldVal = props.task.sprintId ?? 0
      if (newSprintId !== oldVal && !Number.isNaN(newSprintId)) {
        updates.sprintId = newSprintId
      }
      break
    }
  }
  
  // Only emit if there are actual changes
  if (Object.keys(updates).length > 0) {
    emit('update', props.task.id, updates)
  }
  
  editingField.value = null
}

function cancelEditing() {
  editingField.value = null
}
</script>

<style scoped>
.task-card {
  padding: 1rem;
  transition: box-shadow 0.15s ease, transform 0.15s ease;
}

.task-card:hover {
  box-shadow: var(--shadow-md);
}

.task-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.5rem;
}

.task-title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--text-primary);
  line-height: 1.4;
  letter-spacing: -0.01em;
}

.task-description {
  color: var(--text-secondary);
  font-size: 0.8125rem;
  line-height: 1.5;
  margin-bottom: 0.75rem;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.task-meta {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.75rem;
  color: var(--text-tertiary);
}

.meta-icon {
  width: 0.875rem;
  height: 0.875rem;
  color: var(--text-muted);
}

.meta-size {
  flex-shrink: 0;
}

.task-size-badge {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-secondary);
  background-color: var(--bg-tertiary);
  padding: 0.125rem 0.375rem;
  border-radius: 0.25rem;
  flex-shrink: 0;
}

.task-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  margin-bottom: 0.75rem;
}

.tag {
  display: inline-flex;
  align-items: center;
  padding: 0.125rem 0.5rem;
  font-size: 0.6875rem;
  font-weight: 500;
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: 9999px;
}

.task-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 0.75rem;
  border-top: 1px solid var(--border-primary);
}

.status-select {
  width: auto;
  padding: 0.375rem 2rem 0.375rem 0.625rem;
  font-size: 0.8125rem;
  border-radius: 9999px;
  background-position: right 0.5rem center;
}

.task-actions {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.375rem;
  border: none;
  background: transparent;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.15s ease;
  color: var(--text-muted);
}

.action-btn:hover {
  background-color: var(--bg-hover);
  color: var(--text-secondary);
}

.action-btn-danger:hover {
  background-color: var(--badge-red-bg);
  color: var(--badge-red-text);
}

.action-icon {
  width: 0.875rem;
  height: 0.875rem;
}

/* Editable field styles */
.editable {
  cursor: pointer;
  border-radius: 0.25rem;
  padding: 0.125rem 0.25rem;
  margin: -0.125rem -0.25rem;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.editable:hover {
  background-color: var(--bg-hover);
  color: var(--text-primary);
}

.task-title.editable {
  padding: 0.125rem 0.375rem;
  margin: -0.125rem -0.375rem;
}

/* Inline input styles */
.inline-input {
  font-family: inherit;
  background-color: var(--input-bg);
  border: 1px solid var(--input-focus-border);
  border-radius: 0.375rem;
  color: var(--text-primary);
  outline: none;
  box-shadow: 0 0 0 2px var(--input-focus-ring);
}

.title-input {
  flex: 1;
  font-size: 0.9375rem;
  font-weight: 600;
  padding: 0.25rem 0.5rem;
  min-width: 0;
}

.inline-select {
  font-family: inherit;
  font-size: 0.75rem;
  padding: 0.125rem 0.375rem;
  background-color: var(--input-bg);
  border: 1px solid var(--input-focus-border);
  border-radius: 0.375rem;
  color: var(--text-primary);
  outline: none;
  box-shadow: 0 0 0 2px var(--input-focus-ring);
  cursor: pointer;
  max-width: 140px;
}

.date-input {
  font-size: 0.75rem;
  padding: 0.125rem 0.375rem;
  max-width: 130px;
}
</style>
