import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  CardDescription,
  Badge,
  Button,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Skeleton,
} from '@itenium-forge/ui';
import { ClipboardList, BookOpen, Users, Search, Plus, Trash2, Info, CheckCircle } from 'lucide-react';
import { toast } from 'sonner';
import { Separator } from '@/components/ui-extras';
import { useTeamStore } from '@/stores';
import {
  fetchEnrollments,
  fetchCourses,
  fetchUsers,
  enrollInCourse,
  unenrollFromCourse,
  type Enrollment,
  type Course,
  type User,
} from '@/api/client';

function getUserDisplayName(user: User): string {
  if (user.firstName && user.lastName) return `${user.firstName} ${user.lastName}`;
  return user.userName;
}

function getUserInitials(user: User): string {
  if (user.firstName && user.lastName) {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }
  return user.userName.slice(0, 2).toUpperCase();
}

interface MemberCardProps {
  user: User;
  enrollments: Enrollment[];
  courses: Course[];
  isSelected: boolean;
  onSelect: () => void;
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
function MemberCard({ user, enrollments, courses: _courses, isSelected, onSelect }: MemberCardProps) {
  const userEnrollments = enrollments.filter((e) => e.learnerId === user.id);
  const completedCount = userEnrollments.filter((e) => e.completedAt !== null).length;

  return (
    <button
      onClick={onSelect}
      className={`w-full text-left p-3 rounded-lg border transition-all ${
        isSelected
          ? 'border-primary bg-primary/5 ring-1 ring-primary'
          : 'border-border hover:border-muted-foreground/50 hover:bg-muted/50'
      }`}
    >
      <div className="flex items-center gap-3">
        <div
          className={`size-9 rounded-full flex items-center justify-center text-xs font-bold shrink-0 ${
            isSelected
              ? 'bg-primary text-primary-foreground'
              : 'bg-gradient-to-br from-indigo-500 to-purple-600 text-white'
          }`}
        >
          {getUserInitials(user)}
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-medium text-sm truncate">{getUserDisplayName(user)}</p>
          <p className="text-xs text-muted-foreground truncate">@{user.userName}</p>
        </div>
        <div className="text-right shrink-0">
          <Badge variant="outline" className="text-xs">
            {userEnrollments.length} enrolled
          </Badge>
          {completedCount > 0 && (
            <p className="text-xs text-green-600 dark:text-green-400 mt-0.5">{completedCount} done</p>
          )}
        </div>
      </div>
    </button>
  );
}

interface CourseCardProps {
  course: Course;
  isEnrolled: boolean;
  onAssign: () => void;
  isAssigning: boolean;
}

function CourseCard({ course, isEnrolled, onAssign, isAssigning }: CourseCardProps) {
  const levelColors: Record<string, string> = {
    Beginner: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
    Intermediate: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
    Advanced: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
  };

  return (
    <div
      className={`p-3 rounded-lg border transition-colors ${
        isEnrolled
          ? 'border-green-200 bg-green-50/50 dark:border-green-800 dark:bg-green-950/30'
          : 'border-border hover:bg-muted/30'
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <BookOpen className="size-3.5 text-muted-foreground shrink-0" />
            <p className="font-medium text-sm truncate">{course.name}</p>
          </div>
          <div className="flex items-center gap-1.5 flex-wrap">
            {course.category && (
              <Badge variant="outline" className="text-xs px-1.5 py-0">
                {course.category}
              </Badge>
            )}
            {course.level && (
              <span
                className={`text-xs px-1.5 py-0.5 rounded-full ${levelColors[course.level] ?? 'bg-gray-100 text-gray-600'}`}
              >
                {course.level}
              </span>
            )}
          </div>
        </div>
        <div className="shrink-0">
          {isEnrolled ? (
            <div className="flex items-center gap-1 text-green-600 dark:text-green-400 text-xs font-medium">
              <CheckCircle className="size-3.5" />
              Enrolled
            </div>
          ) : (
            <Button size="sm" variant="outline" className="gap-1 h-7 text-xs" onClick={onAssign} disabled={isAssigning}>
              <Plus className="size-3" />
              Assign
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

export function Assignments() {
  const { t } = useTranslation();
  const { selectedTeam } = useTeamStore();
  const queryClient = useQueryClient();

  const [selectedMemberId, setSelectedMemberId] = useState<string | null>(null);
  const [memberSearch, setMemberSearch] = useState('');
  const [courseSearch, setCourseSearch] = useState('');
  const [enrollmentFilter, setEnrollmentFilter] = useState('');

  const { data: enrollments = [], isLoading: enrollmentsLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
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

  const enrollMutation = useMutation({
    mutationFn: (courseId: number) => enrollInCourse(courseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments'] });
      toast.success(t('assignments.assignSuccess'));
    },
    onError: () => {
      toast.error(t('assignments.alreadyEnrolled'));
    },
  });

  const unenrollMutation = useMutation({
    mutationFn: (courseId: number) => unenrollFromCourse(courseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments'] });
      toast.success(t('assignments.removeSuccess'));
    },
    onError: () => {
      toast.error(t('assignments.removeError'));
    },
  });

  const isLoading = enrollmentsLoading || coursesLoading || usersLoading;

  const filteredMembers = users.filter((u) => {
    const name = getUserDisplayName(u).toLowerCase();
    return name.includes(memberSearch.toLowerCase()) || u.email.toLowerCase().includes(memberSearch.toLowerCase());
  });

  const selectedMember = users.find((u) => u.id === selectedMemberId) ?? null;
  const selectedMemberEnrollments = enrollments.filter((e) => e.learnerId === selectedMemberId);
  const enrolledCourseIds = new Set(selectedMemberEnrollments.map((e) => e.courseId));

  const filteredCourses = courses.filter((c) => c.name.toLowerCase().includes(courseSearch.toLowerCase()));

  // All enrollments table rows
  const allEnrollmentRows = enrollments
    .filter((e) => {
      if (!enrollmentFilter) return true;
      const user = users.find((u) => u.id === e.learnerId);
      const name = user ? getUserDisplayName(user).toLowerCase() : e.learnerId.toLowerCase();
      return name.includes(enrollmentFilter.toLowerCase());
    })
    .map((e) => {
      const user = users.find((u) => u.id === e.learnerId);
      const course = courses.find((c) => c.id === e.courseId);
      return {
        ...e,
        userName: user ? getUserDisplayName(user) : e.learnerId.slice(0, 8),
        courseName: course?.name ?? `Course #${e.courseId}`,
      };
    });

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t('assignments.title')}</h1>
        <p className="text-muted-foreground mt-1">
          {selectedTeam ? selectedTeam.name : ''} &mdash; {t('assignments.subtitle')}
        </p>
      </div>

      {/* Manager Note */}
      <Card className="border-blue-200 bg-blue-50/50 dark:border-blue-800 dark:bg-blue-950/30">
        <CardContent className="flex items-start gap-3 pt-4 pb-4">
          <Info className="size-4 text-blue-600 dark:text-blue-400 mt-0.5 shrink-0" />
          <p className="text-sm text-blue-700 dark:text-blue-300">{t('assignments.managerNote')}</p>
        </CardContent>
      </Card>

      {/* Two-Panel Layout */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Left Panel: Team Members */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Users className="size-5 text-muted-foreground" />
              <CardTitle className="text-base">{t('assignments.teamMembers')}</CardTitle>
            </div>
            <CardDescription>Select a member to see their courses</CardDescription>
            <div className="relative mt-2">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <Input
                placeholder="Search members..."
                value={memberSearch}
                onChange={(e) => setMemberSearch(e.target.value)}
                className="pl-9"
              />
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2 max-h-96 overflow-y-auto pr-1">
              {isLoading ? (
                Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-16 w-full rounded-lg" />)
              ) : filteredMembers.length === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-8">No members found</p>
              ) : (
                filteredMembers.map((user) => (
                  <MemberCard
                    key={user.id}
                    user={user}
                    enrollments={enrollments}
                    courses={courses}
                    isSelected={selectedMemberId === user.id}
                    onSelect={() => setSelectedMemberId(selectedMemberId === user.id ? null : user.id)}
                  />
                ))
              )}
            </div>
          </CardContent>
        </Card>

        {/* Right Panel: Course Catalog */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <BookOpen className="size-5 text-muted-foreground" />
              <CardTitle className="text-base">{t('assignments.courseCatalog')}</CardTitle>
            </div>
            <CardDescription>
              {selectedMember ? `Assigning to: ${getUserDisplayName(selectedMember)}` : 'Select a team member first'}
            </CardDescription>
            <div className="relative mt-2">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <Input
                placeholder="Search courses..."
                value={courseSearch}
                onChange={(e) => setCourseSearch(e.target.value)}
                className="pl-9"
              />
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2 max-h-96 overflow-y-auto pr-1">
              {isLoading ? (
                Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-14 w-full rounded-lg" />)
              ) : filteredCourses.length === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-8">No courses found</p>
              ) : (
                filteredCourses.map((course) => (
                  <CourseCard
                    key={course.id}
                    course={course}
                    isEnrolled={enrolledCourseIds.has(course.id)}
                    onAssign={() => enrollMutation.mutate(course.id)}
                    isAssigning={enrollMutation.isPending}
                  />
                ))
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      <Separator />

      {/* All Enrollments Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ClipboardList className="size-5 text-muted-foreground" />
              <div>
                <CardTitle className="text-base">{t('assignments.currentEnrollments')}</CardTitle>
                <CardDescription>All course-member assignments</CardDescription>
              </div>
            </div>
            <div className="relative w-56">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <Input
                placeholder={t('assignments.filterByMember') + '...'}
                value={enrollmentFilter}
                onChange={(e) => setEnrollmentFilter(e.target.value)}
                className="pl-9"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('assignments.assignedTo')}</TableHead>
                <TableHead>{t('assignments.course')}</TableHead>
                <TableHead>{t('assignments.enrolledAt')}</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Actions</TableHead>
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
              ) : allEnrollmentRows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-center py-12 text-muted-foreground">
                    {t('assignments.noAssignments')}
                  </TableCell>
                </TableRow>
              ) : (
                allEnrollmentRows.map((row) => (
                  <TableRow key={row.id} className="group hover:bg-muted/50 transition-colors">
                    <TableCell>
                      <span className="font-medium text-sm">{row.userName}</span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">{row.courseName}</span>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {new Date(row.enrolledAt).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                      })}
                    </TableCell>
                    <TableCell>
                      {row.completedAt ? (
                        <Badge className="bg-green-100 text-green-700 border-green-200 dark:bg-green-900 dark:text-green-300">
                          <CheckCircle className="size-3 mr-1" />
                          Completed
                        </Badge>
                      ) : (
                        <Badge variant="outline" className="text-blue-600 border-blue-300">
                          Active
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="opacity-0 group-hover:opacity-100 transition-opacity text-destructive hover:text-destructive hover:bg-destructive/10"
                        onClick={() => unenrollMutation.mutate(row.courseId)}
                        disabled={unenrollMutation.isPending}
                      >
                        <Trash2 className="size-3.5 mr-1" />
                        {t('assignments.removeAssignment')}
                      </Button>
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
