import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { Target, Flag, BookOpen, Award, Video } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent, Badge, Button } from '@itenium-forge/ui';
import { fetchGoals, fetchConsultantActivity, type ActivityType } from '@/api/client';
import { useAuthStore } from '@/stores';

interface ConsultantProfileProps {
  consultantId: string;
}

function activityIcon(type: ActivityType) {
  switch (type) {
    case 'GoalCreated':
      return <Target className="size-4 text-primary" />;
    case 'ReadinessFlagRaised':
      return <Flag className="size-4 text-destructive" />;
    case 'ResourceCompleted':
      return <BookOpen className="size-4 text-green-500" />;
    case 'ValidationReceived':
      return <Award className="size-4 text-yellow-500" />;
    default:
      return null;
  }
}

function activityBadgeVariant(type: ActivityType): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (type) {
    case 'ReadinessFlagRaised':
      return 'destructive';
    case 'ValidationReceived':
      return 'default';
    default:
      return 'secondary';
  }
}

export function ConsultantProfile({ consultantId }: ConsultantProfileProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const isManager = !user?.isBackOffice && !!user;

  const handleStartSession = () => {
    void navigate({ to: '/session/$consultantId', params: { consultantId } });
  };

  const { data: goals, isLoading: goalsLoading } = useQuery({
    queryKey: ['goals', consultantId],
    queryFn: () => fetchGoals(consultantId),
  });

  const { data: activity, isLoading: activityLoading } = useQuery({
    queryKey: ['activity', consultantId],
    queryFn: () => fetchConsultantActivity(consultantId),
  });

  const isLoading = goalsLoading || activityLoading;

  if (isLoading) {
    return <div className="p-6">{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('consultant.profile')}</h1>
          <p className="text-muted-foreground text-sm">{consultantId}</p>
        </div>
        {isManager && (
          <Button onClick={handleStartSession}>
            <Video className="size-4 mr-2" />
            {t('session.startSession')}
          </Button>
        )}
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        {/* Active Goals */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <Target className="size-4" />
              {t('goals.activeGoals')} ({goals?.filter((g) => g.status === 'Active').length ?? 0})
            </CardTitle>
          </CardHeader>
          <CardContent>
            {(!goals || goals.length === 0) && (
              <p className="text-sm text-muted-foreground">{t('goals.noGoals')}</p>
            )}
            <div className="space-y-3">
              {goals?.filter((g) => g.status === 'Active').map((goal) => {
                const deadline = new Date(goal.deadline);
                const daysLeft = Math.ceil((deadline.getTime() - Date.now()) / (1000 * 60 * 60 * 24));

                return (
                  <div key={goal.id} className="flex items-start justify-between rounded border p-3">
                    <div>
                      <p className="text-sm font-medium">{goal.skill?.name ?? `Skill #${goal.skillId}`}</p>
                      <p className="text-xs text-muted-foreground">
                        {goal.currentNiveau} → {goal.targetNiveau}
                      </p>
                    </div>
                    <Badge variant={daysLeft < 0 ? 'destructive' : 'outline'} className="text-xs">
                      {daysLeft < 0 ? t('goals.overdue') : `${daysLeft}d`}
                    </Badge>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>

        {/* Activity History */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('consultant.activityHistory')}</CardTitle>
          </CardHeader>
          <CardContent>
            {(!activity || activity.length === 0) && (
              <p className="text-sm text-muted-foreground">{t('consultant.noActivity')}</p>
            )}
            <div className="space-y-3 max-h-80 overflow-y-auto">
              {activity?.map((item, idx) => (
                <div key={idx} className="flex items-start gap-3">
                  <div className="mt-0.5 shrink-0">{activityIcon(item.type)}</div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm truncate">{item.description}</p>
                    <p className="text-xs text-muted-foreground">
                      {new Date(item.occurredAt).toLocaleDateString()}
                    </p>
                  </div>
                  <Badge variant={activityBadgeVariant(item.type)} className="text-xs shrink-0">
                    {t(`activity.${item.type}`)}
                  </Badge>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
