import { createFileRoute, Outlet, redirect } from '@tanstack/react-router';
import { useAuthStore } from '@/stores';

export const Route = createFileRoute('/_authenticated/admin')({
  beforeLoad: () => {
    const { user } = useAuthStore.getState();
    if (!user?.isBackOffice) {
      throw redirect({ to: '/' });
    }
  },
  component: () => <Outlet />,
});
