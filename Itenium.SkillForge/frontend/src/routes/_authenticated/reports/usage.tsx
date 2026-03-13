import { createFileRoute } from '@tanstack/react-router';
import { UsageReport } from '@/pages/reports/UsageReport';

export const Route = createFileRoute('/_authenticated/reports/usage')({
  component: UsageReport,
});
