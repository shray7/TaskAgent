<template>
  <component :is="viewComponent" />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useViewport } from '@/composables/useViewport'
import SignUpDesktop from './desktop/SignUpDesktop.vue'
import SignUpTablet from './tablet/SignUpTablet.vue'
import SignUpMobile from './mobile/SignUpMobile.vue'

const { viewport } = useViewport()

const viewComponent = computed(() => {
  switch (viewport.value) {
    case 'desktop':
      return SignUpDesktop
    case 'tablet':
      return SignUpTablet
    case 'mobile':
      return SignUpMobile
    default:
      return SignUpDesktop
  }
})
</script>
