import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Users, Shield, Plus, Pencil, Trash2, Settings2, BookOpen, UserPlus, UserMinus, Search, ChevronDown, ChevronUp } from 'lucide-react';
import {
  Button,
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Skeleton,
  Avatar,
  AvatarFallback,
} from '@itenium-forge/ui';
import {
  fetchUserTeams,
  createTeam,
  updateTeam,
  deleteTeam,
  fetchTeamMembers,
  fetchUsers,
  addTeamMember,
  removeTeamMember,
  type Team,
  type User,
} from '@/api/client';
import { useAuthStore } from '@/stores';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Textarea,
} from '@/components/ui-extras';

const teamSchema = z.object({
  name: z.string().min(1, 'teamAdmin.teamNameRequired'),
  description: z.string().optional(),
});

type TeamFormValues = z.infer<typeof teamSchema>;

const TEAM_COLORS = [
  { bg: 'bg-purple-500', light: 'bg-purple-100', text: 'text-purple-700' },
  { bg: 'bg-blue-500', light: 'bg-blue-100', text: 'text-blue-700' },
  { bg: 'bg-green-500', light: 'bg-green-100', text: 'text-green-700' },
  { bg: 'bg-orange-500', light: 'bg-orange-100', text: 'text-orange-700' },
  { bg: 'bg-pink-500', light: 'bg-pink-100', text: 'text-pink-700' },
  { bg: 'bg-teal-500', light: 'bg-teal-100', text: 'text-teal-700' },
  { bg: 'bg-indigo-500', light: 'bg-indigo-100', text: 'text-indigo-700' },
  { bg: 'bg-rose-500', light: 'bg-rose-100', text: 'text-rose-700' },
];

function getTeamColor(teamId: number) {
  return TEAM_COLORS[teamId % TEAM_COLORS.length];
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
  const colors = ['bg-purple-500', 'bg-blue-500', 'bg-green-500', 'bg-orange-500', 'bg-pink-500', 'bg-teal-500', 'bg-indigo-500', 'bg-rose-500'];
  return colors[userId.charCodeAt(0) % colors.length];
}

// ─── Members Dialog ────────────────────────────────────────────────────────────

interface MembersDialogProps {
  team: Team;
  open: boolean;
  onClose: () => void;
}

