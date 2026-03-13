import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardHeader, CardTitle, CardContent, Button } from '@itenium-forge/ui';
import { TrendingUp, Target, BarChart3, CheckCircle } from 'lucide-react';
import { toast } from 'sonner';
import { fetchProgress, fetchCourses, updateProgress, type Progress as ProgressType } from '@/api/client';
import { Progress } from '@/components/ui-extras';

function getProgressColor(pct: number): string {
  if (pct < 33) return '[&>div]:bg-red-500';
  if (pct < 66) return '[&>div]:bg-yellow-500';
  return '[&>div]:bg-green-500';
}

function ProgressCard({ progress, courseName }: { progress: ProgressType; courseName: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [pctValue, setPctValue] = useState(progress.percentageComplete);
  const [notesValue, setNotesValue] = useState(progress.notes ?? '');

  const updateMutation = useMutation({
    mutationFn: ({ courseId, data }: { courseId: number; data: { percentageComplete: number; notes?: string } }) =>
      updateProgress(courseId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['progress'] });
      toast.success(t('myProgress.progressUpdated'));
      setEditing(false);
    },
    onError: () => {
      toast.error(t('common.error'));
    },
  });

  const handleSave = () => {
    updateMutation.mutate({
      courseId: progress.courseId,
      data: { percentageComplete: pctValue, notes: notesValue || undefined },
    });
  };

  const pct = progress.percentageComplete;
  const colorClass = getProgressColor(pct);

  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardContent className="pt-5 space-y-4">
        <div className="flex items-start justify-between gap-2">
          <div>
            <h3 className="font-semibold">{courseName}</h3>
            <p className="text-xs text-muted-foreground mt-0.5">
              {t('myProgress.lastUpdated')}: {new Date(progress.lastUpdated).toLocaleDateString()}
            </p>
          </div>
          <div
            className="text-3xl font-bold tabular-nums"
            style={{ color: pct >= 66 ? '#16a34a' : pct >= 33 ? '#d97706' : '#dc2626' }}
          >
            {pct}%
          </div>
        </div>

        <Progress value={pct} className={`h-3 ${colorClass}`} />

        {progress.notes && !editing && <p className="text-sm text-muted-foreground italic">{progress.notes}</p>}

        {editing ? (
          <div className="space-y-3 border rounded-lg p-3 bg-muted/30">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                {t('myProgress.overallProgress')} (0-100)
              </label>
              <input
                type="number"
                min={0}
                max={100}
                value={pctValue}
                onChange={(e) => setPctValue(Math.min(100, Math.max(0, Number(e.target.value))))}
                className="h-8 w-24 rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">{t('myProgress.notes')}</label>
              <textarea
                value={notesValue}
                onChange={(e) => setNotesValue(e.target.value)}
                rows={2}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-ring resize-none"
              />
            </div>
            <div className="flex gap-2">
              <Button size="sm" onClick={handleSave} disabled={updateMutation.isPending}>
                {t('myProgress.saveProgress')}
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setEditing(false)}>
                {t('common.cancel')}
              </Button>
            </div>
          </div>
        ) : (
          <Button size="sm" variant="outline" onClick={() => setEditing(true)} className="w-full">
            <Target className="size-3 mr-1" />
            {t('myProgress.updateProgress')}
          </Button>
        )}
      </CardContent>
    </Card>
  );
}

export function MyProgress() {
  const { t } = useTranslation();

  const { data: progressList } = useQuery({ queryKey: ['progress'], queryFn: fetchProgress });
  const { data: courses } = useQuery({ queryKey: ['courses'], queryFn: fetchCourses });

  const inProgress = progressList?.filter((p) => p.percentageComplete < 100) ?? [];
  const completed = progressList?.filter((p) => p.percentageComplete === 100) ?? [];

  const avgCompletion =
    progressList && progressList.length > 0
      ? Math.round(progressList.reduce((sum, p) => sum + p.percentageComplete, 0) / progressList.length)
      : 0;

  const getCourseName = (courseId: number) => {
    const c = courses?.find((c) => c.id === courseId);
    return c?.name ?? `Course #${courseId}`;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">{t('myProgress.title')}</h1>
        <p className="text-muted-foreground">{t('myProgress.subtitle')}</p>
      </div>

      {/* Overview stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t('myProgress.overallProgress')}
            </CardTitle>
            <BarChart3 className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{avgCompletion}%</div>
            <Progress value={avgCompletion} className="h-2 mt-2" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('myProgress.inProgress')}</CardTitle>
            <TrendingUp className="size-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">{inProgress.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t('myProgress.coursesCompleted')}
            </CardTitle>
            <CheckCircle className="size-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600 dark:text-green-400">{completed.length}</div>
          </CardContent>
        </Card>
      </div>

      {/* In-progress courses */}
      {inProgress.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 gap-3 text-muted-foreground">
          <TrendingUp className="size-12 opacity-20" />
          <p className="text-lg">{t('myProgress.noProgress')}</p>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {inProgress.map((p) => (
            <ProgressCard key={p.id} progress={p} courseName={getCourseName(p.courseId)} />
          ))}
        </div>
      )}

      {/* Completed courses */}
      {completed.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <CheckCircle className="size-5 text-green-500" />
            {t('myProgress.coursesCompleted')}
          </h2>
          <div className="grid gap-3 md:grid-cols-2">
            {completed.map((p) => (
              <Card key={p.id} className="bg-green-50 dark:bg-green-950 border-green-200 dark:border-green-800">
                <CardContent className="pt-4 flex items-center justify-between">
                  <span className="font-medium">{getCourseName(p.courseId)}</span>
                  <span className="inline-flex items-center gap-1 text-sm font-semibold text-green-700 dark:text-green-300">
                    <CheckCircle className="size-4" />
                    100%
                  </span>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
