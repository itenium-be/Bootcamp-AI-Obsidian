import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fetchRoadmap, fetchSeniority, updateSkillProgress } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import type { RoadmapSkillItem } from '@/api/client';

function SeniorityBar({ userId }: { userId: string }) {
  const { t } = useTranslation();

  const { data: seniority } = useQuery({
    queryKey: ['seniority', userId],
    queryFn: () => fetchSeniority(userId),
    enabled: !!userId,
  });

  if (!seniority) return null;

  return (
    <div className="flex flex-wrap gap-4 rounded-md border bg-muted/50 p-4">
      <span className="font-medium text-sm">{t('seniority.progress')}:</span>
      {seniority.map((item) => (
        <span key={item.level} className="text-sm">
          <span className="font-medium">{t(`seniority.${item.level.toLowerCase()}`)}</span>
          {': '}
          <span className={item.met >= item.required && item.required > 0 ? 'text-green-600' : ''}>
            {t('seniority.meets', { met: item.met, required: item.required })}
          </span>
        </span>
      ))}
    </div>
  );
}

function SkillCard({
  skill,
  onLevelChange,
}: {
  skill: RoadmapSkillItem;
  onLevelChange: (skillId: number, level: number) => void;
}) {
  const { t } = useTranslation();

  const levels = Array.from({ length: skill.levelCount }, (_, i) => i + 1);

  return (
    <div className="rounded-md border p-4 space-y-2">
      <div className="flex items-start justify-between gap-2">
        <div>
          <h3 className="font-semibold">{skill.name}</h3>
          {skill.category && <span className="text-xs text-muted-foreground">{skill.category}</span>}
        </div>
        <span className="text-sm text-muted-foreground whitespace-nowrap">
          {skill.achievedLevel}/{skill.levelCount}
        </span>
      </div>

      {skill.levelCount > 1 && (
        <div className="flex gap-1">
          {levels.map((lvl) => (
            <button
              key={lvl}
              onClick={() => onLevelChange(skill.skillId, lvl === skill.achievedLevel ? lvl - 1 : lvl)}
              className={`w-8 h-8 rounded text-xs font-medium transition-colors ${
                lvl <= skill.achievedLevel
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground hover:bg-muted/80'
              }`}
              aria-label={`Level ${lvl}`}
            >
              {lvl}
            </button>
          ))}
        </div>
      )}

      {skill.levelCount === 1 && (
        <button
          onClick={() => onLevelChange(skill.skillId, skill.achievedLevel === 0 ? 1 : 0)}
          className={`px-3 py-1 rounded text-xs font-medium transition-colors ${
            skill.achievedLevel >= 1
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted text-muted-foreground hover:bg-muted/80'
          }`}
        >
          {skill.achievedLevel >= 1 ? '✓ Done' : 'Mark done'}
        </button>
      )}

      {skill.unmetPrerequisites.length > 0 && (
        <div className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded p-2 space-y-1">
          {skill.unmetPrerequisites.map((prereq) => (
            <p key={prereq.skillName}>
              ⚠{' '}
              {t('roadmap.prerequisiteWarning', {
                name: prereq.skillName,
                required: prereq.requiredLevel,
                current: prereq.currentLevel,
              })}
            </p>
          ))}
        </div>
      )}
    </div>
  );
}

export function Roadmap() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const user = useAuthStore((s) => s.user);
  const userId = user?.id ?? '';

  const [showAll, setShowAll] = useState(false);

  const { data: roadmap, isLoading } = useQuery({
    queryKey: ['roadmap', userId, showAll],
    queryFn: () => fetchRoadmap(userId, showAll),
    enabled: !!userId,
  });

  const progressMutation = useMutation({
    mutationFn: ({ skillId, level }: { skillId: number; level: number }) => updateSkillProgress(userId, skillId, level),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['roadmap', userId] });
      void queryClient.invalidateQueries({ queryKey: ['seniority', userId] });
    },
  });

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  const skills = roadmap?.skills ?? [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('roadmap.title')}</h1>
        <button onClick={() => setShowAll((v) => !v)} className="text-sm underline text-primary">
          {showAll ? t('roadmap.showLess') : t('roadmap.showAll')}
        </button>
      </div>

      {userId && <SeniorityBar userId={userId} />}

      {skills.length === 0 && <p className="text-muted-foreground">{t('roadmap.noProfile')}</p>}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {skills.map((skill) => (
          <SkillCard
            key={skill.skillId}
            skill={skill}
            onLevelChange={(skillId, level) => progressMutation.mutate({ skillId, level })}
          />
        ))}
      </div>
    </div>
  );
}
