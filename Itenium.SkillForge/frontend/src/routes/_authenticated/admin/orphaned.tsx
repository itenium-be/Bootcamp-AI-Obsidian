import { createFileRoute } from '@tanstack/react-router';
import { OrphanedConsultants } from '@/pages/OrphanedConsultants';

export const Route = createFileRoute('/_authenticated/admin/orphaned')({
  component: OrphanedConsultants,
});
