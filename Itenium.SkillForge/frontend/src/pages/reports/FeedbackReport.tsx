import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Star, MessageSquare, Users, BarChart3 } from 'lucide-react';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  CardDescription,
  Button,
  Badge,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@itenium-forge/ui';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  fetchFeedback,
  fetchFeedbackSummary,
  fetchEnrollments,
  submitFeedback,
  fetchCourses,
  type FeedbackSummary,
} from '@/api/client';
import { useAuthStore } from '@/stores';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Textarea,
} from '@/components/ui-extras';

function StarDisplay({ rating, size = 'sm' }: { rating: number; size?: 'sm' | 'md' }) {
  const starSize = size === 'sm' ? 'size-3.5' : 'size-5';
  return (
    <span className="inline-flex gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <Star
          key={star}
          className={`${starSize} ${star <= Math.round(rating) ? 'text-amber-400 fill-amber-400' : 'text-gray-300 fill-gray-100'}`}
        />
      ))}
    </span>
  );
}

function StarPicker({
  value,
  onChange,
}: {
  value: number;
  onChange: (rating: number) => void;
}) {
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

interface RatingDialogProps {
  open: boolean;
  onClose: () => void;
  courseId: number;
  courseName: string;
}

function RatingDialog({ open, onClose, courseId, courseName }: RatingDialogProps) {
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: submitFeedback,
    onSuccess: () => {
      toast.success(`Thank you for rating "${courseName}"!`);
      queryClient.invalidateQueries({ queryKey: ['feedback'] });
      setRating(0);
      setComment('');
      onClose();
    },
    onError: () => {
      toast.error('Failed to submit rating. Please try again.');
    },
  });

  const handleSubmit = () => {
    if (rating === 0) {
      toast.error('Please select a rating before submitting.');
      return;
    }
    mutation.mutate({ courseId, rating, comment: comment.trim() || undefined });
  };

  return (
    <Dialog open={open} onOpenChange={(open) => !open && onClose()}>
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
          <Button onClick={handleSubmit} disabled={mutation.isPending || rating === 0}>
            {mutation.isPending ? 'Submitting...' : 'Submit Rating'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function BackOfficeFeedbackReport() {
  const { t } = useTranslation();
  const [detailCourse, setDetailCourse] = useState<FeedbackSummary | null>(null);

  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['feedbackSummary'],
    queryFn: fetchFeedbackSummary,
    staleTime: 30_000,
  });

  const { data: allFeedback, isLoading: feedbackLoading } = useQuery({
    queryKey: ['feedback'],
    queryFn: fetchFeedback,
    staleTime: 30_000,
  });

  const isLoading = summaryLoading || feedbackLoading;

  const totalRatings = (summary ?? []).reduce((sum, s) => sum + s.totalRatings, 0);
  const avgRating =
    (summary ?? []).length > 0
      ? (summary ?? []).reduce((sum, s) => sum + s.averageRating * s.totalRatings, 0) /
        Math.max(totalRatings, 1)
      : 0;
  const coursesWithRatings = (summary ?? []).filter((s) => s.totalRatings > 0).length;
  const top3 = [...(summary ?? [])].sort((a, b) => b.averageRating - a.averageRating).slice(0, 3);
  const recentFeedback = [...(allFeedback ?? [])]
    .sort((a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime())
    .slice(0, 10);

  const detailFeedback = detailCourse
    ? (allFeedback ?? []).filter((f) => f.courseId === detailCourse.courseId)
    : [];

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        {isLoading ? (
          Array.from({ length: 3 }).map((_, i) => (
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
            <Card className="bg-gradient-to-br from-amber-50 to-yellow-50 dark:from-amber-950 dark:to-yellow-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">Total Ratings</CardTitle>
                <Star className="size-4 text-amber-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{totalRatings}</div>
                <p className="text-xs text-muted-foreground">Ratings submitted</p>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-orange-50 to-amber-50 dark:from-orange-950 dark:to-amber-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">Average Rating</CardTitle>
                <Star className="size-4 text-orange-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{avgRating.toFixed(1)}</div>
                <StarDisplay rating={avgRating} />
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950 dark:to-indigo-950">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">Courses Rated</CardTitle>
                <BarChart3 className="size-4 text-blue-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{coursesWithRatings}</div>
                <p className="text-xs text-muted-foreground">Courses with ratings</p>
              </CardContent>
            </Card>
          </>
        )}
      </div>

      {/* Top 3 Rated Courses */}
      {!isLoading && top3.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Star className="size-5 text-amber-500" />
              Top Rated Courses
            </CardTitle>
            <CardDescription>Highest rated courses by average learner score</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-3 md:grid-cols-3">
              {top3.map((course, idx) => (
                <div
                  key={course.courseId}
                  className="rounded-lg border p-3 flex flex-col gap-1.5 bg-gradient-to-br from-amber-50/50 to-yellow-50/50 dark:from-amber-950/50 dark:to-yellow-950/50"
                >
                  <div className="flex items-center gap-2">
                    <span className="text-lg font-bold text-amber-500">#{idx + 1}</span>
                    <span className="font-medium text-sm truncate" title={course.courseName}>
                      {course.courseName}
                    </span>
                  </div>
                  <StarDisplay rating={course.averageRating} size="md" />
                  <p className="text-xs text-muted-foreground">
                    {course.averageRating.toFixed(1)} avg · {course.totalRatings} ratings
                  </p>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Ratings Table */}
      <Card>
        <CardHeader>
          <CardTitle>Course Ratings</CardTitle>
          <CardDescription>Average rating per course with feedback breakdown</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : (summary ?? []).length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('reports.noData')}</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Course</TableHead>
                  <TableHead>Average Rating</TableHead>
                  <TableHead className="text-right">Total Ratings</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(summary ?? [])
                  .slice()
                  .sort((a, b) => b.averageRating - a.averageRating)
                  .map((row) => (
                    <TableRow key={row.courseId}>
                      <TableCell className="font-medium">{row.courseName}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <StarDisplay rating={row.averageRating} />
                          <span className="text-sm text-muted-foreground">
                            {row.averageRating.toFixed(1)}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell className="text-right">{row.totalRatings}</TableCell>
                      <TableCell className="text-right">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setDetailCourse(row)}
                          disabled={row.totalRatings === 0}
                        >
                          <MessageSquare className="size-3 mr-1" />
                          View Comments
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Recent Feedback */}
      {!isLoading && recentFeedback.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="size-5 text-primary" />
              Recent Feedback
            </CardTitle>
            <CardDescription>Latest 10 feedback entries from learners</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {recentFeedback.map((fb) => (
                <div
                  key={fb.id}
                  className="rounded-lg border p-3 space-y-1.5"
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <StarDisplay rating={fb.rating} />
                      {fb.courseName && (
                        <Badge variant="outline" className="text-xs">
                          {fb.courseName}
                        </Badge>
                      )}
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {new Date(fb.submittedAt).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                      })}
                    </span>
                  </div>
                  {fb.comment && (
                    <p className="text-sm text-muted-foreground italic">"{fb.comment}"</p>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Detail Dialog */}
      <Dialog open={!!detailCourse} onOpenChange={(open) => !open && setDetailCourse(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              Feedback for {detailCourse?.courseName}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3 max-h-[60vh] overflow-y-auto py-2">
            {detailFeedback.length === 0 ? (
              <p className="text-sm text-muted-foreground">No feedback comments yet.</p>
            ) : (
              detailFeedback.map((fb) => (
                <div key={fb.id} className="rounded-lg border p-3 space-y-1.5">
                  <div className="flex items-center justify-between">
                    <StarDisplay rating={fb.rating} />
                    <span className="text-xs text-muted-foreground">
                      {new Date(fb.submittedAt).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                      })}
                    </span>
                  </div>
                  {fb.comment && (
                    <p className="text-sm text-muted-foreground italic">"{fb.comment}"</p>
                  )}
                </div>
              ))
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDetailCourse(null)}>
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function LearnerFeedbackReport() {
  const [ratingDialog, setRatingDialog] = useState<{
    courseId: number;
    courseName: string;
  } | null>(null);

  const { data: myFeedback, isLoading: feedbackLoading } = useQuery({
    queryKey: ['feedback'],
    queryFn: fetchFeedback,
    staleTime: 30_000,
  });

  const { data: enrollments, isLoading: enrollmentsLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
    staleTime: 30_000,
  });

  const { data: courses, isLoading: coursesLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
    staleTime: 30_000,
  });

  const isLoading = feedbackLoading || enrollmentsLoading || coursesLoading;

  const ratedCourseIds = new Set((myFeedback ?? []).map((f) => f.courseId));
  const completedEnrollments = (enrollments ?? []).filter((e) => e.completedAt !== null);
  const unratedCompleted = completedEnrollments.filter((e) => !ratedCourseIds.has(e.courseId));

  return (
    <div className="space-y-6">
      {/* Rate a Course */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Star className="size-5 text-amber-500" />
            Rate a Course
          </CardTitle>
          <CardDescription>
            Share your experience for courses you've completed
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          ) : unratedCompleted.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {completedEnrollments.length === 0
                ? 'Complete a course to leave a rating.'
                : 'You have rated all your completed courses. Thank you!'}
            </p>
          ) : (
            <div className="space-y-2">
              {unratedCompleted.map((enrollment) => {
                const course = courses?.find((c) => c.id === enrollment.courseId);
                const courseName = course?.name ?? `Course #${enrollment.courseId}`;
                return (
                  <div
                    key={enrollment.id}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <div>
                      <p className="font-medium text-sm">{courseName}</p>
                      {enrollment.completedAt && (
                        <p className="text-xs text-muted-foreground">
                          Completed {new Date(enrollment.completedAt).toLocaleDateString()}
                        </p>
                      )}
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() =>
                        setRatingDialog({ courseId: enrollment.courseId, courseName })
                      }
                    >
                      <Star className="size-3 mr-1 text-amber-500" />
                      Leave Rating
                    </Button>
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* My Submitted Ratings */}
      {!feedbackLoading && (myFeedback ?? []).length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MessageSquare className="size-5 text-primary" />
              My Ratings
            </CardTitle>
            <CardDescription>Your feedback for completed courses</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {(myFeedback ?? [])
                .slice()
                .sort(
                  (a, b) =>
                    new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime(),
                )
                .map((fb) => (
                  <div key={fb.id} className="rounded-lg border p-3 space-y-1.5">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <StarDisplay rating={fb.rating} size="md" />
                        {fb.courseName && (
                          <span className="font-medium text-sm">{fb.courseName}</span>
                        )}
                      </div>
                      <span className="text-xs text-muted-foreground">
                        {new Date(fb.submittedAt).toLocaleDateString(undefined, {
                          year: 'numeric',
                          month: 'short',
                          day: 'numeric',
                        })}
                      </span>
                    </div>
                    {fb.comment && (
                      <p className="text-sm text-muted-foreground italic">"{fb.comment}"</p>
                    )}
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}

      {ratingDialog && (
        <RatingDialog
          open={!!ratingDialog}
          onClose={() => setRatingDialog(null)}
          courseId={ratingDialog.courseId}
          courseName={ratingDialog.courseName}
        />
      )}
    </div>
  );
}

export function FeedbackReport() {
  const { t } = useTranslation();
  const user = useAuthStore((state) => state.user);
  const isBackOffice = user?.isBackOffice ?? false;

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('reports.feedback')}</h1>
          <p className="text-muted-foreground">{t('reports.feedbackSubtitle')}</p>
        </div>
      </div>

      {isBackOffice ? <BackOfficeFeedbackReport /> : <LearnerFeedbackReport />}
    </div>
  );
}
