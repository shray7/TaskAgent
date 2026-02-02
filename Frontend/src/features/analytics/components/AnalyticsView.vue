<template>
  <div :class="['analytics-view', `analytics-${layout}`]">
    <div class="analytics-grid">
      <!-- Sprint Row: Progress, Burndown Chart, Completion Funnel -->
      <div class="sprint-row">
        <!-- Sprint Progress -->
        <section class="analytics-card card">
          <h3 class="analytics-title">
            <TrendingDown class="analytics-icon" />
            Sprint Progress
          </h3>
          <div class="burndown-content">
            <div class="burndown-stats">
              <div class="burndown-stat">
                <span class="burndown-value">{{ totalSize }}</span>
                <span class="burndown-label">{{ taskSizeUnit }}</span>
              </div>
              <div class="burndown-stat">
                <span class="burndown-value">{{ completedSize }}</span>
                <span class="burndown-label">Completed</span>
              </div>
              <div class="burndown-stat">
                <span class="burndown-value">{{ remainingSize }}</span>
                <span class="burndown-label">Remaining</span>
              </div>
            </div>
            <div class="progress-bar-container">
              <div
                class="progress-bar-fill"
                :style="{ width: `${completionPercent}%` }"
                :class="{ 'progress-complete': completionPercent >= 100 }"
              ></div>
            </div>
            <p class="burndown-hint">{{ completionPercent }}% complete by size</p>
            <div v-if="sprintProgress" class="sprint-days">
              {{ sprintProgress.daysElapsed }} days elapsed • {{ sprintProgress.daysRemaining }} days left
            </div>
          </div>
        </section>

        <!-- Sprint Burndown Chart -->
        <section class="analytics-card card">
          <h3 class="analytics-title">
            <TrendingDown class="analytics-icon" />
            Sprint Burndown
          </h3>
          <div v-if="sprintProgress && burndownData.totalDays > 0" class="burndown-chart-container">
            <svg class="burndown-chart" viewBox="0 0 300 180" preserveAspectRatio="xMidYMid meet">
              <!-- Grid lines -->
              <g class="chart-grid">
                <line v-for="i in 4" :key="'h'+i" 
                  :x1="40" :x2="290" 
                  :y1="20 + (i-1) * 40" :y2="20 + (i-1) * 40" 
                  stroke="var(--border-color)" stroke-dasharray="2,2" />
                <line v-for="i in burndownData.gridLines" :key="'v'+i" 
                  :x1="40 + ((i-1) / (burndownData.gridLines - 1)) * 250" 
                  :x2="40 + ((i-1) / (burndownData.gridLines - 1)) * 250" 
                  :y1="20" :y2="140" 
                  stroke="var(--border-color)" stroke-dasharray="2,2" />
              </g>
              
              <!-- Axes -->
              <line x1="40" y1="140" x2="290" y2="140" stroke="var(--text-tertiary)" stroke-width="1" />
              <line x1="40" y1="20" x2="40" y2="140" stroke="var(--text-tertiary)" stroke-width="1" />
              
              <!-- Ideal burndown line -->
              <line 
                x1="40" y1="20" 
                x2="290" y2="140" 
                stroke="var(--text-tertiary)" 
                stroke-width="2" 
                stroke-dasharray="6,4"
                opacity="0.6"
              />
              
              <!-- Actual burndown line -->
              <polyline 
                :points="burndownData.actualPoints" 
                fill="none" 
                stroke="var(--status-blue)" 
                stroke-width="2.5"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
              
              <!-- Current point marker -->
              <circle 
                :cx="burndownData.currentX" 
                :cy="burndownData.currentY" 
                r="5" 
                fill="var(--status-blue)"
              />
              
              <!-- Y-axis labels -->
              <text x="35" y="24" class="chart-label" text-anchor="end">{{ burndownData.maxSize }}</text>
              <text x="35" y="144" class="chart-label" text-anchor="end">0</text>
              
              <!-- X-axis labels -->
              <text x="40" y="158" class="chart-label" text-anchor="start">Day 1</text>
              <text x="290" y="158" class="chart-label" text-anchor="end">Day {{ burndownData.totalDays }}</text>
            </svg>
            
            <div class="burndown-chart-legend">
              <span class="legend-item">
                <span class="legend-line legend-ideal"></span>
                Ideal
              </span>
              <span class="legend-item">
                <span class="legend-line legend-actual"></span>
                Actual
              </span>
            </div>
            
            <p class="burndown-chart-hint">
              <template v-if="burndownData.isAhead">
                <span class="status-good">Ahead of schedule</span> by {{ burndownData.variance }} {{ taskSizeUnit }}
              </template>
              <template v-else-if="burndownData.isBehind">
                <span class="status-warn">Behind schedule</span> by {{ burndownData.variance }} {{ taskSizeUnit }}
              </template>
              <template v-else>
                <span class="status-good">On track</span>
              </template>
            </p>
          </div>
          <p v-else class="analytics-empty">Select a sprint to view burndown</p>
        </section>

        <!-- Completion Funnel -->
        <section class="analytics-card card">
          <h3 class="analytics-title">
            <Filter class="analytics-icon" />
            Completion Funnel
          </h3>
          <div class="funnel-stages">
            <div class="funnel-stage funnel-todo">
              <span class="funnel-label">To Do</span>
              <div class="funnel-bar-wrap">
                <div class="funnel-bar" :style="{ width: funnelWidth('todo') }"></div>
              </div>
              <span class="funnel-count">{{ todoTasks.length }}</span>
            </div>
            <div class="funnel-stage funnel-progress">
              <span class="funnel-label">In Progress</span>
              <div class="funnel-bar-wrap">
                <div class="funnel-bar" :style="{ width: funnelWidth('in-progress') }"></div>
              </div>
              <span class="funnel-count">{{ inProgressTasks.length }}</span>
            </div>
            <div class="funnel-stage funnel-done">
              <span class="funnel-label">Completed</span>
              <div class="funnel-bar-wrap">
                <div class="funnel-bar" :style="{ width: funnelWidth('completed') }"></div>
              </div>
              <span class="funnel-count">{{ completedTasks.length }}</span>
            </div>
          </div>
          <p class="funnel-conversion">
            {{ completionRate }}% completion rate
          </p>
        </section>
      </div>

      <!-- Workload by Assignee - Task Count -->
      <section class="analytics-card card">
        <h3 class="analytics-title">
          <Users class="analytics-icon" />
          Workload (Tasks)
        </h3>
        <div class="workload-list">
          <div
            v-for="item in workloadByAssignee"
            :key="item.userId"
            class="workload-row"
          >
            <span class="workload-avatar" :style="{ backgroundColor: getUserColor(item.userId), color: '#fff' }">{{ getUserAvatar(item.userId) }}</span>
            <span class="workload-name">{{ getUserName(item.userId) }}</span>
            <div class="workload-bar-wrap">
              <div
                class="workload-bar"
                :style="{ width: `${item.percentTasks}%` }"
                :title="`${item.total} tasks`"
              ></div>
            </div>
            <span class="workload-count">{{ item.total }}</span>
          </div>
          <p v-if="workloadByAssignee.length === 0" class="analytics-empty">No assigned tasks</p>
        </div>
      </section>

      <!-- Workload by Assignee - Size (Hours/Days) -->
      <section class="analytics-card card">
        <h3 class="analytics-title">
          <Clock class="analytics-icon" />
          Workload ({{ taskSizeUnit === 'hours' ? 'Hours' : 'Days' }})
        </h3>
        <div class="workload-list">
          <div
            v-for="item in workloadByAssignee"
            :key="item.userId"
            class="workload-row"
          >
            <span class="workload-avatar" :style="{ backgroundColor: getUserColor(item.userId), color: '#fff' }">{{ getUserAvatar(item.userId) }}</span>
            <span class="workload-name">{{ getUserName(item.userId) }}</span>
            <div class="workload-bar-wrap">
              <div
                class="workload-bar workload-bar-size"
                :style="{ width: `${item.percentSize}%` }"
                :title="`${item.size} ${taskSizeUnit}`"
              ></div>
            </div>
            <span class="workload-count">{{ item.size }}{{ taskSizeUnit === 'hours' ? 'h' : 'd' }}</span>
          </div>
          <p v-if="workloadByAssignee.length === 0" class="analytics-empty">No assigned tasks</p>
        </div>
      </section>

      <!-- Overdue & At-Risk Tasks -->
      <section class="analytics-card card analytics-full">
        <h3 class="analytics-title">
          <AlertTriangle class="analytics-icon analytics-icon-warn" />
          Overdue & At Risk
        </h3>
        <div class="at-risk-list">
          <div
            v-for="task in overdueAndAtRiskTasks"
            :key="task.id"
            class="at-risk-item"
            @click="$emit('edit-task', task)"
          >
            <span :class="['at-risk-badge', task.isOverdue ? 'badge-overdue' : 'badge-at-risk']">
              {{ task.isOverdue ? 'Overdue' : 'Due soon' }}
            </span>
            <span class="at-risk-title">{{ task.title }}</span>
            <span class="at-risk-due">{{ formatDueDate(task.dueDate) }}</span>
          </div>
          <p v-if="overdueAndAtRiskTasks.length === 0" class="analytics-empty">No overdue or at-risk tasks</p>
        </div>
      </section>

      <!-- Task Load by Sprint -->
      <section class="analytics-card card">
        <h3 class="analytics-title">
          <BarChart3 class="analytics-icon" />
          Tasks by Sprint
        </h3>
        <div class="sprint-load-list">
          <div
            v-for="sprint in taskLoadBySprint"
            :key="sprint.id ?? 'backlog'"
            class="sprint-load-row"
          >
            <span class="sprint-load-name" :class="{ 'sprint-backlog': sprint.id === null }">
              {{ sprint.name }}
            </span>
            <div class="sprint-load-bar-wrap">
              <div
                class="sprint-load-bar"
                :style="{ width: `${sprint.percentCount}%` }"
                :title="`${sprint.taskCount} tasks`"
              ></div>
            </div>
            <span class="sprint-load-count">{{ sprint.taskCount }}</span>
          </div>
          <p v-if="taskLoadBySprint.length === 0" class="analytics-empty">No sprints available</p>
        </div>
      </section>

      <!-- Tag Breakdown by Sprint (Pie Chart) -->
      <section class="analytics-card card">
        <h3 class="analytics-title">
          <Tag class="analytics-icon" />
          Tags by Sprint
        </h3>
        <div v-if="sprintsWithBacklog.length > 0" class="tag-breakdown-container">
          <!-- Sprint Selector -->
          <div class="tag-sprint-selector">
            <button 
              class="tag-nav-btn" 
              @click="prevSprint" 
              :disabled="selectedSprintIndex === 0"
            >
              <ChevronLeft class="nav-icon" />
            </button>
            <span class="tag-sprint-name">{{ selectedSprintForTags?.name }}</span>
            <button 
              class="tag-nav-btn" 
              @click="nextSprint" 
              :disabled="selectedSprintIndex >= sprintsWithBacklog.length - 1"
            >
              <ChevronRight class="nav-icon" />
            </button>
          </div>

          <!-- Pie Chart and Legend -->
          <div class="tag-pie-container">
            <div 
              class="tag-pie-chart"
              :style="{ background: pieChartGradient }"
            >
              <div class="tag-pie-center">
                {{ tagBreakdownForSprint.reduce((sum, t) => sum + t.count, 0) }}
                <span>tags</span>
              </div>
            </div>
            <div class="tag-legend">
              <div
                v-for="item in tagBreakdownForSprint.slice(0, 6)"
                :key="item.tag"
                class="tag-legend-item"
              >
                <span class="tag-legend-color" :style="{ backgroundColor: item.color }"></span>
                <span class="tag-legend-name">{{ item.tag }}</span>
                <span class="tag-legend-count">{{ item.count }}</span>
              </div>
              <div v-if="tagBreakdownForSprint.length > 6" class="tag-legend-more">
                +{{ tagBreakdownForSprint.length - 6 }} more
              </div>
              <p v-if="tagBreakdownForSprint.length === 0" class="analytics-empty">No tags in this sprint</p>
            </div>
          </div>
        </div>
        <p v-else class="analytics-empty">No sprints available</p>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { TrendingDown, Filter, Users, AlertTriangle, Clock, BarChart3, Tag, ChevronLeft, ChevronRight } from 'lucide-vue-next'
