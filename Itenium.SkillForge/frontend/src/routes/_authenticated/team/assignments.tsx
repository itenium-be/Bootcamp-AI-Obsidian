import { createFileRoute } from '@tanstack/react-router';
import { Assignments } from '@/pages/team/Assignments';

export const Route = createFileRoute('/_authenticated/team/assignments')({
  component: Assignments,
});
