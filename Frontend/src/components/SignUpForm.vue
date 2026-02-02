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
          <h1 class="login-title">Create account</h1>
          <p class="login-subtitle">Sign up to start managing your tasks</p>
        
          <form class="login-form" @submit.prevent="handleSignUp">
            <div class="form-group">
              <label for="name" class="form-label">Full name</label>
              <input
                id="name"
                v-model="name"
                type="text"
                required
                minlength="2"
                maxlength="128"
                class="input"
                placeholder="John Doe"
              />
            </div>

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
                minlength="8"
                class="input"
                placeholder="At least 8 characters"
              />
              <p class="form-hint">Must be at least 8 characters long</p>
            </div>

            <div class="form-group">
              <label for="confirmPassword" class="form-label">Confirm password</label>
              <input
                id="confirmPassword"
                v-model="confirmPassword"
                type="password"
                required
                class="input"
                placeholder="Confirm your password"
              />
            </div>

            <div v-if="error" class="error-message animate-fade-in">
              <AlertCircle class="error-icon" />
              {{ error }}
            </div>

            <button type="submit" :disabled="loading" class="btn btn-primary btn-full">
              <span v-if="loading" class="loading-spinner"></span>
              {{ loading ? 'Creating account...' : 'Create account' }}
            </button>
          </form>

          <p class="signup-link">
            Already have an account? 
            <router-link to="/login" class="link">Sign in</router-link>
          </p>
        </div>
      </div>
      
      <p class="footer-text">
        Built for productivity. Designed for teams.
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { CheckSquare, AlertCircle } from 'lucide-vue-next'

const router = useRouter()
const authStore = useAuthStore()

const name = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')

const loading = ref(false)
const error = ref('')

const handleSignUp = async () => {
  error.value = ''

  // Validate passwords match
  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }

  // Validate password length
  if (password.value.length < 8) {
    error.value = 'Password must be at least 8 characters'
    return
  }

  loading.value = true
  try {
    const result = await authStore.register(email.value, name.value, password.value)
    if (result.success) {
      router.push('/dashboard')
    } else {
      error.value = result.message || 'Registration failed'
    }
  } catch (err) {
    error.value = 'Registration failed. Please try again.'
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

.form-hint {
  font-size: 0.75rem;
  color: var(--text-muted);
  margin: 0;
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

.signup-link {
  margin-top: 1.5rem;
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

.footer-text {
  margin-top: 1.5rem;
  font-size: 0.8125rem;
  color: var(--text-muted);
}
</style>
