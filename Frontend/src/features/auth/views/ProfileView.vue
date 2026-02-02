<template>
  <div class="profile-page">
    <header class="header">
      <div class="header-content">
        <h1 class="page-title">Profile</h1>
      </div>
    </header>
    <main class="main-content" :class="{ 'main-with-bottom-nav': isMobile() }">
      <div class="card empty-state-card">
        <p class="coming-soon">User settings — coming soon.</p>
      </div>
    </main>
    <MobileBottomNav v-if="isMobile()" />
  </div>
</template>

<script setup lang="ts">
import { useViewport } from '@/composables/useViewport'
import MobileBottomNav from '@/components/layout/MobileBottomNav.vue'

const { isMobile } = useViewport()
</script>

<style scoped>
.profile-page {
  min-height: 100vh;
  background-color: var(--bg-secondary);
}

.header {
  position: sticky;
  top: 0;
  z-index: 40;
  background-color: var(--header-bg);
  border-bottom: 1px solid var(--header-border);
}

.header-content {
  padding: 0.75rem 1rem;
  min-height: 3.5rem;
  display: flex;
  align-items: center;
}

.page-title {
  font-size: 1.125rem;
  font-weight: 600;
  margin: 0;
  color: var(--text-primary);
}

.main-content {
  padding: 1.5rem 1rem;
}

.main-with-bottom-nav {
  padding-bottom: calc(3.5rem + env(safe-area-inset-bottom, 0px) + 2rem);
}

.empty-state-card {
  padding: 2rem;
  text-align: center;
}

.coming-soon {
  color: var(--text-muted);
  margin: 0;
}
</style>
