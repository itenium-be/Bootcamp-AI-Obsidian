import { createFileRoute } from '@tanstack/react-router';
import { LiveSession } from '@/pages/LiveSession';

export const Route = createFileRoute('/_authenticated/session/$consultantId')({
  component: LiveSessionRoute,
});

function LiveSessionRoute() {
  const { consultantId } = Route.useParams();
  return <LiveSession consultantId={consultantId} />;
}
