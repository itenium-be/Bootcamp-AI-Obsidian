import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import {
  Users,
  Shield,
  Plus,
  Pencil,
  Trash2,
  AlertTriangle,
  Settings2,
  BookOpen,
} from 'lucide-react';
import { fetchUserTeams, type Course } from '@/api/client';
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
  Button,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Separator,
  Skeleton,
} from '@itenium-forge/ui';

interface Team {
  id: number;
  name: string;
}

const teamSchema = z.object({
  name: z.string().min(1, 'teamAdmin.teamNameRequired'),
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

interface TeamCardProps {
  team: Team;
  onEdit: (team: Team) => void;
  canManage: boolean;
}

function TeamCard({ team, onEdit, canManage }: TeamCardProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const color = getTeamColor(team.id);

  const deleteMutation = useMutation({
    mutationFn: async () => {
      // DELETE /api/team/{id} not yet implemented
      throw new Error('Not implemented');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('common.delete') + ' OK');
    },
    onError: () => {
      toast.error(t('teamAdmin.notImplemented'));
    },
  });

  return (
    <Card className="group relative overflow-hidden transition-all duration-200 hover:shadow-md">
      {/* Colored top bar */}
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
                onClick={() => onEdit(team)}
              >
                <Pencil className="h-3.5 w-3.5" />
                <span className="sr-only">{t('common.edit')}</span>
              </Button>
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0 text-destructive hover:text-destructive"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                    <span className="sr-only">{t('common.delete')}</span>
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>{t('teamAdmin.confirmDelete')}</AlertDialogTitle>
                    <AlertDialogDescription>
                      {t('teamAdmin.notImplemented')}
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
          <span className="flex items-center gap-1">
            <Users className="h-3 w-3" />
            {t('teamAdmin.memberCount')}
          </span>
          <span className="flex items-center gap-1">
            <BookOpen className="h-3 w-3" />
            {t('teamAdmin.courseCount')}
          </span>
        </div>
      </CardFooter>
    </Card>
  );
}

interface TeamDialogProps {
  open: boolean;
  onClose: () => void;
  team?: Team | null;
}

function TeamDialog({ open, onClose, team }: TeamDialogProps) {
  const { t } = useTranslation();

  const form = useForm<TeamFormValues>({
    resolver: zodResolver(teamSchema),
    defaultValues: { name: team?.name ?? '' },
    values: { name: team?.name ?? '' },
  });

  function onSubmit(_values: TeamFormValues) {
    // POST /api/team not yet implemented
    toast.error(t('teamAdmin.notImplemented'));
    onClose();
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-[420px]">
        <DialogHeader>
          <DialogTitle>
            {team ? t('teamAdmin.editTeam') : t('teamAdmin.addTeam')}
          </DialogTitle>
          <DialogDescription>{t('teamAdmin.teamName')}</DialogDescription>
        </DialogHeader>

        {/* Not implemented warning */}
        <div className="flex items-start gap-3 rounded-lg border border-orange-200 bg-orange-50 p-3 text-sm dark:border-orange-800 dark:bg-orange-900/20">
          <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-orange-500" />
          <div>
            <p className="font-medium text-orange-800 dark:text-orange-400">
              {t('teamAdmin.notImplemented')}
            </p>
            <p className="mt-0.5 text-orange-700 dark:text-orange-500">
              {t('teamAdmin.notImplementedNote')}
            </p>
          </div>
        </div>

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
            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose}>
                {t('teamAdmin.cancelEdit')}
              </Button>
              <Button type="submit" disabled title={t('teamAdmin.notImplemented')}>
                {t('teamAdmin.saveTeam')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}

export function TeamAdmin() {
  const { t } = useTranslation();
  const { user } = useAuthStore();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingTeam, setEditingTeam] = useState<Team | null>(null);

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

      {/* Not implemented notice */}
      <div className="flex items-start gap-3 rounded-lg border border-orange-200 bg-orange-50 p-4 dark:border-orange-800 dark:bg-orange-900/20">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-orange-500" />
        <div>
          <p className="font-semibold text-orange-800 dark:text-orange-400">
            {t('teamAdmin.notImplemented')}
          </p>
          <p className="mt-1 text-sm text-orange-700 dark:text-orange-500">
            {t('teamAdmin.notImplementedNote')}
          </p>
        </div>
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
    </div>
  );
}
