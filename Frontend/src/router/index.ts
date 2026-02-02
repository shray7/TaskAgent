import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import DashboardView from '@/features/dashboard/views/DashboardView.vue'
import MyTasksView from '@/features/tasks/views/MyTasksView.vue'
import ProjectSettingsView from '@/features/dashboard/views/ProjectSettingsView.vue'
import ProfileView from '@/features/auth/views/ProfileView.vue'
import LoginView from '@/features/auth/views/LoginView.vue'
import SignUpView from '@/features/auth/views/SignUpView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/dashboard'
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { requiresGuest: true }
    },
    {
      path: '/signup',
      name: 'signup',
      component: SignUpView,
      meta: { requiresGuest: true }
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: DashboardView,
      meta: { requiresAuth: true }
    },
    {
      path: '/project-settings',
      name: 'project-settings',
      component: ProjectSettingsView,
      meta: { requiresAuth: true }
    },
    {
      path: '/my-tasks',
      name: 'my-tasks',
      component: MyTasksView,
      meta: { requiresAuth: true }
    },
    {
      path: '/profile',
      name: 'profile',
      component: ProfileView,
      meta: { requiresAuth: true }
    }
  ],
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if (to.meta.requiresGuest && authStore.isAuthenticated) {
    next('/dashboard')
  } else {
    next()
  }
})

export default router
