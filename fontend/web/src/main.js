import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import './assets/custom.css'
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import axios from 'axios'
import { clearSession } from './auth/session'

axios.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  const target = new URL(config.url, config.baseURL || window.location.origin)
  const apiOrigin = new URL(import.meta.env.VITE_API_URL || 'http://localhost:5079').origin
  if (token && target.origin === apiOrigin && !config.headers.Authorization)
    config.headers.Authorization = `Bearer ${token}`
  return config
})

axios.interceptors.response.use(response => response, error => {
  const token = localStorage.getItem('token')
  if (error.response?.status === 401 && token && error.config?.headers?.Authorization === `Bearer ${token}`
      && !error.config.url.toLowerCase().includes('/auth/login')) {
    clearSession()
    router.replace({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
  }
  return Promise.reject(error)
})

const app = createApp(App)
app.use(router)
app.mount('#app')
