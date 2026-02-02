import { defineStore } from 'pinia'
import type { PiniaPluginContext } from 'pinia'
import { ref } from 'vue'

export type Theme = 'light' | 'dark'

export const useThemeStore = defineStore(
  'theme',
  () => {
    const theme = ref<Theme>('light')
    const resolvedTheme = ref<'light' | 'dark'>('light')

    function updateResolvedTheme(): void {
      resolvedTheme.value = theme.value
      applyTheme()
    }

    function applyTheme(): void {
      const root = document.documentElement
      root.classList.remove('light', 'dark')
      root.classList.add(resolvedTheme.value)
    }

    function setTheme(newTheme: Theme): void {
      theme.value = newTheme
      updateResolvedTheme()
    }

    function toggleTheme(): void {
      setTheme(theme.value === 'light' ? 'dark' : 'light')
    }

    /** Call after Pinia rehydrates persisted theme (e.g. in main or afterRestore). */
    function applyThemeAfterRestore(): void {
      const v = theme.value as string
      if (v === 'system') theme.value = 'light'
      updateResolvedTheme()
    }

    return {
      theme,
      resolvedTheme,
      setTheme,
      toggleTheme,
      applyThemeAfterRestore
    }
  },
  {
    persist: {
      paths: ['theme'],
      afterRestore(ctx: PiniaPluginContext) {
        ;(ctx.store as unknown as { applyThemeAfterRestore: () => void }).applyThemeAfterRestore()
      }
    }
  }
)
