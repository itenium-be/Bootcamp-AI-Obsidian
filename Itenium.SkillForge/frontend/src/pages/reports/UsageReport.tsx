import { useTranslation } from 'react-i18next';
import { Download, BarChart3, Users, BookOpen, TrendingUp } from 'lucide-react';
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
import { fetchCourses, fetchEnrollments } from '@/api/client';

interface CourseUsageRow {
  id: number;
  name: string;
  category: string | null;
  level: string | null;
  enrollments: number;
  inProgress: number;
  completed: number;
  completionRate: number;
}

function getCompletionRateColor(rate: number): string {
  if (rate >= 70) return 'text-green-600 dark:text-green-400';
  if (rate >= 30) return 'text-yellow-600 dark:text-yellow-400';
  return 'text-red-600 dark:text-red-400';
}

function getCompletionRateBadge(rate: number): 'default' | 'secondary' | 'destructive' | 'outline' {
  if (rate >= 70) return 'default';
  if (rate >= 30) return 'secondary';
  return 'destructive';
}

export function UsageReport() {
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

  const isLoading = coursesLoading || enrollmentsLoading;

  const courseUsage: CourseUsageRow[] = (courses ?? [])
    .map((course) => {
      const courseEnrollments = (enrollments ?? []).filter((e) => e.courseId === course.id);
      const completed = courseEnrollments.filter((e) => e.completedAt !== null).length;
      const inProgress = courseEnrollments.length - completed;
      const completionRate =
        courseEnrollments.length > 0 ? Math.round((completed / courseEnrollments.length) * 100) : 0;

      return {
        id: course.id,
        name: course.name,
        category: course.category,
        level: course.level,
        enrollments: courseEnrollments.length,
        inProgress,
        completed,
        completionRate,
      };
    })
    .sort((a, b) => b.enrollments - a.enrollments);

  const totalEnrollments = (enrollments ?? []).length;
  const activeLearnersSet = new Set((enrollments ?? []).map((e) => e.learnerId));
  const activeLearnersCount = activeLearnersSet.size;
  const coursesWithEnrollments = courseUsage.filter((c) => c.enrollments > 0).length;

  const maxEnrollments = Math.max(...courseUsage.map((c) => c.enrollments), 1);
  const top3 = courseUsage.slice(0, 3);

  const handleExport = () => {
    toast.info('Export feature coming soon');
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('reports.usage')}</h1>
          <p className="text-muted-foreground">{t('reports.usageSubtitle')}</p>
        </div>
        <Button variant="outline" onClick={handleExport} className="gap-2">
          <Download className="size-4" />
          {t('reports.exportReport')}
        </Button>
      </div>

      {/* Key Metric Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        {isLoading ? (
          Array.from({ length: 3 }).map((_, i) => (
            <Card key={i}>
              <CardHeader className="pb-2">
                <Skeleton className="h-4 w-32" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-8 w-16" />
              </CardContent>
            </Card>
          ))
        ) : (
          <>
            <Card className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950 dark:to-indigo-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">{t('reports.enrollments')}</CardTitle>
                <TrendingUp className="size-4 text-blue-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{totalEnrollments}</div>
                <p className="text-xs text-muted-foreground">Total platform enrollments</p>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-950 dark:to-pink-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">{t('reports.activeUsers')}</CardTitle>
                <Users className="size-4 text-purple-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{activeLearnersCount}</div>
                <p className="text-xs text-muted-foreground">Unique learners enrolled</p>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-950 dark:to-emerald-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">Courses with Activity</CardTitle>
                <BookOpen className="size-4 text-green-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{coursesWithEnrollments}</div>
                <p className="text-xs text-muted-foreground">
                  out of {courses?.length ?? 0} total courses
                </p>
              </CardContent>
            </Card>
          </>
        )}
      </div>

      {/* Top 3 Most Popular Courses */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="size-5 text-primary" />
            {t('reports.topCourses')}
          </CardTitle>
          <CardDescription>Most popular courses by enrollment count</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-8 w-full" />
              ))}
            </div>
          ) : top3.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('reports.noData')}</p>
          ) : (
            <div className="space-y-3">
              {top3.map((course, index) => {
                const percentage =
                  maxEnrollments > 0 ? Math.round((course.enrollments / maxEnrollments) * 100) : 0;
                return (
                  <div key={course.id} className="flex items-center gap-3">
                    <span className="w-5 text-sm font-bold text-muted-foreground">#{index + 1}</span>
                    <span className="w-40 truncate text-sm font-medium" title={course.name}>
                      {course.name}
                    </span>
                    <div className="flex-1 bg-muted rounded-full h-2">
                      <div
                        className="bg-primary h-2 rounded-full transition-all"
                        style={{ width: `${percentage}%` }}
                      />
                    </div>
                    <span className="text-sm text-muted-foreground w-16 text-right">
                      {course.enrollments} enrolled
                    </span>
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Course Usage Table */}
      <Card>
        <CardHeader>
          <CardTitle>Course Usage Details</CardTitle>
          <CardDescription>
            Detailed enrollment and completion statistics per course, sorted by popularity
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : courseUsage.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('reports.noData')}</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('reports.courseTitle')}</TableHead>
                  <TableHead>Category</TableHead>
                  <TableHead>Level</TableHead>
                  <TableHead className="text-right">{t('reports.enrollments')}</TableHead>
                  <TableHead className="text-right">In Progress</TableHead>
                  <TableHead className="text-right">{t('reports.completions')}</TableHead>
                  <TableHead className="text-right">{t('reports.rate')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {courseUsage.map((course) => (
                  <TableRow key={course.id}>
                    <TableCell className="font-medium">{course.name}</TableCell>
                    <TableCell>
                      {course.category ? (
                        <Badge variant="outline">{course.category}</Badge>
                      ) : (
                        <span className="text-muted-foreground text-xs">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {course.level ? (
                        <Badge variant="secondary">{course.level}</Badge>
                      ) : (
                        <span className="text-muted-foreground text-xs">—</span>
                      )}
                    </TableCell>
                    <TableCell className="text-right font-medium">{course.enrollments}</TableCell>
                    <TableCell className="text-right">{course.inProgress}</TableCell>
                    <TableCell className="text-right">{course.completed}</TableCell>
                    <TableCell className="text-right">
                      {course.enrollments > 0 ? (
                        <span
                          className={`font-semibold ${getCompletionRateColor(course.completionRate)}`}
                        >
                          {course.completionRate}%
                        </span>
                      ) : (
                        <Badge variant={getCompletionRateBadge(course.completionRate)}>
                          {course.completionRate}%
                        </Badge>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
