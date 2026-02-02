<template>
  <button
    @click="themeStore.toggleTheme()"
    class="theme-toggle"
    :title="themeTitle"
    aria-label="Toggle theme"
  >
    <span class="icon-wrapper">
      <Sun v-if="themeStore.resolvedTheme === 'light'" class="icon" />
      <Moon v-else class="icon" />
    </span>
    <span v-if="showLabel" class="label">{{ themeLabel }}</span>
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Sun, Moon } from 'lucide-vue-next'
import { useThemeStore } from '@/stores/theme'

defineProps<{
  showLabel?: boolean
}>()

const themeStore = useThemeStore()

const themeTitle = computed(() =>
  themeStore.theme === 'light' ? 'Light (click for dark)' : 'Dark (click for light)'
)

const themeLabel = computed(() =>
  themeStore.theme === 'light' ? 'Light' : 'Dark'
)
</script>

<style scoped>
.theme-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem;
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
  border: 1px solid var(--border-primary);
  border-radius: 9999px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.theme-toggle:hover {
  background-color: var(--bg-hover);
  color: var(--text-primary);
  border-color: var(--border-secondary);
}

.icon-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
}

.icon {
  width: 1.125rem;
  height: 1.125rem;
}

.label {
  font-size: 0.8125rem;
  font-weight: 500;
  padding-right: 0.375rem;
}
</style>
