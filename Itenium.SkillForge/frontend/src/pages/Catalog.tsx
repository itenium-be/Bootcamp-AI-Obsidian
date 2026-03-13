import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardFooter,
  Badge,
  Button,
  Input,
  Skeleton,
} from '@itenium-forge/ui';
import { BookOpen, Search, ChevronRight, Zap } from 'lucide-react';
import { toast } from 'sonner';
import { fetchCourses, fetchEnrollments, enrollInCourse, type Course } from '@/api/client';

function getLevelColor(level: string | null): string {
  if (!level) return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300';
  switch (level.toLowerCase()) {
    case 'beginner':
      return 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300';
    case 'intermediate':
      return 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300';
    case 'advanced':
      return 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300';
    default:
      return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300';
  }
}

function CourseCardSkeleton() {
  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <div className="flex gap-2 mb-2">
          <Skeleton className="h-5 w-20" />
          <Skeleton className="h-5 w-16" />
        </div>
        <Skeleton className="h-6 w-3/4" />
        <Skeleton className="h-4 w-full mt-1" />
        <Skeleton className="h-4 w-2/3" />
      </CardHeader>
      <CardFooter className="mt-auto pt-2">
        <Skeleton className="h-9 w-28" />
      </CardFooter>
    </Card>
  );
}

function CourseCard({
  course,
  isEnrolled,
  onEnroll,
  isEnrolling,
}: {
  course: Course;
  isEnrolled: boolean;
  onEnroll: (id: number) => void;
  isEnrolling: boolean;
}) {
  const { t } = useTranslation();

  return (
    <Card className="flex flex-col hover:shadow-md transition-shadow">
      <CardHeader className="pb-2">
        <div className="flex flex-wrap gap-2 mb-2">
          {course.category && (
            <Badge variant="outline" className="text-xs">
              {course.category}
            </Badge>
          )}
          {course.level && (
            <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${getLevelColor(course.level)}`}>
              {course.level}
            </span>
          )}
        </div>
        <CardTitle className="text-base leading-tight">{course.name}</CardTitle>
        {course.description && (
          <CardDescription className="line-clamp-2 text-sm">{course.description}</CardDescription>
        )}
      </CardHeader>
      <CardFooter className="mt-auto pt-2 flex gap-2">
        {isEnrolled ? (
          <>
            <Badge className="bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300 border-0">
              ✓ {t('catalog.enrolled')}
            </Badge>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/my-courses">
                {t('catalog.viewProgress')}
                <ChevronRight className="size-3 ml-1" />
              </Link>
            </Button>
          </>
        ) : (
          <Button
            size="sm"
            onClick={() => onEnroll(course.id)}
            disabled={isEnrolling}
            className="gap-1"
          >
            <Zap className="size-3" />
            {t('catalog.enrollNow')}
          </Button>
        )}
      </CardFooter>
    </Card>
  );
}

export function Catalog() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [level, setLevel] = useState('');

  const { data: courses, isLoading: coursesLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
  });

  const { data: enrollments } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  const enrollMutation = useMutation({
    mutationFn: enrollInCourse,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments'] });
      toast.success(t('catalog.enrollSuccess'));
    },
    onError: () => {
      toast.error(t('common.error'));
    },
  });

  const enrolledCourseIds = new Set(enrollments?.map((e) => e.courseId) ?? []);

  const filtered =
    courses?.filter((c) => {
      const matchSearch =
        !search ||
        c.name.toLowerCase().includes(search.toLowerCase()) ||
        (c.description?.toLowerCase().includes(search.toLowerCase()) ?? false);
      const matchCategory = !category || c.category === category;
      const matchLevel = !level || c.level === level;
      return matchSearch && matchCategory && matchLevel;
    }) ?? [];

  const categories = [...new Set(courses?.map((c) => c.category).filter((c): c is string => c !== null))] as string[];
  const levels = [...new Set(courses?.map((c) => c.level).filter((l): l is string => l !== null))] as string[];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">{t('catalog.title')}</h1>
        <p className="text-muted-foreground">{t('catalog.subtitle')}</p>
      </div>

      {/* Search and Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <Input
            placeholder={t('catalog.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
        <select
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          className="h-9 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
        >
          <option value="">{t('catalog.allCategories')}</option>
          {categories.map((cat) => (
            <option key={cat} value={cat}>
              {cat}
            </option>
          ))}
        </select>
        <select
          value={level}
          onChange={(e) => setLevel(e.target.value)}
          className="h-9 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
        >
          <option value="">{t('catalog.allLevels')}</option>
          {levels.map((lvl) => (
            <option key={lvl} value={lvl}>
              {lvl}
            </option>
          ))}
        </select>
      </div>

      {/* Course Grid */}
      {coursesLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <CourseCardSkeleton key={i} />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 gap-3 text-muted-foreground">
          <BookOpen className="size-12 opacity-30" />
          <p className="text-lg">{t('catalog.noCoursesFound')}</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map((course) => (
            <CourseCard
              key={course.id}
              course={course}
              isEnrolled={enrolledCourseIds.has(course.id)}
              onEnroll={(id) => enrollMutation.mutate(id)}
              isEnrolling={enrollMutation.isPending}
            />
          ))}
        </div>
      )}
    </div>
  );
}
