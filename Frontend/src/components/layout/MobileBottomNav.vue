<template>
  <nav class="mobile-bottom-nav" aria-label="Main navigation">
    <router-link
      to="/dashboard"
      class="nav-item"
      active-class="active"
      exact-active-class="active"
    >
      <LayoutDashboard class="nav-icon" />
      <span class="nav-label">Dashboard</span>
    </router-link>
    <router-link
      to="/my-tasks"
      class="nav-item"
      active-class="active"
      exact-active-class="active"
    >
      <CheckSquare class="nav-icon" />
      <span class="nav-label">My Tasks</span>
    </router-link>
    <router-link
      to="/project-settings"
      class="nav-item"
      active-class="active"
      exact-active-class="active"
    >
      <Settings class="nav-icon" />
      <span class="nav-label">Settings</span>
    </router-link>
    <router-link
      to="/profile"
      class="nav-item"
      active-class="active"
      exact-active-class="active"
    >
      <User class="nav-icon" />
      <span class="nav-label">Profile</span>
    </router-link>
  </nav>
</template>

<script setup lang="ts">
import { LayoutDashboard, CheckSquare, Settings, User } from 'lucide-vue-next'
</script>

<style scoped>
.mobile-bottom-nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  height: calc(3.5rem + env(safe-area-inset-bottom, 0px));
  padding-bottom: env(safe-area-inset-bottom, 0px);
  background: var(--header-bg);
  border-top: 1px solid var(--header-border);
  display: flex;
  align-items: stretch;
  justify-content: space-around;
  z-index: 40;
}

.nav-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.25rem;
  padding: 0.5rem;
  color: var(--text-muted);
  text-decoration: none;
  font-size: 0.6875rem;
  font-weight: 500;
  transition: color 0.15s ease, background 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.nav-item:hover {
  color: var(--text-secondary);
}

.nav-item.active {
  color: var(--btn-primary-bg);
}

.nav-icon {
  width: 1.5rem;
  height: 1.5rem;
  flex-shrink: 0;
}

.nav-label {
  line-height: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}
</style>
