import { createFileRoute } from '@tanstack/react-router';
import { CompletionReport } from '@/pages/reports/CompletionReport';

export const Route = createFileRoute('/_authenticated/reports/completion')({
  component: CompletionReport,
});
