import { createFileRoute } from '@tanstack/react-router';
import { FeedbackReport } from '@/pages/reports/FeedbackReport';

export const Route = createFileRoute('/_authenticated/reports/feedback')({
  component: FeedbackReport,
});