import { ref, computed, watch } from 'vue'
import { getInitials, getUserColor } from '@/utils/initials'
import type { Task, Sprint } from '@/types'

const props = withDefaults(
  defineProps<{
    todoTasks: Task[]
    inProgressTasks: Task[]
    completedTasks: Task[]
    allFilteredTasks: Task[]
    allProjectTasks: Task[]
    sprints: Sprint[]
    users: { id: number; name: string; avatar: string }[]
    taskSizeUnit: string
    sprintProgress: { daysElapsed: number; daysRemaining: number; percentComplete: number } | null
    layout?: 'desktop' | 'tablet' | 'mobile'
  }>(),
  { layout: 'desktop' }
)

defineEmits<{
  'edit-task': [task: Task]
}>()

const totalSize = computed(() => {
  const tasks = [...props.todoTasks, ...props.inProgressTasks, ...props.completedTasks]
  return tasks.reduce((sum, t) => sum + (t.size ?? 0), 0)
})

const completedSize = computed(() =>
  props.completedTasks.reduce((sum, t) => sum + (t.size ?? 0), 0)
)

const remainingSize = computed(() =>
  [...props.todoTasks, ...props.inProgressTasks].reduce((sum, t) => sum + (t.size ?? 0), 0)
)

const completionPercent = computed(() => {
  if (totalSize.value <= 0) return 100
  return Math.round((completedSize.value / totalSize.value) * 100)
})

