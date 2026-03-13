import { createFileRoute } from '@tanstack/react-router';
import { Roadmap } from '@/pages/Roadmap';

export const Route = createFileRoute('/_authenticated/my-progress')({
  component: Roadmap,
});
