<template>
  <AnalyticsView v-bind="$props" :layout="'tablet'" @edit-task="$emit('edit-task', $event)" />
</template>

<script setup lang="ts">
import AnalyticsView from './AnalyticsView.vue'
import type { Task, Sprint } from '@/types'

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
}>()

defineEmits<{ 'edit-task': [task: Task] }>()
</script>