// Burndown chart data
const burndownData = computed(() => {
  if (!props.sprintProgress) {
    return { totalDays: 0, maxSize: 0, actualPoints: '', currentX: 0, currentY: 0, gridLines: 5, isAhead: false, isBehind: false, variance: 0 }
  }
  
  const { daysElapsed, daysRemaining } = props.sprintProgress
  const totalDays = daysElapsed + daysRemaining
  const maxSize = totalSize.value || 1
  const remaining = remainingSize.value
  
  // Chart dimensions
  const chartLeft = 40
  const chartRight = 290
  const chartTop = 20
  const chartBottom = 140
  const chartWidth = chartRight - chartLeft
  const chartHeight = chartBottom - chartTop
  
  // Calculate current position
  const progressPercent = totalDays > 0 ? daysElapsed / totalDays : 0
  const currentX = chartLeft + progressPercent * chartWidth
  const currentY = maxSize > 0 
    ? chartTop + ((maxSize - remaining) / maxSize) * chartHeight 
    : chartBottom
  
  // Build actual line points (start to current)
  // Simple: straight line from start to current remaining
  const actualPoints = `${chartLeft},${chartTop} ${currentX},${chartBottom - (remaining / maxSize) * chartHeight}`
  
  // Calculate ideal remaining at this point
  const idealRemaining = maxSize * (1 - progressPercent)
  const variance = Math.abs(Math.round(remaining - idealRemaining))
  const isAhead = remaining < idealRemaining - 0.5
  const isBehind = remaining > idealRemaining + 0.5
  
  // Grid lines based on sprint length
  const gridLines = Math.min(Math.max(totalDays + 1, 3), 8)
  
  return {
    totalDays,
    maxSize: Math.round(maxSize),
    actualPoints,
    currentX,
    currentY: chartBottom - (remaining / maxSize) * chartHeight,
    gridLines,
    isAhead,
    isBehind,
    variance
  }
})

