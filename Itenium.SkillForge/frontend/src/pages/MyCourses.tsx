import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Card, CardHeader, CardTitle, CardContent, Badge, Button } from '@itenium-forge/ui';
import { BookOpen, CheckCircle, Clock, PlayCircle, BookMarked, Trophy, Star } from 'lucide-react';
import { toast } from 'sonner';
import {
  fetchEnrollments,
  fetchCourses,
  fetchProgress,
  fetchFeedback,
  submitFeedback,
  type Enrollment,
  type Course,
  type Progress as ProgressType,
} from '@/api/client';
import { Progress, Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Textarea } from '@/components/ui-extras';

interface EnrichedCourse {
  enrollment: Enrollment;
  course: Course | undefined;
  progress: ProgressType | undefined;
}

function getStatus(
  enrollment: Enrollment,
  progress: ProgressType | undefined,
): 'completed' | 'inProgress' | 'notStarted' {
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

function StarPicker({ value, onChange }: { value: number; onChange: (r: number) => void }) {
  const [hovered, setHovered] = useState(0);
  return (
    <span className="inline-flex gap-1">
      {[1, 2, 3, 4, 5].map((star) => (
        <button
          key={star}
          type="button"
          onClick={() => onChange(star)}
          onMouseEnter={() => setHovered(star)}
          onMouseLeave={() => setHovered(0)}
          className="focus:outline-none"
        >
          <Star
            className={`size-7 transition-colors ${
              star <= (hovered || value)
                ? 'text-amber-400 fill-amber-400'
                : 'text-gray-300 fill-gray-100'
            }`}
          />
        </button>
      ))}
    </span>
  );
}

function CourseRatingDialog({
  open,
  onClose,
  courseId,
  courseName,
  existingRating,
}: {
  open: boolean;
  onClose: () => void;
  courseId: number;
  courseName: string;
  existingRating?: number;
}) {
  const [rating, setRating] = useState(existingRating ?? 0);
  const [comment, setComment] = useState('');
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: submitFeedback,
    onSuccess: () => {
      toast.success(`Thank you for rating "${courseName}"!`);
      queryClient.invalidateQueries({ queryKey: ['feedback'] });
      onClose();
    },
    onError: () => {
      toast.error('Failed to submit rating. Please try again.');
    },
  });

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Rate This Course</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <p className="text-sm text-muted-foreground font-medium">{courseName}</p>
          <div>
            <p className="text-sm font-medium mb-2">Your Rating</p>
            <StarPicker value={rating} onChange={setRating} />
          </div>
          <div>
            <p className="text-sm font-medium mb-2">Comment (optional)</p>
            <Textarea
              placeholder="Share your thoughts about this course..."
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              rows={3}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={mutation.isPending}>
            Cancel
          </Button>
          <Button onClick={() => mutation.mutate({ courseId, rating, comment: comment.trim() || undefined })} disabled={mutation.isPending || rating === 0}>
            {mutation.isPending ? 'Submitting...' : 'Submit Rating'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function CourseItem({ item, ratedCourseIds }: { item: EnrichedCourse; ratedCourseIds: Set<number> }) {
  const { t } = useTranslation();
  const status = getStatus(item.enrollment, item.progress);
  const pct = item.progress?.percentageComplete ?? 0;
  const [ratingOpen, setRatingOpen] = useState(false);
  const isRated = ratedCourseIds.has(item.enrollment.courseId);
  const courseName = item.course?.name ?? `Course #${item.enrollment.courseId}`;

  return (
    <>
    <Card className="hover:shadow-md transition-shadow">
      <CardContent className="pt-4">
        <div className="flex flex-col gap-3">
          <div className="flex items-start justify-between gap-2">
            <div className="flex-1 min-w-0">
              <h3 className="font-semibold truncate">{courseName}</h3>
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
          <div className="flex items-center justify-end gap-2">
            {status === 'completed' && (
              isRated ? (
                <span className="inline-flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
                  <Star className="size-3 fill-amber-400 text-amber-400" />
                  Rated
                </span>
              ) : (
                <Button variant="ghost" size="sm" onClick={() => setRatingOpen(true)} className="gap-1 text-amber-600 hover:text-amber-700">
                  <Star className="size-3" />
                  Rate
                </Button>
              )
            )}
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
    {ratingOpen && (
      <CourseRatingDialog
        open={ratingOpen}
        onClose={() => setRatingOpen(false)}
        courseId={item.enrollment.courseId}
        courseName={courseName}
      />
    )}
    </>
  );
}

export function MyCourses() {
  const { t } = useTranslation();

  const { data: enrollments } = useQuery({ queryKey: ['enrollments'], queryFn: fetchEnrollments });
  const { data: courses } = useQuery({ queryKey: ['courses'], queryFn: fetchCourses });
  const { data: progressList } = useQuery({ queryKey: ['progress'], queryFn: fetchProgress });
  const { data: feedback } = useQuery({ queryKey: ['feedback'], queryFn: fetchFeedback });

  const ratedCourseIds = useMemo(() => new Set((feedback ?? []).map((f) => f.courseId)), [feedback]);

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
            <Link to="/catalog">{t('myCourses.browseCatalog')}</Link>
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {enrichedCourses.map((item) => (
            <CourseItem key={item.enrollment.id} item={item} ratedCourseIds={ratedCourseIds} />
          ))}
        </div>
      )}
    </div>
  );
}
