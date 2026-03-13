import { createFileRoute } from '@tanstack/react-router';
import { TeamAdmin } from '@/pages/admin/TeamAdmin';

export const Route = createFileRoute('/_authenticated/admin/teams')({
  component: TeamAdmin,
});