const funnelMax = computed(() =>
  Math.max(
    props.todoTasks.length,
    props.inProgressTasks.length,
    props.completedTasks.length,
    1
  )
)

function funnelWidth(status: 'todo' | 'in-progress' | 'completed'): string {
  const count =
    status === 'todo'
      ? props.todoTasks.length
      : status === 'in-progress'
        ? props.inProgressTasks.length
        : props.completedTasks.length
  return `${(count / funnelMax.value) * 100}%`
}

const completionRate = computed(() => {
  const total = props.todoTasks.length + props.inProgressTasks.length + props.completedTasks.length
  if (total === 0) return 0
  return Math.round((props.completedTasks.length / total) * 100)
})

const workloadByAssignee = computed(() => {
  const assignees = new Map<number, { total: number; size: number }>()
  for (const task of props.allFilteredTasks) {
    const cur = assignees.get(task.assigneeId) ?? { total: 0, size: 0 }
    cur.total++
    cur.size += task.size ?? 0
    assignees.set(task.assigneeId, cur)
  }
  const maxTasks = Math.max(...Array.from(assignees.values()).map((v) => v.total), 1)
  const maxSize = Math.max(...Array.from(assignees.values()).map((v) => v.size), 1)
  return Array.from(assignees.entries()).map(([userId, { total, size }]) => ({
    userId,
    total,
    size,
    percentTasks: (total / maxTasks) * 100,
    percentSize: (size / maxSize) * 100
  }))
})

