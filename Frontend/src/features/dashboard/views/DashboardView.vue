<template>
  <component :is="viewComponent" />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useViewport } from '@/composables/useViewport'
import DashboardDesktop from './desktop/DashboardDesktop.vue'
import DashboardTablet from './tablet/DashboardTablet.vue'
import DashboardMobile from './mobile/DashboardMobile.vue'

const { viewport } = useViewport()

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
