import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { fetchRoadmap, fetchSeniorityProgress, type RoadmapNode, type RoadmapNodeStatus } from '@/api/client';
import { useAuthStore } from '@/stores';

const PROFILE_NAMES: Record<number, string> = {
  1: 'Java',
  2: '.NET',
  3: 'PO & Analysis',
  4: 'QA',
};

const SENIORITY_NAMES: Record<number, string> = {
  1: 'Junior',
  2: 'Medior',
  3: 'Senior',
};

function statusBadge(status: RoadmapNodeStatus) {
  switch (status) {
    case 'Complete':
      return (
        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
          Complete
        </span>
      );
    case 'Active':
      return (
        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
          Active
        </span>
      );
    case 'Locked':
    default:
      return (
        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">
          Not started
        </span>
      );
  }
}

function NiveauBar({ current, total }: { current: number; total: number }) {
  const pct = total > 0 ? Math.round((current / total) * 100) : 0;
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
        <div className="h-full bg-primary rounded-full transition-all" style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs text-muted-foreground whitespace-nowrap">
        {current}/{total}
      </span>
    </div>
  );
}

function SkillCard({ node }: { node: RoadmapNode }) {
  const [showWarnings, setShowWarnings] = useState(false);
  const hasWarnings = node.prerequisiteWarnings.length > 0;

  return (
    <div className="rounded-lg border bg-card p-4 space-y-3">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-medium text-sm">{node.skillName}</p>
          <p className="text-xs text-muted-foreground">{node.category}</p>
        </div>
        {statusBadge(node.status)}
      </div>

      <NiveauBar current={node.currentNiveau} total={node.levelCount} />

      {node.targetNiveau && (
        <p className="text-xs text-muted-foreground">
          Target: niveau {node.targetNiveau} / {node.levelCount}
        </p>
      )}

      {/* FR8: Non-blocking prerequisite warning — skill is NOT locked */}
      {hasWarnings && (
        <div>
          <button
            type="button"
            onClick={() => setShowWarnings((v) => !v)}
            className="text-xs text-amber-600 underline underline-offset-2 cursor-pointer"
          >
            {showWarnings ? 'Hide' : 'Show'} prerequisite warning{node.prerequisiteWarnings.length > 1 ? 's' : ''}
          </button>
          {showWarnings && (
            <div className="mt-2 space-y-1">
              {node.prerequisiteWarnings.map((w) => (
                <div
                  key={w.skillId}
                  className="text-xs bg-amber-50 border border-amber-200 rounded px-3 py-2 text-amber-800"
                >
                  {w.warningText}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function SeniorityProgressBar({ userId }: { userId: string }) {
  const { t } = useTranslation();
  const { data: progress } = useQuery({
    queryKey: ['seniority-progress', userId],
    queryFn: () => fetchSeniorityProgress(userId),
    staleTime: 10_000,
  });

  if (!progress || progress.requiredCount === 0) return null;

  const targetLevelName = progress.targetLevel ? SENIORITY_NAMES[progress.targetLevel] ?? 'Unknown' : 'N/A';
  const pct = Math.round((progress.metCount / progress.requiredCount) * 100);

  return (
    <div className="rounded-lg border bg-card p-4 space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="font-medium text-sm">Seniority Progress</h3>
        {progress.currentLevel && (
          <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded font-medium">
            {SENIORITY_NAMES[progress.currentLevel]}
          </span>
        )}
      </div>

      {/* FR39: "You meet X/Y [Junior|Medior|Senior] requirements" */}
      <p className="text-sm text-muted-foreground">
        You meet{' '}
        <span className="font-semibold text-foreground">
          {progress.metCount}/{progress.requiredCount}
        </span>{' '}
        {targetLevelName} requirements
      </p>

      <div className="h-2 bg-muted rounded-full overflow-hidden">
        <div className="h-full bg-primary rounded-full transition-all" style={{ width: `${pct}%` }} />
      </div>

      {progress.unmetRequirements.length > 0 && (
        <details className="text-xs">
          <summary className="cursor-pointer text-muted-foreground">
            {progress.unmetRequirements.length} unmet requirement
            {progress.unmetRequirements.length > 1 ? 's' : ''}
          </summary>
          <ul className="mt-2 space-y-1 ml-2">
            {progress.unmetRequirements.map((r) => (
              <li key={r.skillId} className="text-muted-foreground">
                {r.skillName}: need niveau {r.minNiveau} (current: {r.currentNiveau})
              </li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}

export function Roadmap() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const userId = user?.id ?? '';

  // FR14: showAll toggle
  const [showAll, setShowAll] = useState(false);

  const { data: roadmap, isLoading } = useQuery({
    queryKey: ['roadmap', userId, showAll],
    queryFn: () => fetchRoadmap(userId, showAll),
    enabled: !!userId,
    staleTime: 10_000,
  });

  if (isLoading) {
    return <div className="p-6">{t('common.loading')}</div>;
  }

  if (!roadmap || roadmap.nodes.length === 0) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-bold">My Roadmap</h1>
        <div className="rounded-lg border p-8 text-center text-muted-foreground">
          No roadmap available. Ask your coach to assign you to a competence centre profile.
        </div>
      </div>
    );
  }

  const profileName = roadmap.profile ? PROFILE_NAMES[roadmap.profile] ?? 'Unknown' : 'Unknown';

  // Group by category for better visual organisation
  const byCategory = roadmap.nodes.reduce<Record<string, RoadmapNode[]>>((acc, node) => {
    const cat = node.category;
    if (!acc[cat]) acc[cat] = [];
    acc[cat].push(node);
    return acc;
  }, {});

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">My Roadmap</h1>
          <p className="text-muted-foreground mt-1">Profile: {profileName}</p>
        </div>

        {/* FR14: Show all toggle */}
        {roadmap.totalSkillCount > roadmap.nodes.length && !showAll && (
          <button
            type="button"
            onClick={() => setShowAll(true)}
            className="text-sm text-primary underline underline-offset-2"
          >
            Show all {roadmap.totalSkillCount} skills
          </button>
        )}
        {showAll && (
          <button
            type="button"
            onClick={() => setShowAll(false)}
            className="text-sm text-muted-foreground underline underline-offset-2"
          >
            Show fewer skills
          </button>
        )}
      </div>

      {/* FR39: Seniority progress indicator */}
      <SeniorityProgressBar userId={userId} />

      {/* FR13: Skill nodes, grouped by category */}
      {Object.entries(byCategory).map(([category, nodes]) => (
        <div key={category} className="space-y-3">
          <h2 className="text-lg font-semibold">{category}</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {nodes.map((node) => (
              <SkillCard key={node.skillId} node={node} />
            ))}
          </div>
        </div>
      ))}

      {!showAll && roadmap.totalSkillCount > roadmap.nodes.length && (
        <div className="text-center text-muted-foreground text-sm">
          Showing {roadmap.nodes.length} of {roadmap.totalSkillCount} skills.{' '}
          <button
            type="button"
            onClick={() => setShowAll(true)}
            className="text-primary underline underline-offset-2"
          >
            Show all
          </button>
        </div>
      )}
    </div>
  );
}
