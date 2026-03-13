import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Users, UserPlus, Shield, Search, UserCheck } from 'lucide-react';
import {
  Avatar,
  AvatarFallback,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  Input,
  Skeleton,
} from '@itenium-forge/ui';
import { fetchUsers, type User } from '@/api/client';
import { useAuthStore } from '@/stores';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  Separator,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui-extras';

function getRoleBadgeClass(role: string) {
  switch (role.toLowerCase()) {
    case 'backoffice':
      return 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300 border-purple-200';
    case 'manager':
      return 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300 border-blue-200';
    default:
      return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300 border-green-200';
  }
}

function getRoleLabel(role: string, t: (key: string) => string) {
  switch (role.toLowerCase()) {
    case 'backoffice':
      return t('userAdmin.backofficeRole');
    case 'manager':
      return t('userAdmin.managerRole');
    default:
      return t('userAdmin.learnerRole');
  }
}

function getUserInitials(user: User): string {
  if (user.firstName && user.lastName) {
    return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
  }
  if (user.firstName) return user.firstName[0].toUpperCase();
  if (user.lastName) return user.lastName[0].toUpperCase();
  return user.userName.slice(0, 2).toUpperCase();
}

function getAvatarColor(userId: string): string {
  const colors = [
    'bg-purple-500',
    'bg-blue-500',
    'bg-green-500',
    'bg-orange-500',
    'bg-pink-500',
    'bg-teal-500',
    'bg-indigo-500',
    'bg-rose-500',
  ];
  const index = userId.charCodeAt(0) % colors.length;
  return colors[index];
}

interface UserDetailDialogProps {
  user: User | null;
  open: boolean;
  onClose: () => void;
}

