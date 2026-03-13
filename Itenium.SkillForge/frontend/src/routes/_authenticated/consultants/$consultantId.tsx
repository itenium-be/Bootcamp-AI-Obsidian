import { createFileRoute } from '@tanstack/react-router';
import { ConsultantProfile } from '@/pages/ConsultantProfile';

export const Route = createFileRoute('/_authenticated/consultants/$consultantId')({
  component: function ConsultantProfileRoute() {
    const { consultantId } = Route.useParams();
    return <ConsultantProfile consultantId={consultantId} />;
  },
});
