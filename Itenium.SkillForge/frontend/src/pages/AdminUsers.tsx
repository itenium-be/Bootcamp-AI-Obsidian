import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Users, UserPlus, Archive, RotateCcw, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  Badge,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@itenium-forge/ui';
import {
  fetchUsers,
  createUser,
  archiveUser,
  restoreUser,
  fetchOrphanedConsultants,
  type UserRecord,
  type CreateUserPayload,
} from '@/api/client';

const VALID_ROLES = ['learner', 'manager', 'backoffice'] as const;
type Role = (typeof VALID_ROLES)[number];

const TEAMS = [
  { id: 1, name: 'Java' },
  { id: 2, name: '.NET' },
  { id: 3, name: 'PO & Analysis' },
  { id: 4, name: 'QA' },
];

function RoleBadge({ role }: { role: string }) {
  const variant =
    role === 'backoffice' ? 'destructive' : role === 'manager' ? 'default' : 'secondary';
  return <Badge variant={variant}>{role}</Badge>;
}

function CreateUserDialog({ onCreated }: { onCreated: () => void }) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<CreateUserPayload>({
    userName: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    role: 'learner',
    teamIds: [],
  });

  const mutation = useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      toast.success(t('users.createSuccess'));
      setOpen(false);
      setForm({ userName: '', email: '', password: '', firstName: '', lastName: '', role: 'learner', teamIds: [] });
      onCreated();
    },
    onError: () => {
      toast.error(t('common.error'));
    },
  });

  const toggleTeam = (teamId: number) => {
    setForm((f) => ({
      ...f,
      teamIds: f.teamIds.includes(teamId) ? f.teamIds.filter((id) => id !== teamId) : [...f.teamIds, teamId],
    }));
  };

  return (
    <>
      <Button onClick={() => setOpen(true)}>
        <UserPlus className="size-4 mr-2" />
        {t('users.createUser')}
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{t('users.createUser')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="firstName">{t('users.firstName')}</Label>
                <Input
                  id="firstName"
                  value={form.firstName}
                  onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
                />
              </div>
              <div>
                <Label htmlFor="lastName">{t('users.lastName')}</Label>
                <Input
                  id="lastName"
                  value={form.lastName}
                  onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
                />
              </div>
            </div>
            <div>
              <Label htmlFor="userName">{t('auth.username')}</Label>
              <Input
                id="userName"
                value={form.userName}
                onChange={(e) => setForm((f) => ({ ...f, userName: e.target.value }))}
              />
            </div>
            <div>
              <Label htmlFor="email">{t('learners.email')}</Label>
              <Input
                id="email"
                type="email"
                value={form.email}
                onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
              />
            </div>
            <div>
              <Label htmlFor="password">{t('auth.password')}</Label>
              <Input
                id="password"
                type="password"
                value={form.password}
                onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
              />
            </div>
            <div>
              <Label>{t('users.role')}</Label>
              <Select value={form.role} onValueChange={(v) => setForm((f) => ({ ...f, role: v as Role }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {VALID_ROLES.map((role) => (
                    <SelectItem key={role} value={role}>
                      {role}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            {form.role !== 'backoffice' && (
              <div>
                <Label>{t('users.teams')}</Label>
                <div className="flex flex-wrap gap-2 mt-1">
                  {TEAMS.map((team) => (
                    <Button
                      key={team.id}
                      variant={form.teamIds.includes(team.id) ? 'default' : 'outline'}
                      size="sm"
                      onClick={() => toggleTeam(team.id)}
                    >
                      {team.name}
                    </Button>
                  ))}
                </div>
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button
              onClick={() => mutation.mutate(form)}
              disabled={mutation.isPending || !form.userName || !form.email || !form.password}
            >
              {mutation.isPending ? t('common.loading') : t('users.createUser')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

export function AdminUsers() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data: users, isLoading } = useQuery({
    queryKey: ['admin-users'],
    queryFn: fetchUsers,
  });

  const { data: orphaned } = useQuery({
    queryKey: ['orphaned-consultants'],
    queryFn: fetchOrphanedConsultants,
  });

  const archiveMutation = useMutation({
    mutationFn: archiveUser,
    onSuccess: () => {
      toast.success(t('users.archiveSuccess'));
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
    },
    onError: () => toast.error(t('common.error')),
  });

  const restoreMutation = useMutation({
    mutationFn: restoreUser,
    onSuccess: () => {
      toast.success(t('users.restoreSuccess'));
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
    },
    onError: () => toast.error(t('common.error')),
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['admin-users'] });
    queryClient.invalidateQueries({ queryKey: ['orphaned-consultants'] });
  };

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Users className="size-8" />
            {t('users.title')}
          </h1>
          <p className="text-muted-foreground">{t('users.description')}</p>
        </div>
        <CreateUserDialog onCreated={invalidate} />
      </div>

      {/* Orphaned consultants alert */}
      {orphaned && orphaned.length > 0 && (
        <Card className="border-yellow-500">
          <CardHeader>
            <CardTitle className="text-sm flex items-center gap-2 text-yellow-700">
              <AlertTriangle className="size-4" />
              {t('users.orphanedAlert', { count: orphaned.length })}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {orphaned.map((u) => (
                <Badge key={u.id} variant="outline">
                  {u.firstName} {u.lastName}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Users table */}
      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('learners.name')}</th>
              <th className="p-3 text-left font-medium">{t('learners.email')}</th>
              <th className="p-3 text-left font-medium">{t('users.role')}</th>
              <th className="p-3 text-left font-medium">{t('users.teams')}</th>
              <th className="p-3 text-left font-medium">{t('courses.status')}</th>
              <th className="p-3 text-left font-medium">{t('courses.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {users?.map((user: UserRecord) => (
              <tr key={user.id} className={`border-b ${user.isArchived ? 'opacity-50' : ''}`}>
                <td className="p-3">
                  {user.firstName} {user.lastName}
                  <span className="text-xs text-muted-foreground ml-2">@{user.userName}</span>
                </td>
                <td className="p-3 text-muted-foreground">{user.email}</td>
                <td className="p-3">
                  <RoleBadge role={user.role} />
                </td>
                <td className="p-3">
                  {user.teamIds.length > 0 ? (
                    <div className="flex gap-1">
                      {user.teamIds.map((id) => (
                        <Badge key={id} variant="outline" className="text-xs">
                          {TEAMS.find((t) => t.id === id)?.name ?? id}
                        </Badge>
                      ))}
                    </div>
                  ) : (
                    <span className="text-muted-foreground">-</span>
                  )}
                </td>
                <td className="p-3">
                  {user.isArchived ? (
                    <Badge variant="destructive">{t('users.archived')}</Badge>
                  ) : (
                    <Badge variant="secondary">{t('common.active')}</Badge>
                  )}
                </td>
                <td className="p-3">
                  {user.isArchived ? (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => restoreMutation.mutate(user.id)}
                      disabled={restoreMutation.isPending}
                    >
                      <RotateCcw className="size-3 mr-1" />
                      {t('users.restore')}
                    </Button>
                  ) : (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => archiveMutation.mutate(user.id)}
                      disabled={archiveMutation.isPending}
                    >
                      <Archive className="size-3 mr-1" />
                      {t('users.archive')}
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {users?.length === 0 && (
              <tr>
                <td colSpan={6} className="p-3 text-center text-muted-foreground">
                  {t('users.noUsers')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
