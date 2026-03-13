import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Loader2, UserPlus } from 'lucide-react';
import { toast } from 'sonner';
import {
  Button,
  Input,
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  CardFooter,
  Select,
  SelectTrigger,
  SelectContent,
  SelectItem,
  SelectValue,
  Checkbox,
} from '@itenium-forge/ui';
import { fetchUserTeams, fetchUsers, createUser, updateUserRole } from '@/api/client';

const ROLES = ['learner', 'manager', 'backoffice'] as const;
type Role = (typeof ROLES)[number];

const createFormSchema = (t: (key: string) => string) =>
  z.object({
    email: z.string().min(1, t('users.emailRequired')).email(t('users.emailInvalid')),
    firstName: z.string().min(1, t('users.firstNameRequired')),
    lastName: z.string().min(1, t('users.lastNameRequired')),
    password: z.string().min(8, t('users.passwordMinLength')),
    role: z.enum(ROLES),
    teamIds: z.array(z.number()),
  });

type FormData = z.infer<ReturnType<typeof createFormSchema>>;

export function Users() {
  const { t } = useTranslation();
  const [showForm, setShowForm] = useState(false);
  const [pendingRoles, setPendingRoles] = useState<Record<string, string>>({});
  const queryClient = useQueryClient();

  // Memoized so zodResolver doesn't get a new schema reference every render
  const formSchema = useMemo(() => createFormSchema(t), [t]);

  const { data: users = [], isLoading: isLoadingUsers } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
  });

  // Deferred until the form is opened
  const { data: teams = [] } = useQuery({
    queryKey: ['teams'],
    queryFn: fetchUserTeams,
    enabled: showForm,
  });

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: '',
      firstName: '',
      lastName: '',
      password: '',
      role: 'learner' as Role,
      teamIds: [],
    },
  });

  const selectedRole = form.watch('role');

  const { mutate, isPending } = useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      toast.success(t('users.createSuccess'));
      form.reset();
      setShowForm(false);
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: () => {
      toast.error(t('users.createError'));
    },
  });

  const { mutate: mutateRole, isPending: isRolePending } = useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: string }) => updateUserRole(userId, role),
    onSuccess: (_data, { userId }) => {
      toast.success(t('users.updateRoleSuccess'));
      setPendingRoles((prev) => Object.fromEntries(Object.entries(prev).filter(([k]) => k !== userId)));
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: () => {
      toast.error(t('users.updateRoleError'));
    },
  });

  const onSubmit = (data: FormData) => {
    mutate({
      ...data,
      teamIds: data.role === 'manager' ? data.teamIds : [],
    });
  };

  const handleCancel = () => {
    form.reset();
    setShowForm(false);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('users.title')}</h1>
        {!showForm && (
          <Button onClick={() => setShowForm(true)}>
            <UserPlus className="size-4 mr-2" />
            {t('users.createUser')}
          </Button>
        )}
      </div>

      {showForm && (
        <Card className="max-w-lg">
          <CardHeader>
            <CardTitle>{t('users.createUser')}</CardTitle>
          </CardHeader>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)}>
              <CardContent className="space-y-4">
                <FormField
                  control={form.control}
                  name="email"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('users.email')}</FormLabel>
                      <FormControl>
                        <Input type="email" placeholder={t('users.emailPlaceholder')} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="firstName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('users.firstName')}</FormLabel>
                        <FormControl>
                          <Input placeholder={t('users.firstNamePlaceholder')} {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="lastName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('users.lastName')}</FormLabel>
                        <FormControl>
                          <Input placeholder={t('users.lastNamePlaceholder')} {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="password"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('auth.password')}</FormLabel>
                      <FormControl>
                        <Input type="password" placeholder={t('auth.enterPassword')} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="role"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('users.role')}</FormLabel>
                      <Select
                        value={field.value}
                        onValueChange={(value) => {
                          field.onChange(value);
                          form.setValue('teamIds', []);
                        }}
                      >
                        <FormControl>
                          <SelectTrigger>
                            <SelectValue placeholder={t('users.rolePlaceholder')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {ROLES.map((role) => (
                            <SelectItem key={role} value={role}>
                              {t(`users.roles.${role}`)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                {selectedRole === 'manager' && teams.length > 0 && (
                  <FormField
                    control={form.control}
                    name="teamIds"
                    render={() => (
                      <FormItem>
                        <FormLabel>{t('users.teams')}</FormLabel>
                        <div className="space-y-2">
                          {teams.map((team) => (
                            <FormField
                              key={team.id}
                              control={form.control}
                              name="teamIds"
                              render={({ field }) => (
                                <FormItem className="flex flex-row items-center space-x-3 space-y-0">
                                  <FormControl>
                                    <Checkbox
                                      checked={field.value?.includes(team.id)}
                                      onCheckedChange={(checked) => {
                                        return checked
                                          ? field.onChange([...field.value, team.id])
                                          : field.onChange(field.value?.filter((v) => v !== team.id));
                                      }}
                                    />
                                  </FormControl>
                                  <FormLabel className="font-normal">{team.name}</FormLabel>
                                </FormItem>
                              )}
                            />
                          ))}
                        </div>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                )}
              </CardContent>

              <CardFooter className="flex gap-2">
                <Button type="submit" disabled={isPending}>
                  {isPending && <Loader2 className="size-4 mr-2 animate-spin" />}
                  {t('common.save')}
                </Button>
                <Button type="button" variant="ghost" onClick={handleCancel}>
                  {t('common.cancel')}
                </Button>
              </CardFooter>
            </form>
          </Form>
        </Card>
      )}

      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('users.tableName')}</th>
              <th className="p-3 text-left font-medium">{t('users.tableEmail')}</th>
              <th className="p-3 text-left font-medium">{t('users.tableRole')}</th>
              <th className="p-3 text-left font-medium">{t('common.edit')}</th>
            </tr>
          </thead>
          <tbody>
            {isLoadingUsers ? (
              <tr>
                <td colSpan={4} className="p-3 text-center text-muted-foreground">
                  {t('common.loading')}
                </td>
              </tr>
            ) : users.length === 0 ? (
              <tr>
                <td colSpan={4} className="p-3 text-center text-muted-foreground">
                  {t('users.noUsers')}
                </td>
              </tr>
            ) : (
              users.map((user) => {
                const currentRole = user.roles[0] ?? '';
                const pendingRole = pendingRoles[user.id];
                const displayRole = pendingRole ?? currentRole;
                const isDirty = pendingRole !== undefined && pendingRole !== currentRole;
                return (
                  <tr key={user.id} className="border-b">
                    <td className="p-3">
                      {user.firstName} {user.lastName}
                    </td>
                    <td className="p-3 text-muted-foreground">{user.email}</td>
                    <td className="p-3">
                      <Select
                        value={displayRole}
                        onValueChange={(value) => setPendingRoles((prev) => ({ ...prev, [user.id]: value }))}
                      >
                        <SelectTrigger className="w-36">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {ROLES.map((role) => (
                            <SelectItem key={role} value={role}>
                              {t(`users.roles.${role}`)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </td>
                    <td className="p-3">
                      {isDirty && (
                        <Button
                          size="sm"
                          disabled={isRolePending}
                          onClick={() => mutateRole({ userId: user.id, role: pendingRole })}
                        >
                          {isRolePending && <Loader2 className="size-4 mr-2 animate-spin" />}
                          {t('common.save')}
                        </Button>
                      )}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
