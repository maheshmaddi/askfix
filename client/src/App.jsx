import { Routes, Route, Navigate, useLocation } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider, useAuth } from './store/auth'
import AppLayout from './components/layout/AppLayout'
import LoginPage from './pages/LoginPage'
import FeedPage from './pages/FeedPage'
import AskPage from './pages/AskPage'
import QuestionPage from './pages/QuestionPage'
import ProfilePage from './pages/ProfilePage'
import SearchPage from './pages/SearchPage'
import { TagsPage, TagDetailPage } from './pages/TagsPage'
import NotificationsPage from './pages/NotificationsPage'
import BookmarksPage from './pages/BookmarksPage'
import AdminPage from './pages/AdminPage'
import SettingsPage from './pages/SettingsPage'
import NotFoundPage from './pages/NotFoundPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, refetchOnWindowFocus: false, staleTime: 30_000 },
  },
})

function Splash() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-canvas">
      <div className="flex flex-col items-center gap-4">
        <span className="w-12 h-12 rounded-2xl bg-gradient-to-br from-brand-dark to-brand-violet flex items-center justify-center animate-pulse">
          <svg viewBox="0 0 24 24" className="w-7 h-7" fill="#fff" aria-hidden="true">
            <path d="M11.2 5.2 16.8 18.4h-2.4l-1.2-3h-4.4l-1.2 3H5.2L10.8 5.2Zm-.5 7.9h2.6L12 9.7Z" />
            <circle cx="17.2" cy="6.6" r="1.55" />
          </svg>
        </span>
        <span className="text-ink-faint text-[13.5px]">Loading AskFix…</span>
      </div>
    </div>
  )
}

function Protected({ children }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()
  if (isLoading) return <Splash />
  if (!user) return <Navigate to={`/login?next=${encodeURIComponent(location.pathname)}`} replace />
  return children
}

function AdminRoute({ children }) {
  const { user, isLoading, isAdmin } = useAuth()
  if (isLoading) return <Splash />
  if (!user) return <Navigate to="/login?next=%2Fadmin" replace />
  if (!isAdmin) return <Navigate to="/" replace />
  return children
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<AppLayout />}>
            <Route path="/" element={<Protected><FeedPage /></Protected>} />
            <Route path="/ask" element={<Protected><AskPage /></Protected>} />
            <Route path="/question/:id" element={<Protected><QuestionPage /></Protected>} />
            <Route path="/tag/:slug" element={<Protected><TagDetailPage /></Protected>} />
            <Route path="/tags" element={<Protected><TagsPage /></Protected>} />
            <Route path="/profile/:id" element={<Protected><ProfilePage /></Protected>} />
            <Route path="/search" element={<Protected><SearchPage /></Protected>} />
            <Route path="/notifications" element={<Protected><NotificationsPage /></Protected>} />
            <Route path="/bookmarks" element={<Protected><BookmarksPage /></Protected>} />
            <Route path="/settings" element={<Protected><SettingsPage /></Protected>} />
            <Route path="/admin" element={<AdminRoute><AdminPage /></AdminRoute>} />
            <Route path="*" element={<Protected><NotFoundPage /></Protected>} />
          </Route>
        </Routes>
      </AuthProvider>
    </QueryClientProvider>
  )
}
