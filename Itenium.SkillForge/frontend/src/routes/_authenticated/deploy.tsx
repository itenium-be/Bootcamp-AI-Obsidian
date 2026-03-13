import { createFileRoute } from '@tanstack/react-router';
import { DeployPanic } from '@/pages/DeployPanic';

export const Route = createFileRoute('/_authenticated/deploy')({
  component: DeployPanic,
});
