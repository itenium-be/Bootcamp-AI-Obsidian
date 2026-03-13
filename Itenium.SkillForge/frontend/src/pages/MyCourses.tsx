import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  Badge,
  Button,
  Progress,
} from '@itenium-forge/ui';
import { BookOpen, CheckCircle, Clock, PlayCircle, BookMarked, Trophy } from 'lucide-react';
import { fetchEnrollments, fetchCourses, fetchProgress, type Enrollment, type Course, type Progress as ProgressType } from '@/api/client';

type EnrichedCourse = {
  enrollment: Enrollment;
  course: Course | undefined;
  progress: ProgressType | undefined;
};

function getStatus(enrollment: Enrollment, progress: ProgressType | undefined): 'completed' | 'inProgress' | 'notStarted' {
  if (enrollment.completedAt !== null) return 'completed';
  if (progress && progress.percentageComplete > 0) return 'inProgress';
  return 'notStarted';
}

function StatusBadge({ status }: { status: 'completed' | 'inProgress' | 'notStarted' }) {
  const { t } = useTranslation();
  if (status === 'completed') {
    return (
      <span className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300">
        <CheckCircle className="size-3" />
        {t('myCourses.completed')}
      </span>
    );
  }
  if (status === 'inProgress') {
    return (
      <span className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
        <PlayCircle className="size-3" />
        {t('myCourses.inProgress')}
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
      <Clock className="size-3" />
      {t('myCourses.notStarted')}
    </span>
  );
}

function CourseItem({ item }: { item: EnrichedCourse }) {
  const { t } = useTranslation();
  const status = getStatus(item.enrollment, item.progress);
  const pct = item.progress?.percentageComplete ?? 0;

  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardContent className="pt-4">
        <div className="flex flex-col gap-3">
          <div className="flex items-start justify-between gap-2">
            <div className="flex-1 min-w-0">
              <h3 className="font-semibold truncate">{item.course?.name ?? `Course #${item.enrollment.courseId}`}</h3>
              <div className="flex flex-wrap gap-1.5 mt-1">
                {item.course?.category && (
                  <Badge variant="outline" className="text-xs">
                    {item.course.category}
                  </Badge>
                )}
                {item.course?.level && (
                  <Badge variant="secondary" className="text-xs">
                    {item.course.level}
                  </Badge>
                )}
              </div>
            </div>
            <StatusBadge status={status} />
          </div>

          {/* Progress bar */}
          <div className="space-y-1">
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>{pct}%</span>
              {status === 'completed' && item.enrollment.completedAt && (
                <span>
                  {t('myCourses.completedOn')} {new Date(item.enrollment.completedAt).toLocaleDateString()}
                </span>
              )}
              {status !== 'completed' && (
                <span>
                  {t('myCourses.enrolledOn')} {new Date(item.enrollment.enrolledAt).toLocaleDateString()}
                </span>
              )}
            </div>
            <Progress value={pct} className="h-2" />
          </div>

          {/* Action button */}
          <div className="flex justify-end">
            {status === 'completed' ? (
              <Button variant="outline" size="sm" asChild>
                <Link to="/my-certificates">
                  <Trophy className="size-3 mr-1" />
                  View Certificate
                </Link>
              </Button>
            ) : status === 'inProgress' ? (
              <Button size="sm" asChild>
                <Link to="/my-progress">
                  <PlayCircle className="size-3 mr-1" />
                  {t('myCourses.continueLearning')}
                </Link>
              </Button>
            ) : (
              <Button size="sm" variant="outline" asChild>
                <Link to="/my-progress">
                  <BookMarked className="size-3 mr-1" />
                  {t('myCourses.startLearning')}
                </Link>
              </Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

export function MyCourses() {
  const { t } = useTranslation();

  const { data: enrollments } = useQuery({ queryKey: ['enrollments'], queryFn: fetchEnrollments });
  const { data: courses } = useQuery({ queryKey: ['courses'], queryFn: fetchCourses });
  const { data: progressList } = useQuery({ queryKey: ['progress'], queryFn: fetchProgress });

  const enrichedCourses = useMemo<EnrichedCourse[]>(
    () =>
      enrollments?.map((enrollment) => ({
        enrollment,
        course: courses?.find((c) => c.id === enrollment.courseId),
        progress: progressList?.find((p) => p.courseId === enrollment.courseId),
      })) ?? [],
    [enrollments, courses, progressList],
  );

  const completedCount = enrichedCourses.filter((e) => getStatus(e.enrollment, e.progress) === 'completed').length;
  const inProgressCount = enrichedCourses.filter((e) => getStatus(e.enrollment, e.progress) === 'inProgress').length;
  const totalCount = enrichedCourses.length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">{t('myCourses.title')}</h1>
        <p className="text-muted-foreground">{t('myCourses.subtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Enrolled</CardTitle>
            <BookOpen className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('myCourses.inProgress')}</CardTitle>
            <PlayCircle className="size-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">{inProgressCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('myCourses.completed')}</CardTitle>
            <CheckCircle className="size-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600 dark:text-green-400">{completedCount}</div>
          </CardContent>
        </Card>
      </div>

      {/* Course List */}
      {totalCount === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 gap-4 text-muted-foreground">
          <BookOpen className="size-16 opacity-20" />
          <p className="text-lg font-medium">{t('myCourses.noEnrollments')}</p>
          <Button asChild>
            <Link to="/catalog">
              {t('myCourses.browseCatalog')}
            </Link>
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {enrichedCourses.map((item) => (
            <CourseItem key={item.enrollment.id} item={item} />
          ))}
        </div>
      )}
    </div>
  );
}
