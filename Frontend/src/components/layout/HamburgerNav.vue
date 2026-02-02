<template>
  <div>
    <button
      class="hamburger-btn"
      aria-label="Open menu"
      @click="open = true"
    >
      <Menu class="hamburger-icon" />
    </button>

    <Teleport to="body">
      <Transition name="drawer">
        <div v-if="open" class="nav-overlay" @click.self="open = false">
          <aside class="nav-drawer">
            <div class="drawer-header">
              <div class="brand">
                <div class="brand-icon">
                  <CheckSquare class="brand-icon-svg" />
                </div>
                <span class="brand-name">TaskAgent</span>
              </div>
              <button class="close-btn" aria-label="Close menu" @click="open = false">
                <X class="close-icon" />
              </button>
            </div>

            <nav class="drawer-nav">
              <router-link to="/dashboard" class="nav-link" @click="open = false">
                <LayoutDashboard class="nav-icon" />
                Dashboard
              </router-link>
              <router-link to="/my-tasks" class="nav-link" @click="open = false">
                <User class="nav-icon" />
                My Tasks
              </router-link>
            </nav>

            <div v-if="showProjectSprint" class="drawer-section">
              <ProjectSelector />
              <SprintSelector />
            </div>

            <div class="drawer-footer">
              <div class="user-pill">
                <span
                  class="user-avatar"
                  :style="authStore.user ? { backgroundColor: getUserColor(authStore.user.id), color: '#fff' } : {}"
                >
                  {{ authStore.user ? getInitials(authStore.user) : '' }}
                </span>
                <span class="user-name">{{ authStore.user?.name }}</span>
              </div>
              <div class="drawer-actions">
                <ThemeToggle class="theme-toggle" />
                <button class="logout-btn" @click="handleLogout">
                  <LogOut class="btn-icon" />
                  Logout
                </button>
              </div>
            </div>
          </aside>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Menu, X, CheckSquare, LayoutDashboard, User, LogOut } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import ThemeToggle from '@/components/ui/ThemeToggle.vue'
import ProjectSelector from '@/components/layout/ProjectSelector.vue'
import SprintSelector from '@/components/layout/SprintSelector.vue'
import { getInitials, getUserColor } from '@/utils/initials'

defineProps<{
  showProjectSprint?: boolean
}>()

const router = useRouter()
const authStore = useAuthStore()
const open = ref(false)

const handleLogout = () => {
  authStore.logout()
  open.value = false
  router.push('/login')
}
</script>

<style scoped>
.hamburger-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  background-color: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 0.5rem;
  color: var(--header-text);
  cursor: pointer;
}

.hamburger-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.nav-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 100;
  display: flex;
  justify-content: flex-start;
}

.nav-drawer {
  width: min(320px, 85vw);
  height: 100%;
  background-color: var(--header-bg);
  border-right: 1px solid var(--header-border);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.drawer-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.brand-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background-color: rgba(255, 255, 255, 0.15);
  border-radius: 0.5rem;
}

.brand-icon-svg {
  width: 1.125rem;
  height: 1.125rem;
  color: var(--header-text);
}

.brand-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--header-text);
}

.close-btn {
  padding: 0.5rem;
  background: transparent;
  border: none;
  color: var(--header-text);
  cursor: pointer;
  border-radius: 0.375rem;
}

.close-btn:hover {
  background-color: rgba(255, 255, 255, 0.1);
}

.close-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.drawer-nav {
  display: flex;
  flex-direction: column;
  padding: 1rem;
  gap: 0.25rem;
}

.drawer-nav .nav-link {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  font-size: 0.9375rem;
  font-weight: 500;
  color: rgba(255, 255, 255, 0.85);
  text-decoration: none;
  border-radius: 0.5rem;
  transition: background 0.15s;
}

.drawer-nav .nav-link:hover {
  background-color: rgba(255, 255, 255, 0.1);
  color: white;
}

.drawer-nav .nav-link.router-link-active {
  background-color: rgba(255, 255, 255, 0.15);
  color: white;
}

.nav-icon {
  width: 1.125rem;
  height: 1.125rem;
}

.drawer-section {
  padding: 0 1rem 1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  padding-top: 1rem;
  margin-top: auto;
}

.drawer-footer {
  margin-top: auto;
  padding: 1rem 1.25rem;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.drawer-footer .user-pill {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.drawer-footer .user-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 2rem;
  height: 2rem;
  font-size: 0.75rem;
  font-weight: 600;
  border-radius: 0.375rem;
}

.drawer-footer .user-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--header-text);
}

.drawer-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.theme-toggle {
  flex: 1;
}

.logout-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--header-text);
  background-color: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 0.5rem;
  cursor: pointer;
}

.logout-btn:hover {
  background-color: rgba(255, 255, 255, 0.15);
}

.btn-icon {
  width: 1rem;
  height: 1rem;
}

.drawer-enter-active,
.drawer-leave-active {
  transition: opacity 0.2s ease;
}

.drawer-enter-active .nav-drawer,
.drawer-leave-active .nav-drawer {
  transition: transform 0.2s ease;
}

.drawer-enter-from,
.drawer-leave-to {
  opacity: 0;
}

.drawer-enter-from .nav-drawer,
.drawer-leave-to .nav-drawer {
  transform: translateX(-100%);
}
</style>
