import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'

import App from './App.vue'
import router from './router'
import { setTokenGetter, setUnauthorizedCallback } from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const app = createApp(App)
const pinia = createPinia()
pinia.use(piniaPluginPersistedstate)

app.use(pinia)
setTokenGetter(() => useAuthStore().token ?? null)
setUnauthorizedCallback(() => {
  const auth = useAuthStore()
  auth.logout()
})
app.use(router)
app.mount('#app')
