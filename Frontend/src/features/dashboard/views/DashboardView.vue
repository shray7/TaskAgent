<template>
  <component :is="viewComponent" />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { useViewport } from '@/composables/useViewport'
import { useBoardRealtime } from '@/composables/useBoardRealtime'
import { useProjectsStore } from '@/stores/projects'
import { useSprintsStore } from '@/stores/sprints'
import DashboardDesktop from './desktop/DashboardDesktop.vue'
import DashboardTablet from './tablet/DashboardTablet.vue'
import DashboardMobile from './mobile/DashboardMobile.vue'

const { viewport } = useViewport()
const { currentProjectId } = storeToRefs(useProjectsStore())
const { currentSprintId } = storeToRefs(useSprintsStore())
useBoardRealtime(currentProjectId, currentSprintId)

const viewComponent = computed(() => {
  switch (viewport.value) {
    case 'desktop':
      return DashboardDesktop
    case 'tablet':
      return DashboardTablet
    case 'mobile':
      return DashboardMobile
    default:
      return DashboardDesktop
  }
})
</script>
