import { createFileRoute } from '@tanstack/react-router';
import { MyCertificates } from '@/pages/MyCertificates';

export const Route = createFileRoute('/_authenticated/my-certificates')({ component: MyCertificates });
