import { useTranslation } from 'react-i18next';
import { BookOpen, Users, Award, Target, Flag } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent, Badge } from '@itenium-forge/ui';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore, useTeamStore } from '@/stores';
import { fetchMyGoals, fetchCoachDashboard } from '@/api/client';

function LearnerDashboard() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const consultantId = user?.id ?? '';

  const { data: goals, isLoading } = useQuery({
    queryKey: ['goals', 'mine', consultantId],
    queryFn: () => fetchMyGoals(consultantId),
    enabled: !!consultantId,
  });

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
        <p className="text-muted-foreground">
          {goals && goals.length > 0
            ? t('dashboard.welcomeWithGoals', { name: user?.name })
            : t('dashboard.welcome')}
        </p>
      </div>

      {/* FR15: pre-populated goals banner */}
      {goals && goals.length > 0 && (
        <div className="rounded-lg border border-primary/20 bg-primary/5 p-4">
          <p className="text-sm font-medium text-primary">
            {t('dashboard.coachPreparedGoals', { count: goals.length })}
          </p>
        </div>
      )}

      {/* Active goals grid */}
      {goals && goals.length > 0 && (
        <div>
          <h2 className="mb-3 text-lg font-semibold">{t('goals.activeGoals')}</h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {goals.map((goal) => {
              const now = new Date();
              const deadline = new Date(goal.deadline);
              const daysLeft = Math.ceil((deadline.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
              const pct = goal.skill
                ? Math.round((goal.currentNiveau / goal.skill.levelCount) * 100)
                : 0;

              return (
                <Card key={goal.id}>
                  <CardHeader className="flex flex-row items-start justify-between pb-2">
                    <div>
                      <CardTitle className="text-base">{goal.skill?.name ?? `Skill #${goal.skillId}`}</CardTitle>
                      <p className="text-xs text-muted-foreground">{goal.skill?.category}</p>
                    </div>
                    <Badge variant={daysLeft < 0 ? 'destructive' : 'secondary'}>
                      {daysLeft < 0 ? t('goals.overdue') : `${daysLeft}d`}
                    </Badge>
                  </CardHeader>
                  <CardContent>
                    <div className="flex items-center gap-2 text-sm text-muted-foreground mb-2">
                      <Target className="size-3" />
                      <span>{goal.currentNiveau} → {goal.targetNiveau}</span>
                    </div>
                    <div className="h-1.5 rounded-full bg-muted">
                      <div className="h-1.5 rounded-full bg-primary" style={{ width: `${pct}%` }} />
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </div>
      )}

      {/* Placeholder stats */}
      {(!goals || goals.length === 0) && (
        <div className="grid gap-4 md:grid-cols-3">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">{t('dashboard.totalCourses')}</CardTitle>
              <BookOpen className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">0</div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}

function ManagerDashboard() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { selectedTeam } = useTeamStore();
  const coachId = user?.id ?? '';

  const { data: items, isLoading } = useQuery({
    queryKey: ['dashboard', 'coach', coachId],
    queryFn: () => fetchCoachDashboard(coachId),
    enabled: !!coachId,
    staleTime: 30_000,
  });

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  const withFlags = items?.filter((i) => i.readinessFlagAgeInDays !== null) ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
        <p className="text-muted-foreground">
          {t('dashboard.welcome')}
          {selectedTeam && ` - ${selectedTeam.name}`}
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.totalConsultants')}</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{items?.length ?? 0}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.needingAttention')}</CardTitle>
            <Flag className="size-4 text-destructive" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{withFlags.length}</div>
            <p className="text-xs text-muted-foreground">{t('dashboard.withActiveFlags')}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.completedCourses')}</CardTitle>
            <Award className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {items?.reduce((sum, i) => sum + i.activeGoalCount, 0) ?? 0}
            </div>
            <p className="text-xs text-muted-foreground">{t('dashboard.activeGoalsAcrossTeam')}</p>
          </CardContent>
        </Card>
      </div>

      {/* Consultants needing attention */}
      {withFlags.length > 0 && (
        <div>
          <h2 className="mb-3 text-lg font-semibold flex items-center gap-2">
            <Flag className="size-4 text-destructive" />
            {t('dashboard.needsAttention')}
          </h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {withFlags.map((item) => (
              <Card key={item.consultantId} className="border-destructive/30">
                <CardContent className="pt-4 space-y-2">
                  <p className="font-medium text-sm">{item.consultantId}</p>
                  <Badge variant="destructive" className="flex items-center gap-1 w-fit text-xs">
                    <Flag className="size-3" />
                    {t('dashboard.raisedDaysAgo', { days: Math.floor(item.readinessFlagAgeInDays ?? 0) })}
                  </Badge>
                  <p className="text-xs text-muted-foreground">
                    {t('dashboard.activeGoals', { count: item.activeGoalCount })}
                  </p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export function Dashboard() {
  const { mode } = useTeamStore();
  const { user } = useAuthStore();

  // Learners have no team assignment (mode stays backoffice but they have no teams)
  // If the user is not a manager, show the learner dashboard
  const isLearner = !user?.isBackOffice && mode !== 'manager';

  if (isLearner) {
    return <LearnerDashboard />;
  }

  if (mode === 'manager') {
    return <ManagerDashboard />;
  }

  // BackOffice: show generic stats
  return <ManagerDashboard />;
}
