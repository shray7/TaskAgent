<template>
  <div class="login-page">
    <div class="login-bg">
      <div class="login-bg-shapes">
        <div class="shape shape-1" aria-hidden="true"></div>
        <div class="shape shape-2" aria-hidden="true"></div>
        <div class="shape shape-3" aria-hidden="true"></div>
        <div class="shape shape-4" aria-hidden="true"></div>
        <div class="shape shape-5" aria-hidden="true"></div>
        <div class="shape shape-6" aria-hidden="true"></div>
        <div class="shape shape-7" aria-hidden="true"></div>
      </div>
    </div>
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
import ThemeToggle from '@/components/ui/ThemeToggle.vue'
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
  position: relative;
  overflow: hidden;
}

.login-bg {
  position: absolute;
  inset: 0;
  background: var(--login-gradient);
  z-index: 0;
}

.login-bg-shapes {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.shape {
  position: absolute;
  border-radius: 50%;
  filter: blur(50px);
  animation: float 18s ease-in-out infinite;
}

/* Light theme - warmer peachy/amber accents for contrast */
.shape-1 {
  width: 380px;
  height: 380px;
  background: radial-gradient(circle at 30% 30%, rgba(251, 191, 36, 0.35) 0%, transparent 65%);
  top: -15%;
  left: -10%;
  animation-delay: 0s;
}

.shape-2 {
  width: 300px;
  height: 300px;
  background: radial-gradient(circle at 50% 50%, rgba(217, 119, 6, 0.25) 0%, transparent 60%);
  top: 15%;
  right: -5%;
  animation-delay: -3s;
}

.shape-3 {
  width: 280px;
  height: 280px;
  background: radial-gradient(circle at 40% 40%, rgba(180, 83, 9, 0.3) 0%, transparent 60%);
  bottom: 5%;
  left: -5%;
  animation-delay: -6s;
}

.shape-4 {
  width: 240px;
  height: 240px;
  background: radial-gradient(circle at 60% 60%, rgba(251, 191, 36, 0.3) 0%, transparent 60%);
  bottom: -8%;
  right: 10%;
  animation-delay: -9s;
}

.shape-5 {
  width: 200px;
  height: 200px;
  background: radial-gradient(circle at 50% 50%, rgba(217, 119, 6, 0.2) 0%, transparent 55%);
  top: 45%;
  left: 25%;
  animation-delay: -2s;
}

.shape-6 {
  width: 220px;
  height: 220px;
  background: radial-gradient(circle at 50% 50%, rgba(251, 191, 36, 0.25) 0%, transparent 55%);
  top: 30%;
  right: 20%;
  animation-delay: -5s;
}

.shape-7 {
  width: 180px;
  height: 180px;
  background: radial-gradient(circle at 50% 50%, rgba(180, 83, 9, 0.25) 0%, transparent 55%);
  bottom: 30%;
  left: 45%;
  animation-delay: -8s;
}

/* Dark theme */
:root.dark .shape-1 {
  background: radial-gradient(circle at 30% 30%, rgba(251, 191, 36, 0.2) 0%, transparent 65%);
}

:root.dark .shape-2 {
  background: radial-gradient(circle at 50% 50%, rgba(217, 119, 6, 0.15) 0%, transparent 60%);
}

:root.dark .shape-3 {
  background: radial-gradient(circle at 40% 40%, rgba(251, 191, 36, 0.18) 0%, transparent 60%);
}

:root.dark .shape-4 {
  background: radial-gradient(circle at 60% 60%, rgba(180, 83, 9, 0.2) 0%, transparent 60%);
}

:root.dark .shape-5 {
  background: radial-gradient(circle at 50% 50%, rgba(217, 119, 6, 0.12) 0%, transparent 55%);
}

:root.dark .shape-6 {
  background: radial-gradient(circle at 50% 50%, rgba(251, 191, 36, 0.15) 0%, transparent 55%);
}

:root.dark .shape-7 {
  background: radial-gradient(circle at 50% 50%, rgba(180, 83, 9, 0.15) 0%, transparent 55%);
}

@keyframes float {
  0%, 100% { transform: translate(0, 0) scale(1); }
  25% { transform: translate(15px, -20px) scale(1.02); }
  50% { transform: translate(-10px, 15px) scale(0.98); }
  75% { transform: translate(-20px, -10px) scale(1.01); }
}

.login-container {
  width: 100%;
  max-width: 26rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
  z-index: 1;
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