const overdueAndAtRiskTasks = computed(() => {
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const inThreeDays = new Date(today)
  inThreeDays.setDate(inThreeDays.getDate() + 3)

  const result: (Task & { isOverdue: boolean })[] = []
  for (const task of props.allFilteredTasks) {
    if (!task.dueDate) continue
    const due = new Date(task.dueDate)
    due.setHours(0, 0, 0, 0)
    if (due < today) {
      result.push({ ...task, isOverdue: true })
    } else if (due <= inThreeDays && task.status !== 'completed') {
      result.push({ ...task, isOverdue: false })
    }
  }
  result.sort((a, b) => {
    const da = new Date(a.dueDate!).getTime()
    const db = new Date(b.dueDate!).getTime()
    return da - db
  })
  return result
})

// Task load by sprint
const taskLoadBySprint = computed(() => {
  const sprintData: { id: number | null; name: string; taskCount: number; totalSize: number }[] = []
  
  // Group tasks by sprint
  const tasksBySprintId = new Map<number | null, Task[]>()
  for (const task of props.allProjectTasks) {
    const sprintId = task.sprintId ?? null
    const existing = tasksBySprintId.get(sprintId) ?? []
    existing.push(task)
    tasksBySprintId.set(sprintId, existing)
  }
  
  // Add sprint data
  for (const sprint of props.sprints) {
    const tasks = tasksBySprintId.get(sprint.id) ?? []
    sprintData.push({
      id: sprint.id,
      name: sprint.name,
      taskCount: tasks.length,
      totalSize: tasks.reduce((sum, t) => sum + (t.size ?? 0), 0)
    })
  }
  
  // Add backlog (tasks without sprint)
  const backlogTasks = tasksBySprintId.get(null) ?? []
  if (backlogTasks.length > 0) {
    sprintData.push({
      id: null,
      name: 'Backlog',
      taskCount: backlogTasks.length,
      totalSize: backlogTasks.reduce((sum, t) => sum + (t.size ?? 0), 0)
    })
  }
  
  const maxCount = Math.max(...sprintData.map(s => s.taskCount), 1)
  const maxSize = Math.max(...sprintData.map(s => s.totalSize), 1)
  
  return sprintData.map(s => ({
    ...s,
    percentCount: (s.taskCount / maxCount) * 100,
    percentSize: (s.totalSize / maxSize) * 100
  }))
})

// Tag colors for pie chart
const tagColors = [
  '#6366f1', '#8b5cf6', '#a855f7', '#d946ef', '#ec4899',
  '#f43f5e', '#ef4444', '#f97316', '#f59e0b', '#eab308',
  '#84cc16', '#22c55e', '#10b981', '#14b8a6', '#06b6d4'
]

// Selected sprint for tag breakdown
const selectedSprintIndex = ref(0)

// Watch for sprint changes and reset index
watch(() => props.sprints.length, () => {
  if (selectedSprintIndex.value >= sprintsWithBacklog.value.length) {
    selectedSprintIndex.value = 0
  }
})

const sprintsWithBacklog = computed(() => {
  const result = [...props.sprints.map(s => ({ id: s.id as number | null, name: s.name }))]
  // Check if there are backlog tasks
  const hasBacklog = props.allProjectTasks.some(t => !t.sprintId)
  if (hasBacklog) {
    result.push({ id: null, name: 'Backlog' })
  }
  return result
})

