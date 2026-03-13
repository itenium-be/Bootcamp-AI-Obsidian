import { createFileRoute } from '@tanstack/react-router';
import { IteniumDino } from '@/pages/IteniumDino';

export const Route = createFileRoute('/_authenticated/itenium-dino')({
  component: IteniumDino,
});
