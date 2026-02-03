<template>
  <div class="notifications" aria-live="polite">
    <div
      v-for="item in notificationsStore.items"
      :key="item.id"
      :class="['notification', item.type]"
      role="alert"
    >
      <span class="notification-message">{{ item.message }}</span>
      <button
        type="button"
        class="notification-dismiss"
        aria-label="Dismiss"
        @click="notificationsStore.dismiss(item.id)"
      >
        <X class="dismiss-icon" />
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { X } from 'lucide-vue-next'
import { useNotificationsStore } from '@/stores/notifications'

const notificationsStore = useNotificationsStore()
</script>

<style scoped>
.notifications {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-width: min(24rem, calc(100vw - 2rem));
}

.notification {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  background: var(--card-bg);
  border: 1px solid var(--card-border);
  box-shadow: var(--shadow-lg);
}

.notification.error {
  border-color: var(--badge-red-text, #b91c1c);
  background: var(--badge-red-bg, #fef2f2);
}

.notification-message {
  flex: 1;
  font-size: 0.875rem;
  color: var(--text-primary);
}

.notification-dismiss {
  flex-shrink: 0;
  padding: 0.25rem;
  background: transparent;
  border: none;
  border-radius: 0.25rem;
  color: var(--text-muted);
  cursor: pointer;
}

.notification-dismiss:hover {
  color: var(--text-primary);
  background: var(--bg-hover);
}

.dismiss-icon {
  width: 1rem;
  height: 1rem;
}
</style>