function MembersDialog({ team, open, onClose }: MembersDialogProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [memberSearch, setMemberSearch] = useState('');
  const [showAddSection, setShowAddSection] = useState(false);

  const { data: members, isLoading: membersLoading } = useQuery({
    queryKey: ['team-members', team.id],
    queryFn: () => fetchTeamMembers(team.id),
    enabled: open,
  });

  const { data: allUsers } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
    enabled: open && showAddSection,
  });

  const addMutation = useMutation({
    mutationFn: (userId: string) => addTeamMember(team.id, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['team-members', team.id] });
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success('Member added successfully');
    },
    onError: () => toast.error(t('common.error')),
  });

  const removeMutation = useMutation({
    mutationFn: (userId: string) => removeTeamMember(team.id, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['team-members', team.id] });
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success('Member removed successfully');
    },
    onError: () => toast.error(t('common.error')),
  });

  const memberIds = new Set(members?.map((m) => m.id) ?? []);
  const availableUsers = (allUsers ?? []).filter(
    (u) => !memberIds.has(u.id) &&
      (!memberSearch ||
        u.userName.toLowerCase().includes(memberSearch.toLowerCase()) ||
        u.email.toLowerCase().includes(memberSearch.toLowerCase()) ||
        (u.firstName?.toLowerCase().includes(memberSearch.toLowerCase()) ?? false) ||
        (u.lastName?.toLowerCase().includes(memberSearch.toLowerCase()) ?? false)),
  );

  return (
    <Dialog open={open} onOpenChange={(o: boolean) => !o && onClose()}>
      <DialogContent className="sm:max-w-[520px]">
        <DialogHeader>
          <DialogTitle>
            {t('teamAdmin.memberCount')} — {team.name}
          </DialogTitle>
          <DialogDescription>
            {members?.length ?? 0} member(s)
          </DialogDescription>
        </DialogHeader>

        {/* Current Members */}
        <div className="max-h-56 overflow-y-auto space-y-2">
          {membersLoading ? (
            Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="flex items-center gap-3 p-2">
                <Skeleton className="h-8 w-8 rounded-full" />
                <div className="flex-1 space-y-1">
                  <Skeleton className="h-4 w-32" />
                  <Skeleton className="h-3 w-24" />
                </div>
              </div>
            ))
          ) : members && members.length > 0 ? (
            members.map((member) => (
              <div key={member.id} className="flex items-center gap-3 rounded-md p-2 hover:bg-muted/50">
                <Avatar className="h-8 w-8">
                  <AvatarFallback className={`text-xs font-semibold text-white ${getAvatarColor(member.id)}`}>
                    {getUserInitials(member)}
                  </AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">
                    {member.firstName || member.lastName
                      ? `${member.firstName ?? ''} ${member.lastName ?? ''}`.trim()
                      : member.userName}
                  </p>
                  <p className="text-xs text-muted-foreground truncate">{member.email}</p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-7 w-7 p-0 text-destructive hover:text-destructive shrink-0"
                  onClick={() => removeMutation.mutate(member.id)}
                  disabled={removeMutation.isPending}
                >
                  <UserMinus className="h-3.5 w-3.5" />
                  <span className="sr-only">Remove</span>
                </Button>
              </div>
            ))
          ) : (
            <p className="py-4 text-center text-sm text-muted-foreground">{t('teamMembers.noMembers')}</p>
          )}
        </div>

        {/* Add Members */}
        <div className="border-t pt-3">
          <Button
            variant="ghost"
            size="sm"
            className="w-full justify-between"
            onClick={() => setShowAddSection((v) => !v)}
          >
            <span className="flex items-center gap-2">
              <UserPlus className="h-4 w-4" />
              Add Members
            </span>
            {showAddSection ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </Button>
          {showAddSection && (
            <div className="mt-2 space-y-2">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Search users..."
                  value={memberSearch}
                  onChange={(e) => setMemberSearch(e.target.value)}
                  className="pl-8 h-8 text-sm"
                />
              </div>
              <div className="max-h-40 overflow-y-auto space-y-1">
                {availableUsers.length === 0 ? (
                  <p className="py-2 text-center text-xs text-muted-foreground">No users available</p>
                ) : (
                  availableUsers.map((u) => (
                    <div key={u.id} className="flex items-center gap-2 rounded-md p-1.5 hover:bg-muted/50">
                      <Avatar className="h-7 w-7">
                        <AvatarFallback className={`text-xs font-semibold text-white ${getAvatarColor(u.id)}`}>
                          {getUserInitials(u)}
                        </AvatarFallback>
                      </Avatar>
                      <div className="flex-1 min-w-0">
                        <p className="text-xs font-medium truncate">
                          {u.firstName || u.lastName
                            ? `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim()
                            : u.userName}
                        </p>
                        <p className="text-xs text-muted-foreground truncate">{u.email}</p>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 w-7 p-0 shrink-0"
                        onClick={() => addMutation.mutate(u.id)}
                        disabled={addMutation.isPending}
                      >
                        <UserPlus className="h-3.5 w-3.5" />
                        <span className="sr-only">Add</span>
                      </Button>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            {t('common.cancel')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Team Card ─────────────────────────────────────────────────────────────────

interface TeamCardProps {
  team: Team;
  onEdit: (team: Team) => void;
  onManageMembers: (team: Team) => void;
  canManage: boolean;
}

function TeamCard({ team, onEdit, onManageMembers, canManage }: TeamCardProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const color = getTeamColor(team.id);

  const { data: members } = useQuery({
    queryKey: ['team-members', team.id],
    queryFn: () => fetchTeamMembers(team.id),
    enabled: canManage,
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteTeam(team.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('common.delete') + ' OK');
    },
    onError: () => toast.error(t('common.error')),
  });

  return (
    <Card className="group relative overflow-hidden transition-all duration-200 hover:shadow-md">
      <div className={`h-1.5 w-full ${color.bg}`} />

      <CardHeader className="pb-2">
        <div className="flex items-start justify-between">
          <div className={`rounded-xl p-3 ${color.light}`}>
            <Users className={`h-6 w-6 ${color.text}`} />
          </div>
          {canManage && (
            <div className="flex gap-1 opacity-0 transition-opacity group-hover:opacity-100">
              <Button
                variant="ghost"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={() => onManageMembers(team)}
                title="Manage Members"
              >
                <UserPlus className="h-3.5 w-3.5" />
                <span className="sr-only">Manage Members</span>
              </Button>
              <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={() => onEdit(team)}>
                <Pencil className="h-3.5 w-3.5" />
                <span className="sr-only">{t('common.edit')}</span>
              </Button>
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-8 w-8 p-0 text-destructive hover:text-destructive">
                    <Trash2 className="h-3.5 w-3.5" />
                    <span className="sr-only">{t('common.delete')}</span>
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>{t('teamAdmin.confirmDelete')}</AlertDialogTitle>
                    <AlertDialogDescription>
                      Delete "{team.name}"? This action cannot be undone.
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                    <AlertDialogAction
                      onClick={() => deleteMutation.mutate()}
                      className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                    >
                      {t('common.delete')}
                    </AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            </div>
          )}
        </div>
      </CardHeader>

      <CardContent>
        <h3 className="text-lg font-semibold">{team.name}</h3>
        <p className="mt-1 text-xs text-muted-foreground">ID: {team.id}</p>
      </CardContent>

      <CardFooter className="border-t pt-3">
        <div className="flex w-full items-center justify-between text-xs text-muted-foreground">
          <button
            className="flex items-center gap-1 hover:text-foreground transition-colors"
            onClick={() => canManage && onManageMembers(team)}
          >
            <Users className="h-3 w-3" />
            {members !== undefined ? `${members.length} ${t('teamAdmin.memberCount')}` : t('teamAdmin.memberCount')}
          </button>
          <span className="flex items-center gap-1">
            <BookOpen className="h-3 w-3" />
            {t('teamAdmin.courseCount')}
          </span>
        </div>
      </CardFooter>
    </Card>
  );
}

// ─── Team Dialog ───────────────────────────────────────────────────────────────

interface TeamDialogProps {
  open: boolean;
  onClose: () => void;
  team?: Team | null;
}

function TeamDialog({ open, onClose, team }: TeamDialogProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const form = useForm<TeamFormValues>({
    resolver: zodResolver(teamSchema),
    defaultValues: { name: team?.name ?? '', description: '' },
    values: { name: team?.name ?? '', description: '' },
  });

  const createMutation = useMutation({
    mutationFn: (data: TeamFormValues) => createTeam({ name: data.name, description: data.description || undefined }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success('Team created successfully');
      onClose();
    },
    onError: () => toast.error(t('common.error')),
  });

  const updateMutation = useMutation({
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    mutationFn: (data: TeamFormValues) => updateTeam(team!.id, { name: data.name, description: data.description || undefined }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success('Team updated successfully');
      onClose();
    },
    onError: () => toast.error(t('common.error')),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  function onSubmit(values: TeamFormValues) {
    if (team) {
      updateMutation.mutate(values);
    } else {
      createMutation.mutate(values);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o: boolean) => !o && onClose()}>
      <DialogContent className="sm:max-w-[420px]">
        <DialogHeader>
          <DialogTitle>{team ? t('teamAdmin.editTeam') : t('teamAdmin.addTeam')}</DialogTitle>
          <DialogDescription>{t('teamAdmin.teamName')}</DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('teamAdmin.teamName')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('teamAdmin.teamName')} {...field} />
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
                  <FormLabel>Description</FormLabel>
                  <FormControl>
                    <Textarea placeholder="Optional description..." rows={2} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
                {t('teamAdmin.cancelEdit')}
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending ? t('common.loading') : t('teamAdmin.saveTeam')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Main Component ────────────────────────────────────────────────────────────

export function TeamAdmin() {
  const { t } = useTranslation();
  const { user } = useAuthStore();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingTeam, setEditingTeam] = useState<Team | null>(null);
  const [membersTeam, setMembersTeam] = useState<Team | null>(null);
  const [membersDialogOpen, setMembersDialogOpen] = useState(false);

  const canManage = user?.isBackOffice ?? false;

  const { data: teams, isLoading } = useQuery({
    queryKey: ['teams'],
    queryFn: fetchUserTeams,
  });

  if (!user?.isBackOffice) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center text-center">
        <Shield className="mb-4 h-16 w-16 text-destructive/50" />
        <h2 className="text-2xl font-bold">{t('teamAdmin.accessDenied')}</h2>
        <p className="mt-2 text-muted-foreground">{t('teamAdmin.accessDeniedDesc')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold">
            <Settings2 className="h-8 w-8 text-primary" />
            {t('teamAdmin.title')}
          </h1>
          <p className="mt-1 text-muted-foreground">{t('teamAdmin.subtitle')}</p>
        </div>
        {canManage && (
          <Button
            onClick={() => {
              setEditingTeam(null);
              setDialogOpen(true);
            }}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            {t('teamAdmin.addTeam')}
          </Button>
        )}
      </div>

      {/* Teams Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i}>
              <div className="h-1.5 w-full bg-muted" />
              <CardHeader className="pb-2">
                <Skeleton className="h-12 w-12 rounded-xl" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-5 w-32" />
                <Skeleton className="mt-1 h-3 w-16" />
              </CardContent>
              <CardFooter className="border-t pt-3">
                <Skeleton className="h-4 w-full" />
              </CardFooter>
            </Card>
          ))}
        </div>
      ) : teams && teams.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {teams.map((team) => (
            <TeamCard
              key={team.id}
              team={team}
              onEdit={(t) => {
                setEditingTeam(t);
                setDialogOpen(true);
              }}
              onManageMembers={(t) => {
                setMembersTeam(t);
                setMembersDialogOpen(true);
              }}
              canManage={canManage}
            />
          ))}
        </div>
      ) : (
        <div className="flex min-h-[30vh] flex-col items-center justify-center rounded-lg border border-dashed text-center">
          <Users className="mb-3 h-12 w-12 text-muted-foreground/40" />
          <p className="text-lg font-medium text-muted-foreground">{t('teamAdmin.noTeams')}</p>
        </div>
      )}

      <TeamDialog
        open={dialogOpen}
        onClose={() => {
          setDialogOpen(false);
          setEditingTeam(null);
        }}
        team={editingTeam}
      />

      {membersTeam && (
        <MembersDialog
          team={membersTeam}
          open={membersDialogOpen}
          onClose={() => {
            setMembersDialogOpen(false);
            setMembersTeam(null);
          }}
        />
      )}
    </div>
  );
}
