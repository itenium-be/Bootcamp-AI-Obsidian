import { useTranslation } from 'react-i18next';
import {
  BookOpen,
  Users,
  Award,
  TrendingUp,
  BarChart3,
  CheckCircle,
  Target,
  Trophy,
  Activity,
  FileText,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent, CardDescription, Skeleton, Badge } from '@itenium-forge/ui';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { useAuthStore, useTeamStore } from '@/stores';
import { fetchStats, fetchProgress } from '@/api/client';
import { Progress } from '@/components/ui-extras';

function StatCardSkeleton() {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-4 w-4 rounded" />
      </CardHeader>
      <CardContent>
        <Skeleton className="h-8 w-16 mb-1" />
        <Skeleton className="h-3 w-32" />
      </CardContent>
    </Card>
  );
}

export function Dashboard() {
  const { t } = useTranslation();
  const { mode, selectedTeam, teams } = useTeamStore();
  const { user } = useAuthStore();

  const isBackOffice = user?.isBackOffice ?? false;
  const isLearnerOnly = !isBackOffice && (!teams || teams.length === 0);

  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['stats'],
    queryFn: fetchStats,
    staleTime: 30_000,
  });

  const { data: progressData, isLoading: progressLoading } = useQuery({
    queryKey: ['progress'],
    queryFn: fetchProgress,
    enabled: isLearnerOnly,
    staleTime: 30_000,
  });

  const myProgress =
    progressData
      ?.filter((p) => p.learnerId === user?.id)
      .sort((a, b) => b.percentageComplete - a.percentageComplete)
      .slice(0, 3) ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
        <p className="text-muted-foreground">
          {t('dashboard.welcome')}
          {mode === 'manager' && selectedTeam && ` - ${selectedTeam.name}`}
        </p>
      </div>

      {/* BackOffice: 5-card stats grid */}
      {isBackOffice ? (
        <div className="grid gap-4 md:grid-cols-3 lg:grid-cols-5">
          {statsLoading ? (
            Array.from({ length: 5 }).map((_, i) => <StatCardSkeleton key={i} />)
          ) : (
            <>
              <Card className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950 dark:to-indigo-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.totalCourses')}</CardTitle>
                  <BookOpen className="size-4 text-blue-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalCourses ?? 0}</div>
                  <p className="text-xs text-muted-foreground flex items-center gap-1">
                    <TrendingUp className="size-3 text-green-500" />
                    Platform-wide
                  </p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-950 dark:to-pink-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.activeLearners')}</CardTitle>
                  <Users className="size-4 text-purple-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalLearners ?? 0}</div>
                  <p className="text-xs text-muted-foreground">Registered learners</p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-950 dark:to-emerald-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.totalEnrollments')}</CardTitle>
                  <Activity className="size-4 text-green-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalEnrollments ?? 0}</div>
                  <p className="text-xs text-muted-foreground">Total enrollments</p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-amber-50 to-orange-50 dark:from-amber-950 dark:to-orange-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.certificates')}</CardTitle>
                  <Award className="size-4 text-amber-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalCertificates ?? 0}</div>
                  <p className="text-xs text-muted-foreground flex items-center gap-1">
                    <Trophy className="size-3 text-amber-500" />
                    Issued certificates
                  </p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-teal-50 to-cyan-50 dark:from-teal-950 dark:to-cyan-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.completionRate')}</CardTitle>
                  <Target className="size-4 text-teal-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.completionRate ?? 0}%</div>
                  <Progress value={stats?.completionRate ?? 0} className="mt-2 h-1.5" />
                </CardContent>
              </Card>
            </>
          )}
        </div>
      ) : (
        /* Learner / Manager: 3-card stats grid */
        <div className="grid gap-4 md:grid-cols-3">
          {statsLoading ? (
            Array.from({ length: 3 }).map((_, i) => <StatCardSkeleton key={i} />)
          ) : isLearnerOnly ? (
            <>
              <Card className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950 dark:to-indigo-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.totalCourses')}</CardTitle>
                  <BookOpen className="size-4 text-blue-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalCourses ?? 0}</div>
                  <p className="text-xs text-muted-foreground">Available to explore</p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-950 dark:to-emerald-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.totalEnrollments')}</CardTitle>
                  <Activity className="size-4 text-green-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalEnrollments ?? 0}</div>
                  <p className="text-xs text-muted-foreground">Platform enrollments</p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-amber-50 to-orange-50 dark:from-amber-950 dark:to-orange-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.certificates')}</CardTitle>
                  <Award className="size-4 text-amber-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalCertificates ?? 0}</div>
                  <p className="text-xs text-muted-foreground">Platform-wide</p>
                </CardContent>
              </Card>
            </>
          ) : (
            <>
              <Card className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950 dark:to-indigo-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.totalCourses')}</CardTitle>
                  <BookOpen className="size-4 text-blue-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalCourses ?? 0}</div>
                  <p className="text-xs text-muted-foreground">Available courses</p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-950 dark:to-emerald-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.totalEnrollments')}</CardTitle>
                  <Activity className="size-4 text-green-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.totalEnrollments ?? 0}</div>
                  <p className="text-xs text-muted-foreground">Team enrollments</p>
                </CardContent>
              </Card>

              <Card className="bg-gradient-to-br from-teal-50 to-cyan-50 dark:from-teal-950 dark:to-cyan-950">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium">{t('dashboard.completionRate')}</CardTitle>
                  <Target className="size-4 text-teal-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats?.completionRate ?? 0}%</div>
                  <Progress value={stats?.completionRate ?? 0} className="mt-2 h-1.5" />
                </CardContent>
              </Card>
            </>
          )}
        </div>
      )}

      {/* Learner: Motivational Banner + My Progress */}
      {isLearnerOnly && (
        <>
          <Card className="border-l-4 border-l-blue-500 bg-gradient-to-r from-blue-50 to-transparent dark:from-blue-950">
            <CardContent className="pt-4">
              <div className="flex items-center gap-3">
                <Trophy className="size-6 text-blue-500 flex-shrink-0" />
                <div className="flex-1">
                  <p className="font-medium">{t('dashboard.keepLearning')}</p>
                  <Progress value={stats?.completionRate ?? 0} className="mt-2 h-2" />
                </div>
              </div>
            </CardContent>
          </Card>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">{t('dashboard.yourProgress')}</CardTitle>
                <CardDescription>Your top courses in progress</CardDescription>
              </CardHeader>
              <CardContent>
                {progressLoading ? (
                  <div className="space-y-3">
                    {Array.from({ length: 3 }).map((_, i) => (
                      <Skeleton key={i} className="h-8 w-full" />
                    ))}
                  </div>
                ) : myProgress.length === 0 ? (
                  <p className="text-sm text-muted-foreground">{t('dashboard.noRecentActivity')}</p>
                ) : (
                  <div className="space-y-3">
                    {myProgress.map((p) => (
                      <div key={p.id} className="space-y-1">
                        <div className="flex justify-between text-sm">
                          <span className="text-muted-foreground">Course #{p.courseId}</span>
                          <span className="font-medium">{p.percentageComplete}%</span>
                        </div>
                        <Progress value={p.percentageComplete} className="h-2" />
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">{t('dashboard.quickActions')}</CardTitle>
                <CardDescription>Jump to your learning activities</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <Link
                    to="/courses"
                    className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                  >
                    <BookOpen className="size-4 text-blue-500" />
                    <span className="text-sm font-medium">{t('dashboard.browseCatalog')}</span>
                  </Link>
                  <Link
                    to="/settings"
                    className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                  >
                    <Award className="size-4 text-amber-500" />
                    <span className="text-sm font-medium">{t('dashboard.myCertificates')}</span>
                  </Link>
                </div>
              </CardContent>
            </Card>
          </div>
        </>
      )}

      {/* Manager: Team Overview + Quick Links */}
      {!isBackOffice && !isLearnerOnly && (
        <div className="grid gap-4 md:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('dashboard.teamOverview')}</CardTitle>
              <CardDescription>{selectedTeam ? `Team: ${selectedTeam.name}` : 'Your team at a glance'}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Total Enrollments</span>
                  <Badge variant="secondary">{stats?.totalEnrollments ?? 0}</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Certificates Issued</span>
                  <Badge variant="secondary">{stats?.totalCertificates ?? 0}</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Completion Rate</span>
                  <Badge variant="secondary">{stats?.completionRate ?? 0}%</Badge>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('dashboard.quickActions')}</CardTitle>
              <CardDescription>Manage your team</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <Link
                  to="/courses"
                  className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                >
                  <Users className="size-4 text-purple-500" />
                  <span className="text-sm font-medium">{t('dashboard.teamMembers')}</span>
                </Link>
                <Link
                  to="/courses"
                  className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                >
                  <BarChart3 className="size-4 text-blue-500" />
                  <span className="text-sm font-medium">{t('dashboard.teamProgress')}</span>
                </Link>
                <Link
                  to="/courses"
                  className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                >
                  <BookOpen className="size-4 text-green-500" />
                  <span className="text-sm font-medium">{t('dashboard.assignCourses')}</span>
                </Link>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* BackOffice: Admin Actions + System Health */}
      {isBackOffice && (
        <div className="grid gap-4 md:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('dashboard.quickActions')}</CardTitle>
              <CardDescription>Platform administration</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <Link
                  to="/courses"
                  className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                >
                  <Users className="size-4 text-purple-500" />
                  <span className="text-sm font-medium">{t('dashboard.manageUsers')}</span>
                </Link>
                <Link
                  to="/courses"
                  className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                >
                  <Target className="size-4 text-blue-500" />
                  <span className="text-sm font-medium">{t('dashboard.manageTeams')}</span>
                </Link>
                <Link
                  to="/courses"
                  className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                >
                  <FileText className="size-4 text-green-500" />
                  <span className="text-sm font-medium">{t('dashboard.viewReports')}</span>
                </Link>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('dashboard.systemHealth')}</CardTitle>
              <CardDescription>Platform infrastructure status</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t('dashboard.apiStatus')}</span>
                  <div className="flex items-center gap-1.5">
                    <CheckCircle className="size-4 text-green-500" />
                    <span className="text-sm font-medium text-green-600 dark:text-green-400">
                      {t('dashboard.healthy')}
                    </span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t('dashboard.dbStatus')}</span>
                  <div className="flex items-center gap-1.5">
                    <CheckCircle className="size-4 text-green-500" />
                    <span className="text-sm font-medium text-green-600 dark:text-green-400">
                      {t('dashboard.connected')}
                    </span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Active Users</span>
                  <Badge variant="secondary">{stats?.totalLearners ?? 0}</Badge>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
