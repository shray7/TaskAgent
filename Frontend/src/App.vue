<template>
  <AppNotifications />
  <div v-if="capturedError" class="error-boundary">
    <p class="error-boundary-message">Something went wrong.</p>
    <button type="button" class="btn btn-primary" @click="reloadPage">Reload</button>
  </div>
  <RouterView v-else />
</template>

<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue'
import AppNotifications from '@/components/AppNotifications.vue'
import { logger } from '@/utils/logger'

const capturedError = ref<Error | null>(null)

function reloadPage(): void {
  if (typeof window !== 'undefined') window.location.reload()
}

onErrorCaptured((err) => {
  capturedError.value = err
  logger.error('Unhandled error in component', { message: err?.message, stack: err?.stack })
  return false
})
</script>

<style scoped>
.error-boundary {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  padding: 2rem;
  background: var(--bg-secondary);
}
.error-boundary-message {
  margin: 0;
  color: var(--text-secondary);
}
</style>

<style>
/* Global styles handled in main.css */
</style>
