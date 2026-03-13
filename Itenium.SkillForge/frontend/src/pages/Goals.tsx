import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Flag, CheckCircle, Target, Calendar, Sparkles } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent, Badge, Button } from '@itenium-forge/ui';
import { toast } from 'sonner';
import { useAuthStore } from '@/stores';
import { fetchMyGoals, raiseReadinessFlag, lowerReadinessFlag, type Goal } from '@/api/client';

const ONBOARDING_KEY = 'skillforge-onboarding-dismissed';

function OnboardingModal({ goals, onDismiss }: { goals: Goal[]; onDismiss: () => void }) {
  const { t } = useTranslation();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-background rounded-lg border shadow-lg w-full max-w-md mx-4 p-6 space-y-4">
        <div className="flex items-center gap-2">
          <Sparkles className="size-5 text-primary" />
          <h2 className="text-xl font-semibold">{t('onboarding.title')}</h2>
        </div>
        <p className="text-muted-foreground">{t('onboarding.subtitle')}</p>
        {goals.length > 0 && (
          <div className="space-y-2">
            <p className="text-sm font-medium">{t('onboarding.goalsReady', { count: goals.length })}</p>
            <ul className="space-y-1">
              {goals.slice(0, 5).map((goal) => (
                <li key={goal.id} className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Target className="size-3 text-primary shrink-0" />
                  {goal.skill?.name ?? `Skill #${goal.skillId}`}
                </li>
              ))}
              {goals.length > 5 && (
                <li className="text-xs text-muted-foreground pl-5">
                  {t('onboarding.andMore', { count: goals.length - 5 })}
                </li>
              )}
            </ul>
          </div>
        )}
        <div className="flex justify-end pt-2">
          <Button onClick={onDismiss}>{t('onboarding.getStarted')}</Button>
        </div>
      </div>
    </div>
  );
}

function GoalProgressBar({ current, target, max }: { current: number; target: number; max: number }) {
  const pct = max > 0 ? Math.round((current / max) * 100) : 0;
  const targetPct = max > 0 ? Math.round((target / max) * 100) : 0;

  return (
    <div className="space-y-1">
      <div className="flex justify-between text-xs text-muted-foreground">
        <span>Niveau {current}</span>
        <span>Target: {target}</span>
        <span>Max: {max}</span>
      </div>
      <div className="relative h-2 rounded-full bg-muted">
        {/* Target marker */}
        <div
          className="absolute top-0 h-2 w-0.5 bg-primary/50"
          style={{ left: `${targetPct}%` }}
        />
        {/* Progress fill */}
        <div
          className="h-2 rounded-full bg-primary transition-all"
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}

function GoalCard({ goal, onRaiseFlag, onLowerFlag }: {
  goal: Goal;
  onRaiseFlag: (goalId: string) => void;
  onLowerFlag: (goalId: string) => void;
}) {
  const { t } = useTranslation();
  const now = new Date();
  const deadline = new Date(goal.deadline);
  const isOverdue = deadline < now;
  const daysLeft = Math.ceil((deadline.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between pb-2">
        <div>
          <CardTitle className="text-base">{goal.skill?.name ?? `Skill #${goal.skillId}`}</CardTitle>
          <p className="text-xs text-muted-foreground">{goal.skill?.category}</p>
        </div>
        <Badge variant={isOverdue ? 'destructive' : 'secondary'}>
          {isOverdue ? t('goals.overdue') : `${daysLeft}d ${t('goals.left')}`}
        </Badge>
      </CardHeader>
      <CardContent className="space-y-4">
        {goal.skill && (
          <GoalProgressBar
            current={goal.currentNiveau}
            target={goal.targetNiveau}
            max={goal.skill.levelCount}
          />
        )}

        <div className="flex items-center gap-4 text-sm text-muted-foreground">
          <span className="flex items-center gap-1">
            <Target className="size-3" />
            {goal.currentNiveau} → {goal.targetNiveau}
          </span>
          <span className="flex items-center gap-1">
            <Calendar className="size-3" />
            {deadline.toLocaleDateString()}
          </span>
        </div>

        <div className="flex gap-2">
          <Button
            size="sm"
            variant="outline"
            onClick={() => onRaiseFlag(goal.id)}
            className="flex items-center gap-1"
          >
            <Flag className="size-3" />
            {t('goals.raiseFlag')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => onLowerFlag(goal.id)}
            className="flex items-center gap-1 text-muted-foreground"
          >
            <CheckCircle className="size-3" />
            {t('goals.lowerFlag')}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

export function Goals() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const queryClient = useQueryClient();

  const consultantId = user?.id ?? '';

  const { data: goals, isLoading } = useQuery({
    queryKey: ['goals', 'mine', consultantId],
    queryFn: () => fetchMyGoals(consultantId),
    enabled: !!consultantId,
  });

  const [onboardingDismissed, setOnboardingDismissed] = useState(
    () => !!localStorage.getItem(`${ONBOARDING_KEY}-${consultantId}`),
  );

  const showOnboarding = !onboardingDismissed && !isLoading && !!goals && goals.length > 0;

  const handleDismissOnboarding = () => {
    localStorage.setItem(`${ONBOARDING_KEY}-${consultantId}`, '1');
    setOnboardingDismissed(true);
  };

  const raiseMutation = useMutation({
    mutationFn: (goalId: string) => raiseReadinessFlag(goalId, consultantId),
    onSuccess: () => {
      toast.success(t('goals.flagRaised'));
      queryClient.invalidateQueries({ queryKey: ['goals'] });
    },
    onError: () => toast.error(t('goals.flagRaiseError')),
  });

  const lowerMutation = useMutation({
    mutationFn: (goalId: string) => lowerReadinessFlag(goalId, consultantId),
    onSuccess: () => {
      toast.success(t('goals.flagLowered'));
      queryClient.invalidateQueries({ queryKey: ['goals'] });
    },
    onError: () => toast.error(t('goals.flagLowerError')),
  });

  if (isLoading) {
    return <div className="p-6">{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      {showOnboarding && goals && (
        <OnboardingModal goals={goals} onDismiss={handleDismissOnboarding} />
      )}
      <div>
        <h1 className="text-3xl font-bold">{t('goals.title')}</h1>
        <p className="text-muted-foreground">{t('goals.subtitle')}</p>
      </div>

      {goals && goals.length === 0 && (
        <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
          {t('goals.noGoals')}
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {goals?.map((goal) => (
          <GoalCard
            key={goal.id}
            goal={goal}
            onRaiseFlag={(id) => raiseMutation.mutate(id)}
            onLowerFlag={(id) => lowerMutation.mutate(id)}
          />
        ))}
      </div>
    </div>
  );
}
