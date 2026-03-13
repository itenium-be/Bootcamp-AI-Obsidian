import { createFileRoute } from '@tanstack/react-router';
import { MyProgress } from '@/pages/MyProgress';

export const Route = createFileRoute('/_authenticated/my-progress')({ component: MyProgress });
