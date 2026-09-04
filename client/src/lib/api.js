import http from './http'

// ---- auth ------------------------------------------------------------------------------
export const apiInfo = () => http.get('/auth/info').then(r => r.data)
export const login = (username, password) => http.post('/auth/login', { username, password }).then(r => r.data)
export const logout = () => http.post('/auth/logout')
export const me = () => http.get('/auth/me').then(r => r.data)
export const updateMe = (bio) => http.put('/auth/me', { bio }).then(r => r.data)

// ---- feed ------------------------------------------------------------------------------
export const getFeed = ({ tab = 'latest', tag, page = 1, pageSize = 15 }) =>
  http.get('/feed', { params: { tab, tag, page, pageSize } }).then(r => r.data)

// ---- questions -------------------------------------------------------------------------
export const getQuestion = (id) => http.get(`/questions/${id}`).then(r => r.data)
export const createQuestion = (payload) => http.post('/questions', payload).then(r => r.data)
export const updateQuestion = (id, payload) => http.put(`/questions/${id}`, payload).then(r => r.data)
export const deleteQuestion = (id) => http.delete(`/questions/${id}`)
export const similarQuestions = (q) => http.get('/questions/similar', { params: { q } }).then(r => r.data)
export const toggleFollow = (id) => http.post(`/questions/${id}/follow`).then(r => r.data)
export const toggleBookmark = (id) => http.post(`/questions/${id}/bookmark`).then(r => r.data)
export const relatedQuestions = (id) => http.get(`/questions/${id}/related`).then(r => r.data)

// ---- answers ---------------------------------------------------------------------------
export const getAnswers = (id, sort = 'top', page = 1) =>
  http.get(`/questions/${id}/answers`, { params: { sort, page } }).then(r => r.data)
export const createAnswer = (questionId, bodyHtml) =>
  http.post(`/questions/${questionId}/answers`, { bodyHtml }).then(r => r.data)
export const updateAnswer = (id, bodyHtml) => http.put(`/answers/${id}`, { bodyHtml }).then(r => r.data)
export const deleteAnswer = (id) => http.delete(`/answers/${id}`)
export const voteAnswer = (id, value) => http.post(`/answers/${id}/vote`, { value }).then(r => r.data)
export const acceptAnswer = (id) => http.post(`/answers/${id}/accept`).then(r => r.data)

// ---- comments --------------------------------------------------------------------------
export const getComments = (answerId) => http.get(`/answers/${answerId}/comments`).then(r => r.data)
export const addComment = (answerId, body) =>
  http.post(`/answers/${answerId}/comments`, { body }).then(r => r.data)
export const deleteComment = (id) => http.delete(`/answers/comments/${id}`)

// ---- tags ------------------------------------------------------------------------------
export const getTags = (sort = 'popular', take = 100) => http.get('/tags', { params: { sort, take } }).then(r => r.data)
export const getTag = (slug) => http.get(`/tags/${slug}`).then(r => r.data)

// ---- users -----------------------------------------------------------------------------
export const getProfile = (id) => http.get(`/users/${id}`).then(r => r.data)
export const getUserQuestions = (id) => http.get(`/users/${id}/questions`).then(r => r.data)
export const getUserAnswers = (id) => http.get(`/users/${id}/answers`).then(r => r.data)
export const getMyBookmarks = () => http.get('/users/me/bookmarks').then(r => r.data)

// ---- notifications ---------------------------------------------------------------------
export const getNotifications = ({ unreadOnly = false, page = 1 } = {}) =>
  http.get('/notifications', { params: { unreadOnly, page } }).then(r => r.data)
export const unreadCount = () => http.get('/notifications/unread-count').then(r => r.data)
export const markAllRead = () => http.post('/notifications/read-all')
export const markRead = (id) => http.post(`/notifications/${id}/read`)

// ---- search & misc ---------------------------------------------------------------------
export const search = (q, take = 20) => http.get('/search', { params: { q, take } }).then(r => r.data)
export const getStats = () => http.get('/stats').then(r => r.data)
export const uploadImage = (file) => {
  const form = new FormData()
  form.append('file', file)
  return http.post('/uploads', form, { headers: { 'Content-Type': 'multipart/form-data' } }).then(r => r.data)
}

// ---- settings (notification preferences) -------------------------------------------------
export const getNotifPrefs = () => http.get('/settings/notifications').then(r => r.data)
export const saveNotifPrefs = (prefs) => http.put('/settings/notifications', prefs).then(r => r.data)

// ---- admin ------------------------------------------------------------------------------
export const adminStats = () => http.get('/admin/stats').then(r => r.data)
export const adminUsers = ({ query = '', page = 1, pageSize = 20 } = {}) =>
  http.get('/admin/users', { params: { query, page, pageSize } }).then(r => r.data)
export const adminToggleAdmin = (id) => http.post(`/admin/users/${id}/toggle-admin`).then(r => r.data)
export const adminTags = () => http.get('/admin/tags').then(r => r.data)
export const adminUpdateTag = (id, payload) => http.put(`/admin/tags/${id}`, payload).then(r => r.data)
export const adminMergeTag = (id, targetTagId) => http.post(`/admin/tags/${id}/merge`, { targetTagId }).then(r => r.data)
export const adminDeleteTag = (id) => http.delete(`/admin/tags/${id}`)
export const adminContent = ({ type = 'question', query = '', page = 1 } = {}) =>
  http.get('/admin/content', { params: { type, query, page } }).then(r => r.data)
export const getEmailSettings = () => http.get('/admin/email-settings').then(r => r.data)
export const saveEmailSettings = (payload) => http.put('/admin/email-settings', payload).then(r => r.data)
export const testEmailSettings = () => http.post('/admin/email-settings/test').then(r => r.data)

export const errorMessage = (err, fallback = 'Something went wrong. Please try again.') =>
  err?.response?.data?.message || fallback
