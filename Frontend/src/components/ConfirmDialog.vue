<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="isOpen" class="confirm-overlay" @click.self="handleCancel">
        <div class="confirm-dialog" :class="variant">
          <div class="confirm-header">
            <div class="confirm-icon-wrapper" :class="variant">
              <AlertTriangle v-if="variant === 'danger'" class="confirm-icon" />
              <AlertCircle v-else class="confirm-icon" />
            </div>
            <h3 class="confirm-title">{{ title }}</h3>
          </div>
          
          <div class="confirm-body">
            <p class="confirm-message">{{ message }}</p>
            <p v-if="description" class="confirm-description">{{ description }}</p>
          </div>
          
          <div class="confirm-actions">
            <button 
              type="button" 
              class="btn btn-secondary" 
              @click="handleCancel"
              :disabled="loading"
            >
              {{ cancelText }}
            </button>
            <button 
              type="button" 
              :class="['btn', variant === 'danger' ? 'btn-danger' : 'btn-primary']"
              @click="handleConfirm"
              :disabled="loading"
            >
              <span v-if="loading" class="btn-spinner"></span>
              {{ confirmText }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { AlertTriangle, AlertCircle } from 'lucide-vue-next'

interface Props {
  isOpen: boolean
  title: string
  message: string
  description?: string
  confirmText?: string
  cancelText?: string
  variant?: 'danger' | 'warning' | 'info'
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  confirmText: 'Confirm',
  cancelText: 'Cancel',
  variant: 'info',
  loading: false
})

const emit = defineEmits<{
  confirm: []
  cancel: []
}>()

function handleConfirm() {
  emit('confirm')
}

function handleCancel() {
  if (!props.loading) {
    emit('cancel')
  }
}
</script>

<style scoped>
.confirm-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
  padding: 1rem;
  backdrop-filter: blur(2px);
}

.confirm-dialog {
  background-color: var(--card-bg);
  border-radius: 0.75rem;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  width: 100%;
  max-width: 400px;
  overflow: hidden;
  animation: dialog-enter 0.2s ease-out;
}

@keyframes dialog-enter {
  from {
    opacity: 0;
    transform: scale(0.95) translateY(-10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.confirm-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1.5rem 1.5rem 0.75rem;
  text-align: center;
}

.confirm-icon-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 3rem;
  height: 3rem;
  border-radius: 50%;
  margin-bottom: 0.75rem;
}

.confirm-icon-wrapper.danger {
  background-color: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.confirm-icon-wrapper.warning {
  background-color: rgba(245, 158, 11, 0.1);
  color: #f59e0b;
}

.confirm-icon-wrapper.info {
  background-color: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.confirm-icon {
  width: 1.5rem;
  height: 1.5rem;
}

.confirm-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.confirm-body {
  padding: 0.5rem 1.5rem 1.25rem;
  text-align: center;
}

.confirm-message {
  font-size: 0.9375rem;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1.5;
}

.confirm-description {
  font-size: 0.8125rem;
  color: var(--text-tertiary);
  margin: 0.75rem 0 0;
  line-height: 1.5;
}

.confirm-actions {
  display: flex;
  gap: 0.75rem;
  padding: 1rem 1.5rem 1.5rem;
  justify-content: center;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  font-size: 0.875rem;
  font-weight: 500;
  border-radius: 0.5rem;
  border: none;
  cursor: pointer;
  transition: all 0.15s ease;
  min-width: 100px;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  background-color: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-primary);
}

.btn-secondary:hover:not(:disabled) {
  background-color: var(--bg-hover);
}

.btn-primary {
  background-color: var(--color-brown-500);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: var(--color-brown-600);
}

.btn-danger {
  background-color: #ef4444;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background-color: #dc2626;
}

.btn-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Modal transition */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
</style>
