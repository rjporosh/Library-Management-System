'use client'

import { useAuth } from '@/context/authContextValue'
import BorrowRequestsPage from '@/views/BorrowRequestsPage'
import MyRequestsPage from '@/views/MyRequestsPage'

export default function Page() {
  const { isLibrarian } = useAuth()
  return isLibrarian ? <BorrowRequestsPage /> : <MyRequestsPage />
}
