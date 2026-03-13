import { useTranslation } from 'react-i18next';
import { Award, CheckCircle, Target, Download, TrendingUp, Clock } from 'lucide-react';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  CardDescription,
  Button,
  Skeleton,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@itenium-forge/ui';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
import { fetchCourses, fetchEnrollments, fetchCertificates } from '@/api/client';

interface CompletionRow {
  id: number;
  name: string;
  enrolled: number;
  completed: number;
  certificates: number;
  rate: number;
}

function getPerformanceBadge(rate: number) {
  if (rate >= 80) return { label: 'Excellent', variant: 'default' as const };
  if (rate >= 60) return { label: 'Good', variant: 'secondary' as const };
  if (rate >= 30) return { label: 'Needs Attention', variant: 'outline' as const };
  return { label: 'Poor', variant: 'destructive' as const };
}

// Mocked monthly trend data
const monthlyTrend = [
  { month: 'Oct', completions: 4 },
  { month: 'Nov', completions: 7 },
  { month: 'Dec', completions: 5 },
  { month: 'Jan', completions: 12 },
  { month: 'Feb', completions: 9 },
  { month: 'Mar', completions: 15 },
];

export function CompletionReport() {
  const { t } = useTranslation();

  const { data: courses, isLoading: coursesLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
    staleTime: 30_000,
  });

  const { data: enrollments, isLoading: enrollmentsLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
    staleTime: 30_000,
  });

  const { data: certificates, isLoading: certificatesLoading } = useQuery({
    queryKey: ['certificates'],
    queryFn: fetchCertificates,
    staleTime: 30_000,
  });

  const isLoading = coursesLoading || enrollmentsLoading || certificatesLoading;

  const completionRows: CompletionRow[] = (courses ?? [])
    .map((course) => {
      const courseEnrollments = (enrollments ?? []).filter((e) => e.courseId === course.id);
      const completed = courseEnrollments.filter((e) => e.completedAt !== null).length;
      const certs = (certificates ?? []).filter((c) => c.courseId === course.id).length;
      const rate = courseEnrollments.length > 0 ? Math.round((completed / courseEnrollments.length) * 100) : 0;

      return {
        id: course.id,
        name: course.name,
        enrolled: courseEnrollments.length,
        completed,
        certificates: certs,
        rate,
      };
    })
    .sort((a, b) => b.rate - a.rate);

  const totalEnrollments = (enrollments ?? []).length;
  const totalCompleted = (enrollments ?? []).filter((e) => e.completedAt !== null).length;
  const totalCertificates = (certificates ?? []).length;
  const overallRate = totalEnrollments > 0 ? Math.round((totalCompleted / totalEnrollments) * 100) : 0;

  const maxTrend = Math.max(...monthlyTrend.map((m) => m.completions), 1);

  const handleExport = () => {
    toast.info('Export feature coming soon');
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('reports.completion')}</h1>
          <p className="text-muted-foreground">{t('reports.completionSubtitle')}</p>
        </div>
        <Button variant="outline" onClick={handleExport} className="gap-2">
          <Download className="size-4" />
          {t('reports.exportReport')}
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        {isLoading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <CardHeader className="pb-2">
                <Skeleton className="h-4 w-28" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-8 w-16" />
              </CardContent>
            </Card>
          ))
        ) : (
          <>
            <Card className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-950 dark:to-emerald-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">{t('reports.completions')}</CardTitle>
                <CheckCircle className="size-4 text-green-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{totalCompleted}</div>
                <p className="text-xs text-muted-foreground">of {totalEnrollments} enrolled</p>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-teal-50 to-cyan-50 dark:from-teal-950 dark:to-cyan-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">{t('reports.rate')}</CardTitle>
                <Target className="size-4 text-teal-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{overallRate}%</div>
                <div className="bg-muted rounded-full overflow-hidden mt-1 h-1.5"><div className="bg-primary rounded-full transition-all" style={{ height: "100%", width: `${Math.min(100, overallRate)}%` }} /></div>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-amber-50 to-orange-50 dark:from-amber-950 dark:to-orange-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">{t('reports.courseCompletions')}</CardTitle>
                <Award className="size-4 text-amber-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{totalCertificates}</div>
                <p className="text-xs text-muted-foreground">Certificates issued</p>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950 dark:to-indigo-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">Top Completion Rate</CardTitle>
                <TrendingUp className="size-4 text-blue-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {completionRows[0]?.rate ?? 0}%
                </div>
                <p className="text-xs text-muted-foreground truncate" title={completionRows[0]?.name}>
                  {completionRows[0]?.name ?? '—'}
                </p>
              </CardContent>
            </Card>
          </>
        )}
      </div>

      {/* Monthly Trend + Leaderboard */}
      <div className="grid gap-4 md:grid-cols-2">
        {/* Month-by-month trend */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="size-4" />
              {t('reports.completionTrend')}
            </CardTitle>
            <CardDescription>
              Monthly completions — Historical trends available after 30 days
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {monthlyTrend.map((entry) => {
                const pct = Math.round((entry.completions / maxTrend) * 100);
                return (
                  <div key={entry.month} className="flex items-center gap-3">
                    <span className="w-8 text-xs text-muted-foreground">{entry.month}</span>
                    <div className="flex-1 bg-muted rounded-full h-2">
                      <div
                        className="bg-primary h-2 rounded-full transition-all"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <span className="text-sm text-muted-foreground w-8 text-right">
                      {entry.completions}
                    </span>
                  </div>
                );
              })}
            </div>
            <p className="text-xs text-muted-foreground mt-3 italic">{t('reports.comingSoon')}</p>
          </CardContent>
        </Card>

        {/* Completion Rate Highlight */}
        <Card>
          <CardHeader>
            <CardTitle>Overall Completion</CardTitle>
            <CardDescription>Platform-wide learner achievement summary</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col items-center justify-center py-6 gap-4">
            {isLoading ? (
              <Skeleton className="h-24 w-24 rounded-full" />
            ) : (
              <>
                <div className="relative flex items-center justify-center">
                  <div className="text-5xl font-bold text-primary">{overallRate}%</div>
                </div>
                <div className="bg-muted rounded-full overflow-hidden w-full h-3"><div className="bg-primary rounded-full transition-all" style={{ height: "100%", width: `${Math.min(100, overallRate)}%` }} /></div>
                <div className="grid grid-cols-2 gap-4 w-full text-center">
                  <div>
                    <div className="text-xl font-bold text-green-600">{totalCompleted}</div>
                    <div className="text-xs text-muted-foreground">Completed</div>
                  </div>
                  <div>
                    <div className="text-xl font-bold">{totalEnrollments - totalCompleted}</div>
                    <div className="text-xs text-muted-foreground">In Progress</div>
                  </div>
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Completion Leaderboard Table */}
      <Card>
        <CardHeader>
          <CardTitle>Completion Leaderboard</CardTitle>
          <CardDescription>Course completion rates ranked highest to lowest</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : completionRows.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('reports.noData')}</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>#</TableHead>
                  <TableHead>{t('reports.courseTitle')}</TableHead>
                  <TableHead className="text-right">Enrolled</TableHead>
                  <TableHead className="text-right">Completed</TableHead>
                  <TableHead className="text-right">Certificates</TableHead>
                  <TableHead className="text-right">{t('reports.rate')}</TableHead>
                  <TableHead className="text-right">Performance</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {completionRows.map((row, index) => {
                  const perf = getPerformanceBadge(row.rate);
                  return (
                    <TableRow key={row.id}>
                      <TableCell className="text-muted-foreground font-medium">{index + 1}</TableCell>
                      <TableCell className="font-medium">{row.name}</TableCell>
                      <TableCell className="text-right">{row.enrolled}</TableCell>
                      <TableCell className="text-right">{row.completed}</TableCell>
                      <TableCell className="text-right">{row.certificates}</TableCell>
                      <TableCell className="text-right font-semibold">{row.rate}%</TableCell>
                      <TableCell className="text-right">
                        <Badge variant={perf.variant}>{perf.label}</Badge>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
