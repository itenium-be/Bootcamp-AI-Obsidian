import { createFileRoute, redirect } from '@tanstack/react-router';
import { useAuthStore } from '@/stores';
import { AdminUsers } from '@/pages/AdminUsers';

export const Route = createFileRoute('/_authenticated/admin/users')({
  component: AdminUsers,
  beforeLoad: () => {
    const { user } = useAuthStore.getState();
    if (!user?.isBackOffice) {
      throw redirect({ to: '/' });
    }
  },
});
