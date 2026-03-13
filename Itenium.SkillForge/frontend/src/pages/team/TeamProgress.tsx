import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  CardDescription,
  Badge,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Skeleton,
  Progress,
  Button,
} from '@itenium-forge/ui';
import { TrendingUp, BarChart3, Award, CheckCircle, Clock, Search, Filter, Download } from 'lucide-react';
import { useTeamStore } from '@/stores';
import {
  fetchEnrollments,
  fetchProgress,
  fetchCourses,
  fetchUsers,
  type Enrollment,
  type ProgressEntry,
  type Course,
  type User,
} from '@/api/client';

type ProgressStatus = 'completed' | 'inProgress' | 'notStarted';

interface ProgressRow {
  enrollmentId: number;
  userId: string;
  userName: string;
  courseId: number;
  courseName: string;
  progressPct: number;
  status: ProgressStatus;
  lastUpdated: string | null;
}

function getStatus(enrollment: Enrollment, progress: ProgressEntry | undefined): ProgressStatus {
  if (enrollment.completedAt) return 'completed';
  if (progress && progress.progressPercentage > 0) return 'inProgress';
  return 'notStarted';
}

function StatusBadge({ status }: { status: ProgressStatus }) {
  const { t } = useTranslation();
  if (status === 'completed') {
    return (
      <Badge className="bg-green-100 text-green-700 border-green-200 dark:bg-green-900 dark:text-green-300">
        <CheckCircle className="size-3 mr-1" />
        {t('teamProgress.completed')}
      </Badge>
    );
  }
  if (status === 'inProgress') {
    return (
      <Badge className="bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-900 dark:text-blue-300">
        <Clock className="size-3 mr-1" />
        {t('teamProgress.inProgress')}
      </Badge>
    );
  }
  return (
    <Badge variant="outline" className="text-muted-foreground">
      {t('teamProgress.notStarted')}
    </Badge>
  );
}

