import { createFileRoute } from '@tanstack/react-router';
import { UserAdmin } from '@/pages/admin/UserAdmin';

export const Route = createFileRoute('/_authenticated/admin/users')({
  component: UserAdmin,
});
