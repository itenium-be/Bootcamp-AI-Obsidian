import { createFileRoute } from '@tanstack/react-router';
import { MyCourses } from '@/pages/MyCourses';

export const Route = createFileRoute('/_authenticated/my-courses')({ component: MyCourses });