function buildRows(
  enrollments: Enrollment[],
  progressEntries: ProgressEntry[],
  courses: Course[],
  users: User[],
): ProgressRow[] {
  return enrollments.map((enrollment) => {
    const course = courses.find((c) => c.id === enrollment.courseId);
    const user = users.find((u) => u.id === enrollment.userId);
    const progress = progressEntries.find(
      (p) => p.courseId === enrollment.courseId && p.userId === enrollment.userId,
    );
    const status = getStatus(enrollment, progress);
    const progressPct =
      status === 'completed' ? 100 : progress?.progressPercentage ?? 0;

    const userName =
      user
        ? user.firstName && user.lastName
          ? `${user.firstName} ${user.lastName}`
          : user.userName
        : enrollment.userId.slice(0, 8);

    return {
      enrollmentId: enrollment.id,
      userId: enrollment.userId,
      userName,
      courseId: enrollment.courseId,
      courseName: course?.name ?? `Course #${enrollment.courseId}`,
      progressPct,
      status,
      lastUpdated: progress?.lastUpdated ?? enrollment.enrolledAt,
    };
  });
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function StatCircle({ value, label, color }: { value: string; label: string; color: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2">
      <div
        className={`size-24 rounded-full flex items-center justify-center text-2xl font-bold border-4 ${color}`}
      >
        {value}
      </div>
      <span className="text-xs text-muted-foreground text-center">{label}</span>
    </div>
  );
}

export function TeamProgress() {
  const { t } = useTranslation();
  const { selectedTeam } = useTeamStore();
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<ProgressStatus | 'all'>('all');

  const { data: enrollments = [], isLoading: enrollmentsLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  const { data: progressEntries = [], isLoading: progressLoading } = useQuery({
    queryKey: ['progress'],
    queryFn: fetchProgress,
  });

  const { data: courses = [], isLoading: coursesLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
  });

  const { data: users = [], isLoading: usersLoading } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
    retry: false,
    throwOnError: false,
  });

  const isLoading = enrollmentsLoading || progressLoading || coursesLoading || usersLoading;

  const allRows = buildRows(enrollments, progressEntries, courses, users);

  const filteredRows = allRows.filter((row) => {
    const matchesSearch =
      row.userName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      row.courseName.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesStatus = statusFilter === 'all' || row.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const totalEnrollments = allRows.length;
  const completedCount = allRows.filter((r) => r.status === 'completed').length;
  const avgProgress =
    allRows.length > 0
      ? Math.round(allRows.reduce((sum, r) => sum + r.progressPct, 0) / allRows.length)
      : 0;
  const completionRate =
    allRows.length > 0 ? Math.round((completedCount / allRows.length) * 100) : 0;

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('teamProgress.title')}</h1>
          <p className="text-muted-foreground mt-1">
            {selectedTeam ? selectedTeam.name : ''} &mdash; {t('teamProgress.subtitle')}
          </p>
        </div>
        <Button variant="outline" size="sm" className="gap-2">
          <Download className="size-4" />
          {t('teamProgress.exportHint')}
        </Button>
      </div>

      {/* Summary Stats Row */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t('teamProgress.totalEnrollments')}
            </CardTitle>
            <div className="rounded-full bg-blue-100 p-2 dark:bg-blue-900">
              <BarChart3 className="size-4 text-blue-600 dark:text-blue-400" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">
              {isLoading ? <Skeleton className="h-8 w-12" /> : totalEnrollments}
            </div>
            <div className="mt-2">
              <Progress value={completionRate} className="h-1.5" />
            </div>
            <p className="text-xs text-muted-foreground mt-1">{completedCount} completed</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t('teamProgress.averageProgress')}
            </CardTitle>
            <div className="rounded-full bg-purple-100 p-2 dark:bg-purple-900">
              <TrendingUp className="size-4 text-purple-600 dark:text-purple-400" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">
              {isLoading ? <Skeleton className="h-8 w-16" /> : `${avgProgress}%`}
            </div>
            <div className="mt-2">
              <Progress value={avgProgress} className="h-1.5" />
            </div>
            <p className="text-xs text-muted-foreground mt-1">Across all enrollments</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t('teamProgress.completionRate')}
            </CardTitle>
            <div className="rounded-full bg-green-100 p-2 dark:bg-green-900">
              <Award className="size-4 text-green-600 dark:text-green-400" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">
              {isLoading ? <Skeleton className="h-8 w-16" /> : `${completionRate}%`}
            </div>
            <div className="mt-2">
              <Progress value={completionRate} className="h-1.5" />
            </div>
            <p className="text-xs text-muted-foreground mt-1">{completedCount} of {totalEnrollments} courses</p>
          </CardContent>
        </Card>
      </div>

      {/* Circular Visual Stats */}
      {!isLoading && allRows.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Progress Snapshot</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-around py-4">
              <StatCircle
                value={`${avgProgress}%`}
                label={t('teamProgress.averageProgress')}
                color="border-purple-500 text-purple-600 dark:text-purple-400"
              />
              <div className="h-16 w-px bg-border" />
              <StatCircle
                value={`${completionRate}%`}
                label={t('teamProgress.completionRate')}
                color="border-green-500 text-green-600 dark:text-green-400"
              />
              <div className="h-16 w-px bg-border" />
              <StatCircle
                value={String(completedCount)}
                label="Completions"
                color="border-amber-500 text-amber-600 dark:text-amber-400"
              />
              <div className="h-16 w-px bg-border" />
              <StatCircle
                value={String(totalEnrollments)}
                label={t('teamProgress.totalEnrollments')}
                color="border-blue-500 text-blue-600 dark:text-blue-400"
              />
            </div>
          </CardContent>
        </Card>
      )}

      {/* Progress Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between flex-wrap gap-3">
            <div>
              <CardTitle>{t('teamProgress.title')}</CardTitle>
              <CardDescription>Member &times; course progress matrix</CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                <Input
                  placeholder={t('common.search') + '...'}
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-9 w-48"
                />
              </div>
              <div className="flex items-center gap-1 border rounded-md p-1">
                <Filter className="size-3.5 text-muted-foreground ml-1" />
                {(['all', 'completed', 'inProgress', 'notStarted'] as const).map((s) => (
                  <button
                    key={s}
                    onClick={() => setStatusFilter(s)}
                    className={`px-2 py-1 rounded text-xs transition-colors ${
                      statusFilter === s
                        ? 'bg-primary text-primary-foreground'
                        : 'text-muted-foreground hover:text-foreground'
                    }`}
                  >
                    {s === 'all' ? t('teamProgress.allStatuses') : t(`teamProgress.${s}`)}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('teamProgress.member')}</TableHead>
                <TableHead>{t('teamProgress.course')}</TableHead>
                <TableHead>{t('teamProgress.progress')}</TableHead>
                <TableHead>{t('teamProgress.status')}</TableHead>
                <TableHead>{t('teamProgress.lastUpdated')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {[1, 2, 3, 4, 5].map((j) => (
                      <TableCell key={j}>
                        <Skeleton className="h-4 w-full" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : filteredRows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-center py-12 text-muted-foreground">
                    {t('teamProgress.noProgress')}
                  </TableCell>
                </TableRow>
              ) : (
                filteredRows.map((row) => (
                  <TableRow key={row.enrollmentId} className="hover:bg-muted/50 transition-colors">
                    <TableCell>
                      <span className="font-medium">{row.userName}</span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">{row.courseName}</span>
                    </TableCell>
                    <TableCell className="w-48">
                      <div className="flex items-center gap-2">
                        <Progress value={row.progressPct} className="flex-1 h-2" />
                        <span className="text-xs text-muted-foreground w-8 text-right">
                          {row.progressPct}%
                        </span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={row.status} />
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatDate(row.lastUpdated)}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
