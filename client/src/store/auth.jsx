import { createContext, useContext } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { me } from '../lib/api'

const AuthContext = createContext({ user: null, isLoading: true, isAdmin: false })

export function AuthProvider({ children }) {
  const queryClient = useQueryClient()
  const { data: user, isLoading } = useQuery({
    queryKey: ['me'],
    queryFn: me,
    retry: false,
    staleTime: 10 * 60 * 1000,
  })

  const value = {
    user: user ?? null,
    isLoading,
    isAdmin: user?.isAdmin ?? false,
    refresh: () => queryClient.invalidateQueries({ queryKey: ['me'] }),
  }
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)
