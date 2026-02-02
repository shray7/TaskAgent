<template>
  <component :is="viewComponent" />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useViewport } from '@/composables/useViewport'
import LoginDesktop from './desktop/LoginDesktop.vue'
import LoginTablet from './tablet/LoginTablet.vue'
import LoginMobile from './mobile/LoginMobile.vue'

const { viewport } = useViewport()

const viewComponent = computed(() => {
  switch (viewport.value) {
    case 'desktop':
      return LoginDesktop
    case 'tablet':
      return LoginTablet
    case 'mobile':
      return LoginMobile
    default:
      return LoginDesktop
  }
})
</script>
