import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Plus, Pencil, Trash2, BookOpen } from 'lucide-react';
import {
  fetchCourses,
  createCourse,
  updateCourse,
  deleteCourse,
  type Course,
  type CourseFormData,
} from '@/api/client';
import { useAuthStore, useTeamStore } from '@/stores';
import {
  Button,
  Badge,
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
} from '@itenium-forge/ui';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
  Textarea,
} from '@/components/ui-extras';

const courseSchema = z.object({
  name: z.string().min(1, 'courseManagement.courseNameRequired'),
  description: z.string().optional(),
  category: z.string().optional(),
  level: z.string().optional(),
});

type CourseFormValues = z.infer<typeof courseSchema>;

function getLevelBadgeColor(level: string | null) {
  switch (level?.toLowerCase()) {
    case 'beginner':
      return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400';
    case 'intermediate':
      return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400';
    case 'advanced':
      return 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400';
    default:
      return 'bg-muted text-muted-foreground';
  }
}

interface CourseDialogProps {
  open: boolean;
  onClose: () => void;
  course?: Course | null;
}

function CourseDialog({ open, onClose, course }: CourseDialogProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const form = useForm<CourseFormValues>({
    resolver: zodResolver(courseSchema),
    defaultValues: {
      name: course?.name ?? '',
      description: course?.description ?? '',
      category: course?.category ?? '',
      level: course?.level ?? '',
    },
    values: {
      name: course?.name ?? '',
      description: course?.description ?? '',
      category: course?.category ?? '',
      level: course?.level ?? '',
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CourseFormData) => createCourse(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courses'] });
      toast.success(t('courseManagement.saveSuccess'));
      onClose();
    },
    onError: () => {
      toast.error(t('common.error'));
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: CourseFormData) => updateCourse(course!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courses'] });
      toast.success(t('courseManagement.saveSuccess'));
      onClose();
    },
    onError: () => {
      toast.error(t('common.error'));
    },
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  function onSubmit(values: CourseFormValues) {
    const data: CourseFormData = {
      name: values.name,
      description: values.description || undefined,
      category: values.category || undefined,
      level: values.level || undefined,
    };
    if (course) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate(data);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o: boolean) => !o && onClose()}>
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>
            {course ? t('courseManagement.editCourse') : t('courseManagement.addCourse')}
          </DialogTitle>
          <DialogDescription>
            {course ? t('courses.editCourse') : t('courses.addCourse')}
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courseManagement.courseName')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('courseManagement.courseName')} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courseManagement.description')}</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder={t('courseManagement.description')}
                      rows={3}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="category"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courseManagement.category')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('courseManagement.category')} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="level"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courseManagement.level')}</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder={t('courseManagement.level')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="Beginner">
                        {t('courseManagement.levelBeginner')}
                      </SelectItem>
                      <SelectItem value="Intermediate">
                        {t('courseManagement.levelIntermediate')}
                      </SelectItem>
                      <SelectItem value="Advanced">
                        {t('courseManagement.levelAdvanced')}
                      </SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending ? t('common.loading') : t('common.save')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}

export function Courses() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { teams } = useTeamStore();
  const queryClient = useQueryClient();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingCourse, setEditingCourse] = useState<Course | null>(null);

  const canManage = user?.isBackOffice || teams.length > 0;

  const { data: courses, isLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteCourse(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courses'] });
      toast.success(t('courseManagement.deleteSuccess'));
    },
    onError: () => {
      toast.error(t('common.error'));
    },
  });

  function handleAdd() {
    setEditingCourse(null);
    setDialogOpen(true);
  }

  function handleEdit(course: Course) {
    setEditingCourse(course);
    setDialogOpen(true);
  }

  function handleDialogClose() {
    setDialogOpen(false);
    setEditingCourse(null);
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <Skeleton className="h-9 w-48" />
            <Skeleton className="mt-1 h-5 w-72" />
          </div>
          <Skeleton className="h-10 w-32" />
        </div>
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-14 w-full" />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold">
            <BookOpen className="h-8 w-8 text-primary" />
            {t('courses.title')}
          </h1>
          <p className="mt-1 text-muted-foreground">{t('courseManagement.subtitle')}</p>
        </div>
        {canManage && (
          <Button onClick={handleAdd} className="gap-2">
            <Plus className="h-4 w-4" />
            {t('courseManagement.addCourse')}
          </Button>
        )}
      </div>

      <div className="rounded-lg border bg-card shadow-sm">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left text-sm font-semibold">{t('courses.name')}</th>
              <th className="p-3 text-left text-sm font-semibold">{t('courses.description')}</th>
              <th className="p-3 text-left text-sm font-semibold">{t('courses.category')}</th>
              <th className="p-3 text-left text-sm font-semibold">{t('courses.level')}</th>
              {canManage && (
                <th className="p-3 text-right text-sm font-semibold">{t('courses.actions')}</th>
              )}
            </tr>
          </thead>
          <tbody>
            {courses?.map((course) => (
              <tr key={course.id} className="border-b transition-colors hover:bg-muted/30">
                <td className="p-3 font-medium">{course.name}</td>
                <td className="max-w-xs p-3 text-muted-foreground">
                  <span className="line-clamp-2">{course.description || '-'}</span>
                </td>
                <td className="p-3">
                  {course.category ? (
                    <Badge variant="outline">{course.category}</Badge>
                  ) : (
                    <span className="text-muted-foreground">-</span>
                  )}
                </td>
                <td className="p-3">
                  {course.level ? (
                    <span
                      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${getLevelBadgeColor(course.level)}`}
                    >
                      {course.level}
                    </span>
                  ) : (
                    <span className="text-muted-foreground">-</span>
                  )}
                </td>
                {canManage && (
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleEdit(course)}
                        className="h-8 w-8 p-0"
                      >
                        <Pencil className="h-4 w-4" />
                        <span className="sr-only">{t('common.edit')}</span>
                      </Button>
                      <AlertDialog>
                        <AlertDialogTrigger asChild>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 w-8 p-0 text-destructive hover:text-destructive"
                          >
                            <Trash2 className="h-4 w-4" />
                            <span className="sr-only">{t('common.delete')}</span>
                          </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                          <AlertDialogHeader>
                            <AlertDialogTitle>
                              {t('courseManagement.confirmDelete')}
                            </AlertDialogTitle>
                            <AlertDialogDescription>
                              {t('courseManagement.deleteWarning')}
                            </AlertDialogDescription>
                          </AlertDialogHeader>
                          <AlertDialogFooter>
                            <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                            <AlertDialogAction
                              onClick={() => deleteMutation.mutate(course.id)}
                              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                            >
                              {t('common.delete')}
                            </AlertDialogAction>
                          </AlertDialogFooter>
                        </AlertDialogContent>
                      </AlertDialog>
                    </div>
                  </td>
                )}
              </tr>
            ))}
            {courses?.length === 0 && (
              <tr>
                <td
                  colSpan={canManage ? 5 : 4}
                  className="p-8 text-center text-muted-foreground"
                >
                  <BookOpen className="mx-auto mb-2 h-8 w-8 opacity-40" />
                  {t('courses.noCourses')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <CourseDialog open={dialogOpen} onClose={handleDialogClose} course={editingCourse} />
    </div>
  );
}
