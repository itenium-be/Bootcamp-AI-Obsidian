import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Flag, AlertTriangle, Target, Activity } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent, Badge, Button } from '@itenium-forge/ui';
import { useAuthStore } from '@/stores';
import { fetchCoachDashboard, type CoachDashboardItem } from '@/api/client';
import { Link } from '@tanstack/react-router';

function ConsultantCard({ item }: { item: CoachDashboardItem }) {
  const { t } = useTranslation();

  return (
    <Card className={item.isInactive ? 'border-warning/50 bg-warning/5' : ''}>
      <CardHeader className="flex flex-row items-start justify-between pb-2">
        <div>
          <CardTitle className="text-base font-medium">{item.consultantId}</CardTitle>
        </div>
        <div className="flex items-center gap-1">
          {item.isInactive && (
            <Badge variant="outline" className="border-warning text-warning flex items-center gap-1 text-xs">
              <AlertTriangle className="size-3" />
              {t('dashboard.inactive')}
            </Badge>
          )}
          {item.readinessFlagAgeInDays !== null && (
            <Badge variant="destructive" className="flex items-center gap-1 text-xs">
              <Flag className="size-3" />
              {t('dashboard.raisedDaysAgo', { days: Math.floor(item.readinessFlagAgeInDays) })}
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Target className="size-3" />
          <span>{t('dashboard.activeGoals', { count: item.activeGoalCount })}</span>
        </div>
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Activity className="size-3" />
          <span>
            {t('dashboard.lastActivity')}: {new Date(item.lastActivityAt).toLocaleDateString()}
          </span>
        </div>
        <Link
          to="/consultants/$consultantId"
          params={{ consultantId: item.consultantId }}
        >
          <Button size="sm" variant="outline" className="w-full">
            {t('dashboard.viewProfile')}
          </Button>
        </Link>
      </CardContent>
    </Card>
  );
}

export function CoachDashboard() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const coachId = user?.id ?? '';

  const { data: items, isLoading } = useQuery({
    queryKey: ['dashboard', 'coach', coachId],
    queryFn: () => fetchCoachDashboard(coachId),
    enabled: !!coachId,
    staleTime: 30_000,
  });

  if (isLoading) {
    return <div className="p-6">{t('common.loading')}</div>;
  }

  const withFlags = items?.filter((i) => i.readinessFlagAgeInDays !== null) ?? [];
  const inactive = items?.filter((i) => i.isInactive) ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('dashboard.coachTitle')}</h1>
        <p className="text-muted-foreground">{t('dashboard.coachSubtitle')}</p>
      </div>

      {/* Summary strip */}
      {items && items.length > 0 && (
        <div className="grid gap-3 md:grid-cols-3">
          <Card>
            <CardContent className="pt-4">
              <div className="text-2xl font-bold">{items.length}</div>
              <p className="text-sm text-muted-foreground">{t('dashboard.totalConsultants')}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4">
              <div className="flex items-center gap-2">
                <Flag className="size-4 text-destructive" />
                <div className="text-2xl font-bold">{withFlags.length}</div>
              </div>
              <p className="text-sm text-muted-foreground">{t('dashboard.needingAttention')}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4">
              <div className="flex items-center gap-2">
                <AlertTriangle className="size-4 text-warning" />
                <div className="text-2xl font-bold">{inactive.length}</div>
              </div>
              <p className="text-sm text-muted-foreground">{t('dashboard.inactive3Weeks')}</p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Attention first — consultants with readiness flags */}
      {withFlags.length > 0 && (
        <div>
          <h2 className="mb-3 text-lg font-semibold flex items-center gap-2">
            <Flag className="size-4 text-destructive" />
            {t('dashboard.needsAttention')}
          </h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {withFlags.map((item) => (
              <ConsultantCard key={item.consultantId} item={item} />
            ))}
          </div>
        </div>
      )}

      {/* All consultants */}
      <div>
        <h2 className="mb-3 text-lg font-semibold">{t('dashboard.allConsultants')}</h2>
        {items && items.length === 0 && (
          <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
            {t('dashboard.noConsultants')}
          </div>
        )}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {items?.map((item) => (
            <ConsultantCard key={item.consultantId} item={item} />
          ))}
        </div>
      </div>
    </div>
  );
}
