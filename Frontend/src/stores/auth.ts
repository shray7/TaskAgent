import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '@/types'
import { api } from '@/services/api'

export const useAuthStore = defineStore(
  'auth',
  () => {
    const user = ref<User | null>(null)
    const token = ref<string | null>(null)
    const users = ref<User[]>([])
    const usersLoaded = ref(false)

    const isAuthenticated = computed(() => !!user.value && !!token.value)

    async function loadUsers(): Promise<void> {
      if (usersLoaded.value) return
      try {
        users.value = await api.users.getAll()
        usersLoaded.value = true
      } catch {
        users.value = []
      }
    }

    async function login(email: string, password: string): Promise<{ success: boolean; message?: string }> {
      const result = await api.auth.login(email, password)
      if (result.success && result.user) {
        user.value = result.user
        token.value = result.token ?? null
        usersLoaded.value = false
      }
      return { success: result.success, message: result.message }
    }

    async function register(email: string, name: string, password: string): Promise<{ success: boolean; message?: string }> {
      const result = await api.auth.register(email, name, password)
      if (result.success && result.user) {
        user.value = result.user
        token.value = result.token ?? null
        usersLoaded.value = false
      }
      return { success: result.success, message: result.message }
    }

    function logout(): void {
      user.value = null
      token.value = null
    }

    return {
      user,
      token,
      users,
      usersLoaded,
      isAuthenticated,
      loadUsers,
      login,
      register,
      logout
    }
  },
  {
    persist: { paths: ['user', 'token'] }
  }
)
