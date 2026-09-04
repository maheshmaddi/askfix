import { Outlet } from 'react-router-dom'
import Navbar from './Navbar'
import { LeftSidebar, RightSidebar, MobileTabBar } from './Sidebars'

export default function AppLayout({ withRightRail = true }) {
  return (
    <div className="min-h-screen pb-16 lg:pb-0">
      <Navbar />
      <div className="mx-auto max-w-[1280px] px-4 flex gap-8 pt-6">
        <LeftSidebar />
        <main className="flex-1 min-w-0 max-w-[720px] fade-up">
          <Outlet />
        </main>
        {withRightRail && <RightSidebar />}
      </div>
      <MobileTabBar />
    </div>
  )
}
