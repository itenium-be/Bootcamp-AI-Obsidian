import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { UserPlus, UserMinus } from 'lucide-react';
import { toast } from 'sonner';
import { Button, Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from '@itenium-forge/ui';
import { fetchUserTeams, fetchTeamMembers, fetchUsers, addTeamMember, removeTeamMember } from '@/api/client';

export function Teams() {
  const { t } = useTranslation();
  const [selectedTeamId, setSelectedTeamId] = useState<number | null>(null);
  const [addingUserId, setAddingUserId] = useState('');
  const queryClient = useQueryClient();

  const { data: teams = [] } = useQuery({
    queryKey: ['teams'],
    queryFn: fetchUserTeams,
  });

  const activeTeamId = selectedTeamId ?? (teams.length > 0 ? teams[0].id : null);

  const { data: members = [], isLoading: isLoadingMembers } = useQuery({
    queryKey: ['team-members', activeTeamId],
    queryFn: () => fetchTeamMembers(activeTeamId as number),
    enabled: activeTeamId !== null,
  });

  const { data: allUsers = [] } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
  });

  const nonMembers = allUsers.filter((u) => !members.some((m) => m.id === u.id));

  const addMutation = useMutation({
    mutationFn: ({ teamId, userId }: { teamId: number; userId: string }) => addTeamMember(teamId, userId),
    onSuccess: () => {
      toast.success(t('teams.addMemberSuccess'));
      setAddingUserId('');
      queryClient.invalidateQueries({ queryKey: ['team-members', activeTeamId] });
    },
    onError: () => toast.error(t('teams.addMemberError')),
  });

  const removeMutation = useMutation({
    mutationFn: ({ teamId, userId }: { teamId: number; userId: string }) => removeTeamMember(teamId, userId),
    onSuccess: () => {
      toast.success(t('teams.removeMemberSuccess'));
      queryClient.invalidateQueries({ queryKey: ['team-members', activeTeamId] });
    },
    onError: () => toast.error(t('teams.removeMemberError')),
  });

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">{t('teams.title')}</h1>

      <div className="flex gap-1 border-b">
        {teams.map((team) => (
          <button
            key={team.id}
            className={`px-4 py-2 -mb-px border-b-2 font-medium text-sm transition-colors ${
              activeTeamId === team.id
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
            onClick={() => {
              setSelectedTeamId(team.id);
              setAddingUserId('');
            }}
          >
            {team.name}
          </button>
        ))}
      </div>

      {activeTeamId !== null && (
        <div className="space-y-4">
          <div className="flex gap-2 items-center">
            <Select value={addingUserId} onValueChange={setAddingUserId}>
              <SelectTrigger className="w-72">
                <SelectValue placeholder={t('teams.selectMember')} />
              </SelectTrigger>
              <SelectContent>
                {nonMembers.map((user) => (
                  <SelectItem key={user.id} value={user.id}>
                    {user.firstName} {user.lastName} ({user.email})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button
              disabled={!addingUserId || addMutation.isPending}
              onClick={() => addMutation.mutate({ teamId: activeTeamId, userId: addingUserId })}
            >
              <UserPlus className="size-4 mr-2" />
              {t('teams.addMember')}
            </Button>
          </div>

          <div className="rounded-md border">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="p-3 text-left font-medium">{t('users.tableName')}</th>
                  <th className="p-3 text-left font-medium">{t('users.tableEmail')}</th>
                  <th className="p-3 text-left font-medium"></th>
                </tr>
              </thead>
              <tbody>
                {isLoadingMembers ? (
                  <tr>
                    <td colSpan={3} className="p-3 text-center text-muted-foreground">
                      {t('common.loading')}
                    </td>
                  </tr>
                ) : members.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="p-3 text-center text-muted-foreground">
                      {t('teams.noMembers')}
                    </td>
                  </tr>
                ) : (
                  members.map((member) => (
                    <tr key={member.id} className="border-b">
                      <td className="p-3">
                        {member.firstName} {member.lastName}
                      </td>
                      <td className="p-3 text-muted-foreground">{member.email}</td>
                      <td className="p-3 text-right">
                        <Button
                          variant="ghost"
                          size="sm"
                          disabled={removeMutation.isPending}
                          onClick={() => removeMutation.mutate({ teamId: activeTeamId, userId: member.id })}
                        >
                          <UserMinus className="size-4 mr-2" />
                          {t('teams.removeMember')}
                        </Button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
