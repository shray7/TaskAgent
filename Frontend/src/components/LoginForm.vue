<template>
  <div class="login-page">
    <div class="login-container">
      <div class="login-card card animate-slide-in">
        <div class="login-header">
          <div class="brand">
            <div class="brand-icon">
              <CheckSquare class="brand-icon-svg" />
            </div>
            <span class="brand-name">TaskAgent</span>
          </div>
          <ThemeToggle />
        </div>
        
        <div class="login-content">
          <h1 class="login-title">Welcome back</h1>
          <p class="login-subtitle">Sign in to manage your tasks</p>
        
          <form class="login-form" @submit.prevent="handleLogin">
            <div class="form-group">
              <label for="email" class="form-label">Email address</label>
              <input
                id="email"
                v-model="email"
                type="email"
                required
                class="input"
                placeholder="you@example.com"
              />
            </div>

            <div class="form-group">
              <label for="password" class="form-label">Password</label>
              <input
                id="password"
                v-model="password"
                type="password"
                required
                class="input"
                placeholder="Enter your password"
              />
            </div>

            <div v-if="error" class="error-message animate-fade-in">
              <AlertCircle class="error-icon" />
              {{ error }}
            </div>

            <button type="submit" :disabled="loading" class="btn btn-primary btn-full">
              <span v-if="loading" class="loading-spinner"></span>
              {{ loading ? 'Signing in...' : 'Sign in' }}
            </button>
          </form>

          <p class="signup-link">
            Don't have an account? 
            <router-link to="/signup" class="link">Sign up</router-link>
          </p>

          <div class="divider">
            <span class="divider-text">Demo accounts (password: password123)</span>
          </div>

          <div class="demo-accounts">
            <button
              v-for="user in authStore.users"
              :key="user.id"
              type="button"
              @click="fillDemoAccount(user.email)"
              class="demo-account"
            >
              <span class="demo-avatar" :style="{ backgroundColor: getUserColor(user.id), color: '#fff' }">{{ getInitials(user) }}</span>
              <div class="demo-info">
                <span class="demo-name">{{ user.name }}</span>
                <span class="demo-email">{{ user.email }}</span>
              </div>
              <ArrowRight class="demo-arrow" />
            </button>
          </div>
        </div>
      </div>
      
      <p class="footer-text">
        Built for productivity. Designed for teams.
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { CheckSquare, AlertCircle, ArrowRight } from 'lucide-vue-next'
import { getInitials, getUserColor } from '@/utils/initials'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')

onMounted(() => {
  authStore.loadUsers()
})

const loading = ref(false)
const error = ref('')

function fillDemoAccount(userEmail: string) {
  email.value = userEmail
  password.value = 'password123'
}

const handleLogin = async () => {
  loading.value = true
  error.value = ''
  try {
    const result = await authStore.login(email.value, password.value)
    if (result.success) {
      router.push('/dashboard')
    } else {
      error.value = result.message || 'Invalid email or password'
    }
  } catch (err) {
    error.value = 'Login failed. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  background: var(--login-gradient);
}

.login-container {
  width: 100%;
  max-width: 26rem;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.login-card {
  width: 100%;
  overflow: hidden;
}

.login-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid var(--border-primary);
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.625rem;
}

.brand-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background-color: var(--btn-primary-bg);
  border-radius: 0.5rem;
}

.brand-icon-svg {
  width: 1.125rem;
  height: 1.125rem;
  color: var(--btn-primary-text);
}

.brand-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: -0.02em;
}

.login-content {
  padding: 2rem 1.5rem;
}

.login-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: -0.03em;
}

.login-subtitle {
  margin-top: 0.5rem;
  font-size: 0.9375rem;
  color: var(--text-secondary);
}

.login-form {
  margin-top: 1.75rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-secondary);
}

.error-message {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background-color: var(--badge-red-bg);
  color: var(--badge-red-text);
  border-radius: 0.75rem;
  font-size: 0.875rem;
}

.error-icon {
  width: 1rem;
  height: 1rem;
  flex-shrink: 0;
}

.btn-full {
  width: 100%;
  padding: 0.875rem 1.5rem;
  font-size: 0.9375rem;
}

.signup-link {
  margin-top: 1rem;
  text-align: center;
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.link {
  color: var(--color-brown-500);
  font-weight: 500;
  text-decoration: none;
}

.link:hover {
  text-decoration: underline;
}

.loading-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid transparent;
  border-top-color: currentColor;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.divider {
  display: flex;
  align-items: center;
  margin: 1.75rem 0;
  gap: 1rem;
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background-color: var(--border-primary);
}

.divider-text {
  font-size: 0.8125rem;
  color: var(--text-muted);
  font-weight: 500;
}

.demo-accounts {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.demo-account {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.75rem;
  background-color: var(--bg-secondary);
  border: 1px solid var(--border-primary);
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.15s ease;
  text-align: left;
}

.demo-account:hover {
  background-color: var(--bg-hover);
  border-color: var(--border-secondary);
}

.demo-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 2.25rem;
  height: 2.25rem;
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1;
  background-color: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: 0.375rem;
}

.demo-info {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.demo-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-primary);
}

.demo-email {
  font-size: 0.8125rem;
  color: var(--text-tertiary);
}

.demo-arrow {
  width: 1rem;
  height: 1rem;
  color: var(--text-muted);
  transition: transform 0.15s ease;
}

.demo-account:hover .demo-arrow {
  transform: translateX(2px);
  color: var(--text-secondary);
}

.footer-text {
  margin-top: 1.5rem;
  font-size: 0.8125rem;
  color: var(--text-muted);
}
</style>
