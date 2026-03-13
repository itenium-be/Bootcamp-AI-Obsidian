import { createFileRoute } from '@tanstack/react-router';
import { TeamMembers } from '@/pages/team/TeamMembers';

export const Route = createFileRoute('/_authenticated/team/members')({
  component: TeamMembers,
});