const selectedSprintForTags = computed(() => {
  if (sprintsWithBacklog.value.length === 0) return null
  return sprintsWithBacklog.value[selectedSprintIndex.value] ?? sprintsWithBacklog.value[0]
})

function nextSprint() {
  if (selectedSprintIndex.value < sprintsWithBacklog.value.length - 1) {
    selectedSprintIndex.value++
  }
}

function prevSprint() {
  if (selectedSprintIndex.value > 0) {
    selectedSprintIndex.value--
  }
}

// Tag breakdown for selected sprint
const tagBreakdownForSprint = computed(() => {
  if (!selectedSprintForTags.value) return []
  
  const sprintId = selectedSprintForTags.value.id
  const tasksInSprint = props.allProjectTasks.filter(t => 
    sprintId === null ? !t.sprintId : t.sprintId === sprintId
  )
  
  // Count tags
  const tagCounts = new Map<string, number>()
  let totalTags = 0
  for (const task of tasksInSprint) {
    if (task.tags && task.tags.length > 0) {
      for (const tag of task.tags) {
        tagCounts.set(tag, (tagCounts.get(tag) ?? 0) + 1)
        totalTags++
      }
    }
  }
  
  // No tag category
  const tasksWithoutTags = tasksInSprint.filter(t => !t.tags || t.tags.length === 0).length
  
  // Convert to array with percentages
  const result = Array.from(tagCounts.entries())
    .map(([tag, count], index) => ({
      tag,
      count,
      percent: totalTags > 0 ? (count / totalTags) * 100 : 0,
      color: tagColors[index % tagColors.length]!
    }))
    .sort((a, b) => b.count - a.count)
  
  // Add "No Tag" if there are tasks without tags
  if (tasksWithoutTags > 0) {
    result.push({
      tag: 'No Tag',
      count: tasksWithoutTags,
      percent: totalTags + tasksWithoutTags > 0 ? (tasksWithoutTags / (totalTags + tasksWithoutTags)) * 100 : 0,
      color: '#9ca3af'
    })
  }
  
  return result
})

// Calculate pie chart segments (cumulative angles for conic-gradient)
const pieChartGradient = computed(() => {
  if (tagBreakdownForSprint.value.length === 0) return 'conic-gradient(#e5e7eb 0% 100%)'
  
  const total = tagBreakdownForSprint.value.reduce((sum, t) => sum + t.count, 0)
  if (total === 0) return 'conic-gradient(#e5e7eb 0% 100%)'
  
  const segments: string[] = []
  let cumulative = 0
  
  for (const item of tagBreakdownForSprint.value) {
    const percent = (item.count / total) * 100
    segments.push(`${item.color} ${cumulative}% ${cumulative + percent}%`)
    cumulative += percent
  }
  
  return `conic-gradient(${segments.join(', ')})`
})

function getUserAvatar(userId: number): string {
  const u = props.users.find((u) => u.id === userId)
  return u ? getInitials(u) : '?'
}

function getUserName(userId: number): string {
  return props.users.find((u) => u.id === userId)?.name ?? 'Unknown'
}

function formatDueDate(d: Date | string | undefined): string {
  if (!d) return ''
  const date = typeof d === 'string' ? new Date(d) : d
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const due = new Date(date)
  due.setHours(0, 0, 0, 0)
  const diff = Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))
  if (diff < 0) return `${Math.abs(diff)} days overdue`
  if (diff === 0) return 'Today'
  if (diff === 1) return 'Tomorrow'
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}
</script>

<style scoped>
.analytics-view {
  padding-top: 0.5rem;
}

.analytics-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
}

.sprint-row {
  grid-column: 1 / -1;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
}

@media (max-width: 1024px) {
  .sprint-row {
    grid-template-columns: 1fr 1fr;
  }
  .sprint-row > :last-child {
    grid-column: 1 / -1;
  }
}

@media (max-width: 768px) {
  .analytics-grid {
    grid-template-columns: 1fr;
  }
  .sprint-row {
    grid-template-columns: 1fr;
  }
  .sprint-row > :last-child {
    grid-column: auto;
  }
}

