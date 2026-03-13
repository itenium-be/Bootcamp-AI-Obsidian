import { createFileRoute } from '@tanstack/react-router';
import { TeamProgress } from '@/pages/team/TeamProgress';

export const Route = createFileRoute('/_authenticated/team/progress')({
  component: TeamProgress,
});
