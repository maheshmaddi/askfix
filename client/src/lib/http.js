import axios from 'axios'

const http = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: { 'X-Requested-With': 'askfix' },
})

http.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      // session expired — send the SPA back to login
      if (!location.pathname.startsWith('/login')) {
        location.href = '/login?expired=1'
      }
    }
    return Promise.reject(err)
  },
)

export default http