/* Tablet variant: 2-col sprint row */
.analytics-tablet .analytics-grid {
  grid-template-columns: 1fr;
}
.analytics-tablet .sprint-row {
  grid-template-columns: 1fr 1fr;
}
.analytics-tablet .sprint-row > :last-child {
  grid-column: 1 / -1;
}

/* Mobile variant: single column */
.analytics-mobile .analytics-grid {
  grid-template-columns: 1fr;
}
.analytics-mobile .sprint-row {
  grid-template-columns: 1fr;
}
.analytics-mobile .sprint-row > :last-child {
  grid-column: auto;
}
.analytics-mobile .analytics-card {
  padding: 1rem;
}
.analytics-mobile .analytics-title {
  font-size: 0.875rem;
}

.analytics-card {
  padding: 1.25rem;
}

.analytics-full {
  grid-column: 1 / -1;
}

.analytics-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 1rem;
}

.analytics-icon {
  width: 1.125rem;
  height: 1.125rem;
  color: var(--text-tertiary);
}

.analytics-icon-warn {
  color: var(--badge-red-text, #b91c1c);
}

/* Burndown */
.burndown-stats {
  display: flex;
  gap: 1.5rem;
  margin-bottom: 1rem;
}

.burndown-stat {
  display: flex;
  flex-direction: column;
}

.burndown-value {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
}

.burndown-label {
  font-size: 0.75rem;
  color: var(--text-tertiary);
}

.progress-bar-container {
  height: 0.5rem;
  background-color: var(--bg-tertiary);
  border-radius: 9999px;
  overflow: hidden;
  margin-bottom: 0.5rem;
}

.progress-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--status-blue), var(--status-amber));
  border-radius: 9999px;
  transition: width 0.3s ease;
}

.progress-bar-fill.progress-complete {
  background: var(--status-green);
}

.burndown-hint {
  font-size: 0.8125rem;
  color: var(--text-muted);
  margin: 0 0 0.25rem;
}

.sprint-days {
  font-size: 0.75rem;
  color: var(--text-tertiary);
}

/* Burndown Chart */
.burndown-chart-container {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.burndown-chart {
  width: 100%;
  height: auto;
  max-height: 200px;
}

.chart-label {
  font-size: 10px;
  fill: var(--text-tertiary);
}

.chart-grid line {
  opacity: 0.5;
}

.burndown-chart-legend {
  display: flex;
  justify-content: center;
  gap: 1.5rem;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  color: var(--text-secondary);
}

.legend-line {
  width: 20px;
  height: 3px;
  border-radius: 2px;
}

.legend-ideal {
  background: var(--text-tertiary);
  opacity: 0.6;
}

.legend-actual {
  background: var(--status-blue);
}

.burndown-chart-hint {
  font-size: 0.8125rem;
  color: var(--text-muted);
  text-align: center;
  margin: 0;
}

.status-good {
  color: var(--status-green);
  font-weight: 500;
}

.status-warn {
  color: var(--status-amber);
  font-weight: 500;
}

/* Funnel */
.funnel-stages {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.funnel-stage {
  display: grid;
  grid-template-columns: 100px 1fr auto;
  align-items: center;
  gap: 0.75rem;
}

.funnel-bar-wrap {
  height: 1.5rem;
  background-color: var(--bg-tertiary);
  border-radius: 0.375rem;
  overflow: hidden;
}

.funnel-bar {
  height: 100%;
  border-radius: 0.375rem;
  min-width: 4px;
  transition: width 0.3s ease;
}

.funnel-todo .funnel-bar {
  background-color: var(--status-blue);
  opacity: 0.8;
}

.funnel-progress .funnel-bar {
  background-color: var(--status-amber);
  opacity: 0.8;
}

.funnel-done .funnel-bar {
  background-color: var(--status-green);
  opacity: 0.8;
}

.funnel-label {
  font-size: 0.8125rem;
  color: var(--text-secondary);
}

.funnel-count {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  min-width: 2rem;
}

.funnel-conversion {
  font-size: 0.8125rem;
  color: var(--text-muted);
  margin: 1rem 0 0;
}

/* Workload */
.workload-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.workload-row {
  display: grid;
  grid-template-columns: 2rem 140px 1fr auto;
  align-items: center;
  gap: 0.75rem;
}

.workload-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.75rem;
  height: 1.75rem;
  font-size: 0.625rem;
  font-weight: 600;
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: 0.25rem;
}

.workload-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-primary);
}