function UserDetailDialog({ user, open, onClose }: UserDetailDialogProps) {
  const { t } = useTranslation();

  if (!user) return null;

  return (
    <Dialog open={open} onOpenChange={(o: boolean) => !o && onClose()}>
      <DialogContent className="sm:max-w-[440px]">
        <DialogHeader>
          <DialogTitle>{t('userAdmin.userDetails')}</DialogTitle>
          <DialogDescription>{user.email}</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="flex items-center gap-4">
            <Avatar className="h-16 w-16">
              <AvatarFallback className={`text-lg font-bold text-white ${getAvatarColor(user.id)}`}>
                {getUserInitials(user)}
              </AvatarFallback>
            </Avatar>
            <div>
              <p className="text-xl font-semibold">
                {user.firstName || user.lastName
                  ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim()
                  : user.userName}
              </p>
              <p className="text-sm text-muted-foreground">@{user.userName}</p>
            </div>
          </div>
          <Separator />
          <div className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <p className="font-medium text-muted-foreground">{t('userAdmin.email')}</p>
              <p className="mt-0.5">{user.email}</p>
            </div>
            <div>
              <p className="font-medium text-muted-foreground">{t('userAdmin.username')}</p>
              <p className="mt-0.5">{user.userName}</p>
            </div>
            {user.firstName && (
              <div>
                <p className="font-medium text-muted-foreground">{t('userAdmin.firstName')}</p>
                <p className="mt-0.5">{user.firstName}</p>
              </div>
            )}
            {user.lastName && (
              <div>
                <p className="font-medium text-muted-foreground">{t('userAdmin.lastName')}</p>
                <p className="mt-0.5">{user.lastName}</p>
              </div>
            )}
          </div>
          <Separator />
          <div>
            <p className="mb-2 font-medium text-muted-foreground">{t('userAdmin.roles')}</p>
            <div className="flex flex-wrap gap-2">
              {user.roles.map((role) => (
                <span
                  key={role}
                  className={`inline-flex items-center rounded-full border px-3 py-1 text-xs font-medium ${getRoleBadgeClass(role)}`}
                >
                  {getRoleLabel(role, t)}
                </span>
              ))}
              {user.roles.length === 0 && <span className="text-sm text-muted-foreground">—</span>}
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

interface UserRowSkeletonProps {
  count?: number;
}

function UserRowSkeleton({ count = 5 }: UserRowSkeletonProps) {
  return (
    <>
      {Array.from({ length: count }).map((_, i) => (
        <tr key={i} className="border-b">
          <td className="p-3">
            <div className="flex items-center gap-3">
              <Skeleton className="h-10 w-10 rounded-full" />
              <div className="space-y-1">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-3 w-24" />
              </div>
            </div>
          </td>
          <td className="p-3">
            <Skeleton className="h-4 w-40" />
          </td>
          <td className="p-3">
            <Skeleton className="h-6 w-20 rounded-full" />
          </td>
          <td className="p-3">
            <Skeleton className="h-8 w-24" />
          </td>
        </tr>
      ))}
    </>
  );
}

type RoleFilter = 'all' | 'backoffice' | 'manager' | 'learner';

export function UserAdmin() {
  const { t } = useTranslation();
  const { user: currentUser } = useAuthStore();

  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<RoleFilter>('all');
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  const { data: users, isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
    enabled: currentUser?.isBackOffice ?? false,
  });

  const filteredUsers = useMemo(() => {
    if (!users) return [];
    return users.filter((u) => {
      const matchesSearch =
        !search ||
        u.userName.toLowerCase().includes(search.toLowerCase()) ||
        u.email.toLowerCase().includes(search.toLowerCase()) ||
        (u.firstName?.toLowerCase().includes(search.toLowerCase()) ?? false) ||
        (u.lastName?.toLowerCase().includes(search.toLowerCase()) ?? false);

      const matchesRole =
        roleFilter === 'all' ||
        (roleFilter === 'backoffice' && u.roles.some((r) => r.toLowerCase() === 'backoffice')) ||
        (roleFilter === 'manager' && u.roles.some((r) => r.toLowerCase() === 'manager')) ||
        (roleFilter === 'learner' &&
          !u.roles.some((r) => r.toLowerCase() === 'backoffice' || r.toLowerCase() === 'manager'));

      return matchesSearch && matchesRole;
    });
  }, [users, search, roleFilter]);

  const stats = useMemo(() => {
    if (!users) return { total: 0, backoffice: 0, managers: 0, learners: 0 };
    return {
      total: users.length,
      backoffice: users.filter((u) => u.roles.some((r) => r.toLowerCase() === 'backoffice')).length,
      managers: users.filter((u) => u.roles.some((r) => r.toLowerCase() === 'manager')).length,
      learners: users.filter(
        (u) => !u.roles.some((r) => r.toLowerCase() === 'backoffice' || r.toLowerCase() === 'manager'),
      ).length,
    };
  }, [users]);

  if (!currentUser?.isBackOffice) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center text-center">
        <Shield className="mb-4 h-16 w-16 text-destructive/50" />
        <h2 className="text-2xl font-bold">{t('userAdmin.accessDenied')}</h2>
        <p className="mt-2 text-muted-foreground">{t('userAdmin.accessDeniedDesc')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold">
            <Users className="h-8 w-8 text-primary" />
            {t('userAdmin.title')}
          </h1>
          <p className="mt-1 text-muted-foreground">{t('userAdmin.subtitle')}</p>
        </div>
        <Button disabled className="gap-2 opacity-60" title={t('userAdmin.comingSoon')}>
          <UserPlus className="h-4 w-4" />
          {t('userAdmin.addUser')}
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <Card className="border-l-4 border-l-primary">
          <CardContent className="pt-4">
            <p className="text-sm text-muted-foreground">{t('userAdmin.totalUsers')}</p>
            <p className="text-3xl font-bold">{isLoading ? '…' : stats.total}</p>
          </CardContent>
        </Card>
        <Card className="border-l-4 border-l-purple-500">
          <CardContent className="pt-4">
            <p className="text-sm text-muted-foreground">{t('userAdmin.filterBackOffice')}</p>
            <p className="text-3xl font-bold text-purple-600 dark:text-purple-400">
              {isLoading ? '…' : stats.backoffice}
            </p>
          </CardContent>
        </Card>
        <Card className="border-l-4 border-l-blue-500">
          <CardContent className="pt-4">
            <p className="text-sm text-muted-foreground">{t('userAdmin.filterManagers')}</p>
            <p className="text-3xl font-bold text-blue-600 dark:text-blue-400">{isLoading ? '…' : stats.managers}</p>
          </CardContent>
        </Card>
        <Card className="border-l-4 border-l-green-500">
          <CardContent className="pt-4">
            <p className="text-sm text-muted-foreground">{t('userAdmin.filterLearners')}</p>
            <p className="text-3xl font-bold text-green-600 dark:text-green-400">{isLoading ? '…' : stats.learners}</p>
          </CardContent>
        </Card>
      </div>

      {/* Search + Filter Tabs */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center gap-3">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('userAdmin.searchPlaceholder')}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <Tabs value={roleFilter} onValueChange={(v: string) => setRoleFilter(v as RoleFilter)} className="w-full">
            <div className="border-b px-4">
              <TabsList className="h-10 bg-transparent p-0">
                <TabsTrigger
                  value="all"
                  className="rounded-none border-b-2 border-transparent px-4 data-[state=active]:border-primary data-[state=active]:bg-transparent"
                >
                  {t('userAdmin.filterAll')}
                  <Badge variant="secondary" className="ml-2 text-xs">
                    {stats.total}
                  </Badge>
                </TabsTrigger>
                <TabsTrigger
                  value="backoffice"
                  className="rounded-none border-b-2 border-transparent px-4 data-[state=active]:border-primary data-[state=active]:bg-transparent"
                >
                  {t('userAdmin.filterBackOffice')}
                  <Badge variant="secondary" className="ml-2 text-xs">
                    {stats.backoffice}
                  </Badge>
                </TabsTrigger>
                <TabsTrigger
                  value="manager"
                  className="rounded-none border-b-2 border-transparent px-4 data-[state=active]:border-primary data-[state=active]:bg-transparent"
                >
                  {t('userAdmin.filterManagers')}
                  <Badge variant="secondary" className="ml-2 text-xs">
                    {stats.managers}
                  </Badge>
                </TabsTrigger>
                <TabsTrigger
                  value="learner"
                  className="rounded-none border-b-2 border-transparent px-4 data-[state=active]:border-primary data-[state=active]:bg-transparent"
                >
                  {t('userAdmin.filterLearners')}
                  <Badge variant="secondary" className="ml-2 text-xs">
                    {stats.learners}
                  </Badge>
                </TabsTrigger>
              </TabsList>
            </div>

            <TabsContent value={roleFilter} className="mt-0">
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b bg-muted/30">
                      <th className="p-3 text-left text-sm font-semibold">{t('userAdmin.username')}</th>
                      <th className="p-3 text-left text-sm font-semibold">{t('userAdmin.email')}</th>
                      <th className="p-3 text-left text-sm font-semibold">{t('userAdmin.roles')}</th>
                      <th className="p-3 text-right text-sm font-semibold"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {isLoading ? (
                      <UserRowSkeleton />
                    ) : filteredUsers.length === 0 ? (
                      <tr>
                        <td colSpan={4} className="p-8 text-center text-muted-foreground">
                          <Users className="mx-auto mb-2 h-8 w-8 opacity-40" />
                          {t('userAdmin.noUsers')}
                        </td>
                      </tr>
                    ) : (
                      filteredUsers.map((u) => (
                        <tr key={u.id} className="border-b transition-colors hover:bg-muted/30">
                          <td className="p-3">
                            <div className="flex items-center gap-3">
                              <Avatar className="h-10 w-10">
                                <AvatarFallback className={`text-sm font-semibold text-white ${getAvatarColor(u.id)}`}>
                                  {getUserInitials(u)}
                                </AvatarFallback>
                              </Avatar>
                              <div>
                                <p className="font-medium">
                                  {u.firstName || u.lastName
                                    ? `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim()
                                    : u.userName}
                                </p>
                                <p className="text-xs text-muted-foreground">@{u.userName}</p>
                              </div>
                            </div>
                          </td>
                          <td className="p-3 text-sm text-muted-foreground">{u.email}</td>
                          <td className="p-3">
                            <div className="flex flex-wrap gap-1.5">
                              {u.roles.map((role) => (
                                <span
                                  key={role}
                                  className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${getRoleBadgeClass(role)}`}
                                >
                                  {getRoleLabel(role, t)}
                                </span>
                              ))}
                              {u.roles.length === 0 && (
                                <span
                                  className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${getRoleBadgeClass('learner')}`}
                                >
                                  {t('userAdmin.learnerRole')}
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="p-3 text-right">
                            <Button
                              variant="ghost"
                              size="sm"
                              className="gap-1.5"
                              onClick={() => {
                                setSelectedUser(u);
                                setDetailOpen(true);
                              }}
                            >
                              <UserCheck className="h-3.5 w-3.5" />
                              {t('userAdmin.viewDetails')}
                            </Button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      <UserDetailDialog
        user={selectedUser}
        open={detailOpen}
        onClose={() => {
          setDetailOpen(false);
          setSelectedUser(null);
        }}
      />
    </div>
  );
}
