import { createFileRoute } from '@tanstack/react-router';
import { Skills } from '@/pages/Skills';

export const Route = createFileRoute('/_authenticated/skills')({
  component: Skills,
});
