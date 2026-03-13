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
  Button,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Skeleton,
  Avatar,
  AvatarFallback,
} from '@itenium-forge/ui';
import { Users, UserCheck, Award, Search, Mail, AlertTriangle } from 'lucide-react';
import { useTeamStore } from '@/stores';
import { fetchUsers, fetchEnrollments, type User, type Enrollment } from '@/api/client';

function getMemberInitials(user: User): string {
  if (user.firstName && user.lastName) {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }
  return user.userName.slice(0, 2).toUpperCase();
}

function getMemberDisplayName(user: User): string {
  if (user.firstName && user.lastName) {
    return `${user.firstName} ${user.lastName}`;
  }
  return user.userName;
}

function getRoleBadgeVariant(role: string): 'default' | 'secondary' | 'outline' {
  if (role.toLowerCase().includes('backoffice') || role.toLowerCase().includes('manager')) {
    return 'default';
  }
  return 'secondary';
}

interface MemberStats {
  enrolled: number;
  completed: number;
}

function getMemberStats(userId: string, enrollments: Enrollment[]): MemberStats {
  const userEnrollments = enrollments.filter((e) => e.userId === userId);
  return {
    enrolled: userEnrollments.length,
    completed: userEnrollments.filter((e) => e.completedAt !== null).length,
  };
}

function MemberSkeleton() {
  return (
    <TableRow>
      {[1, 2, 3, 4, 5, 6].map((i) => (
        <TableCell key={i}>
          <Skeleton className="h-4 w-full" />
        </TableCell>
      ))}
    </TableRow>
  );
}

export function TeamMembers() {
  const { t } = useTranslation();
  const { selectedTeam } = useTeamStore();
  const [searchQuery, setSearchQuery] = useState('');
  const [accessDenied, setAccessDenied] = useState(false);

  const {
    data: users,
    isLoading: usersLoading,
    error: usersError,
  } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
    retry: false,
    throwOnError: false,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    onError: (err: any) => {
      if (err?.response?.status === 403) {
        setAccessDenied(true);
      }
    },
  });

  const { data: enrollments = [], isLoading: enrollmentsLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  const isLoading = usersLoading || enrollmentsLoading;

  const is403 =
    accessDenied ||
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (usersError && (usersError as any)?.response?.status === 403);

  const filteredUsers = (users ?? []).filter((user) => {
    const name = getMemberDisplayName(user).toLowerCase();
    const email = user.email.toLowerCase();
    const query = searchQuery.toLowerCase();
    return name.includes(query) || email.includes(query);
  });

  const totalMembers = users?.length ?? 0;
  const activeLearners = users?.filter((u) => {
    const stats = getMemberStats(u.id, enrollments);
    return stats.enrolled > 0;
  }).length ?? 0;
  const totalCompleted = enrollments.filter((e) => e.completedAt !== null).length;

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('teamMembers.title')}</h1>
          <p className="text-muted-foreground mt-1">
            {selectedTeam ? selectedTeam.name : ''} &mdash; {t('teamMembers.subtitle')}
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card className="border-l-4 border-l-blue-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('teamMembers.member')}s</CardTitle>
            <div className="rounded-full bg-blue-100 p-2 dark:bg-blue-900">
              <Users className="size-4 text-blue-600 dark:text-blue-400" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{isLoading ? <Skeleton className="h-8 w-12" /> : totalMembers}</div>
            <p className="text-xs text-muted-foreground mt-1">Total team members</p>
          </CardContent>
        </Card>

        <Card className="border-l-4 border-l-green-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Active Learners</CardTitle>
            <div className="rounded-full bg-green-100 p-2 dark:bg-green-900">
              <UserCheck className="size-4 text-green-600 dark:text-green-400" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{isLoading ? <Skeleton className="h-8 w-12" /> : activeLearners}</div>
            <p className="text-xs text-muted-foreground mt-1">Members with active enrollments</p>
          </CardContent>
        </Card>

        <Card className="border-l-4 border-l-amber-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('teamMembers.completedCourses')}</CardTitle>
            <div className="rounded-full bg-amber-100 p-2 dark:bg-amber-900">
              <Award className="size-4 text-amber-600 dark:text-amber-400" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{isLoading ? <Skeleton className="h-8 w-12" /> : totalCompleted}</div>
            <p className="text-xs text-muted-foreground mt-1">Courses completed by team</p>
          </CardContent>
        </Card>
      </div>

      {/* Access Denied Message */}
      {is403 && (
        <Card className="border-amber-200 bg-amber-50 dark:border-amber-800 dark:bg-amber-950">
          <CardContent className="flex items-center gap-3 pt-6">
            <AlertTriangle className="size-5 text-amber-600 dark:text-amber-400 shrink-0" />
            <p className="text-sm text-amber-700 dark:text-amber-300">{t('teamMembers.contactBackoffice')}</p>
          </CardContent>
        </Card>
      )}

      {/* Members Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>{t('teamMembers.title')}</CardTitle>
              <CardDescription>{t('teamMembers.subtitle')}</CardDescription>
            </div>
            <div className="relative w-64">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <Input
                placeholder={t('common.search') + '...'}
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('teamMembers.member')}</TableHead>
                <TableHead>{t('teamMembers.email')}</TableHead>
                <TableHead>{t('teamMembers.role')}</TableHead>
                <TableHead className="text-center">{t('teamMembers.enrolledCourses')}</TableHead>
                <TableHead className="text-center">{t('teamMembers.completedCourses')}</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <>
                  <MemberSkeleton />
                  <MemberSkeleton />
                  <MemberSkeleton />
                </>
              ) : filteredUsers.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-12 text-muted-foreground">
                    {is403 ? t('teamMembers.contactBackoffice') : t('teamMembers.noMembers')}
                  </TableCell>
                </TableRow>
              ) : (
                filteredUsers.map((user) => {
                  const stats = getMemberStats(user.id, enrollments);
                  const displayName = getMemberDisplayName(user);
                  const initials = getMemberInitials(user);
                  const primaryRole = user.roles[0] ?? 'learner';

                  return (
                    <TableRow key={user.id} className="group hover:bg-muted/50 transition-colors">
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <Avatar className="size-9">
                            <AvatarFallback className="bg-gradient-to-br from-blue-500 to-purple-600 text-white text-xs font-semibold">
                              {initials}
                            </AvatarFallback>
                          </Avatar>
                          <div>
                            <p className="font-medium">{displayName}</p>
                            <p className="text-xs text-muted-foreground">@{user.userName}</p>
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2 text-sm">
                          <Mail className="size-3.5 text-muted-foreground" />
                          <span>{user.email}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={getRoleBadgeVariant(primaryRole)}>
                          {primaryRole.toLowerCase().includes('manager')
                            ? t('teamMembers.managerRole')
                            : t('teamMembers.learnerRole')}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <span className="inline-flex items-center justify-center size-8 rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300 text-sm font-semibold">
                          {stats.enrolled}
                        </span>
                      </TableCell>
                      <TableCell className="text-center">
                        <span className="inline-flex items-center justify-center size-8 rounded-full bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300 text-sm font-semibold">
                          {stats.completed}
                        </span>
                      </TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="sm" className="opacity-0 group-hover:opacity-100 transition-opacity">
                          {t('teamMembers.viewDetails')}
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
