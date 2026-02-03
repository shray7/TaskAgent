import { defineStore } from 'pinia'
import { ref } from 'vue'

export type NotificationType = 'error' | 'info'

export interface Notification {
  id: number
  message: string
  type: NotificationType
}

const AUTO_DISMISS_MS = 5000
let nextId = 0
let dismissTimer: ReturnType<typeof setTimeout> | null = null

export const useNotificationsStore = defineStore('notifications', () => {
  const items = ref<Notification[]>([])

  function add(message: string, type: NotificationType = 'error'): void {
    const id = ++nextId
    items.value = [...items.value, { id, message, type }]
    scheduleAutoDismiss()
  }

  function showError(message: string): void {
    add(message, 'error')
  }

  function showInfo(message: string): void {
    add(message, 'info')
  }

  function dismiss(id: number): void {
    items.value = items.value.filter((n) => n.id !== id)
  }

  function scheduleAutoDismiss(): void {
    if (dismissTimer) clearTimeout(dismissTimer)
    dismissTimer = setTimeout(() => {
      if (items.value.length > 0) {
        const oldest = items.value[0]
        if (oldest) dismiss(oldest.id)
      }
      dismissTimer = null
      if (items.value.length > 0) scheduleAutoDismiss()
    }, AUTO_DISMISS_MS)
  }

  return {
    items,
    showError,
    showInfo,
    dismiss
  }
})
