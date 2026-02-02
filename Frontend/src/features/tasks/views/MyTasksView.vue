<template>
  <component :is="viewComponent" />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useViewport } from '@/composables/useViewport'
import MyTasksDesktop from './desktop/MyTasksDesktop.vue'
import MyTasksTablet from './tablet/MyTasksTablet.vue'
import MyTasksMobile from './mobile/MyTasksMobile.vue'

const { viewport } = useViewport()

const viewComponent = computed(() => {
  switch (viewport.value) {
    case 'desktop':
      return MyTasksDesktop
    case 'tablet':
      return MyTasksTablet
    case 'mobile':
      return MyTasksMobile
    default:
      return MyTasksDesktop
  }
})
</script>
