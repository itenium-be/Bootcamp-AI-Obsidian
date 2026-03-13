import { createFileRoute } from '@tanstack/react-router';
import { SkillCatalogue } from '@/pages/SkillCatalogue';

export const Route = createFileRoute('/_authenticated/catalog')({
  component: SkillCatalogue,
});
