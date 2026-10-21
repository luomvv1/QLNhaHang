import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import './assets/custom.css'
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import axios from 'axios'

axios.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

const app = createApp(App)
app.use(router)
app.mount('#app')