.workload-bar-wrap {
  height: 0.5rem;
  background-color: var(--bg-tertiary);
  border-radius: 9999px;
  overflow: hidden;
}

.workload-bar {
  height: 100%;
  background: linear-gradient(90deg, var(--color-brown-400), var(--color-brown-600));
  border-radius: 9999px;
  transition: width 0.3s ease;
}

.workload-bar.workload-bar-size {
  background: linear-gradient(90deg, #6366f1, #4f46e5);
}

.workload-count {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--text-secondary);
  min-width: 3rem;
  text-align: right;
}

/* At-risk */
.at-risk-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.at-risk-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.625rem 0.75rem;
  background-color: var(--bg-tertiary);
  border-radius: 0.5rem;
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.at-risk-item:hover {
  background-color: var(--bg-hover);
}

.at-risk-badge {
  font-size: 0.6875rem;
  font-weight: 600;
  padding: 0.125rem 0.5rem;
  border-radius: 9999px;
  flex-shrink: 0;
}

.badge-overdue {
  background-color: var(--badge-red-bg);
  color: var(--badge-red-text);
}

.badge-at-risk {
  background-color: var(--badge-amber-bg, rgba(217, 119, 6, 0.2));
  color: var(--badge-amber-text, #b45309);
}

.at-risk-title {
  flex: 1;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.at-risk-due {
  font-size: 0.75rem;
  color: var(--text-muted);
  flex-shrink: 0;
}

.analytics-empty {
  font-size: 0.875rem;
  color: var(--text-muted);
  margin: 0;
  padding: 1rem 0;
}

/* Sprint Load Chart */
.sprint-load-list {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
}

.sprint-load-row {
  display: grid;
  grid-template-columns: 100px 1fr 2.5rem;
  align-items: center;
  gap: 0.75rem;
}

.sprint-load-name {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sprint-load-name.sprint-backlog {
  color: var(--text-tertiary);
  font-style: italic;
}

.sprint-load-bar-wrap {
  height: 1.25rem;
  background-color: var(--bg-tertiary);
  border-radius: 0.375rem;
  overflow: hidden;
}

.sprint-load-bar {
  height: 100%;
  background: linear-gradient(90deg, #10b981, #059669);
  border-radius: 0.375rem;
  min-width: 4px;
  transition: width 0.3s ease;
}

.sprint-load-count {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--text-secondary);
  text-align: right;
}

/* Tag Breakdown Pie Chart */
.tag-breakdown-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.tag-sprint-selector {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
}

.tag-nav-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  padding: 0;
  background: var(--bg-tertiary);
  border: none;
  border-radius: 0.375rem;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all 0.15s ease;
}

.tag-nav-btn:hover:not(:disabled) {
  background: var(--bg-hover);
  color: var(--text-primary);
}

.tag-nav-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.nav-icon {
  width: 1rem;
  height: 1rem;
}

.tag-sprint-name {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  min-width: 100px;
  text-align: center;
}

.tag-pie-container {
  display: flex;
  align-items: flex-start;
  gap: 1.5rem;
}

.tag-pie-chart {
  position: relative;
  width: 120px;
  height: 120px;
  border-radius: 50%;
  flex-shrink: 0;
}

.tag-pie-center {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 60px;
  height: 60px;
  background: var(--card-bg);
  border-radius: 50%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.tag-pie-center span {
  font-size: 0.625rem;
  font-weight: 500;
  color: var(--text-tertiary);
  margin-top: 2px;
}

.tag-legend {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.tag-legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.tag-legend-color {
  width: 0.625rem;
  height: 0.625rem;
  border-radius: 2px;
  flex-shrink: 0;
}

.tag-legend-name {
  flex: 1;
  font-size: 0.75rem;
  color: var(--text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tag-legend-count {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-primary);
}

.tag-legend-more {
  font-size: 0.6875rem;
  color: var(--text-tertiary);
  margin-top: 0.25rem;
}
</style>
