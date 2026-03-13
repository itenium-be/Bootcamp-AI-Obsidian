import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import {
  Button,
  Input,
  Label,
  Switch,
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetFooter,
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
} from '@itenium-forge/ui';
import { fetchSkills, createSkill, updateSkill, deleteSkill } from '@/api/client';
import type { Skill, SkillFormData } from '@/api/client';
import { useAuthStore } from '@/stores';

function groupByCategory(skills: Skill[]): Record<string, Skill[]> {
  return skills.reduce<Record<string, Skill[]>>((acc, skill) => {
    const key = skill.category ?? 'Other';
    if (!acc[key]) acc[key] = [];
    acc[key].push(skill);
    return acc;
  }, {});
}

const formSchema = z.object({
  name: z.string().min(1),
  description: z.string().nullable(),
  category: z.string().nullable(),
  levelCount: z.number().int().min(1).max(7),
  isUniversal: z.boolean(),
});

type FormValues = z.infer<typeof formSchema>;

interface SkillFormSheetProps {
  skill: Skill | null;
  open: boolean;
  onClose: () => void;
}

function SkillFormSheet({ skill, open, onClose }: SkillFormSheetProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const isEdit = skill !== null;

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    values: {
      name: skill?.name ?? '',
      description: skill?.description ?? null,
      category: skill?.category ?? null,
      levelCount: skill?.levelCount ?? 1,
      isUniversal: skill?.isUniversal ?? true,
    },
  });

  const mutation = useMutation({
    mutationFn: (data: SkillFormData) => (isEdit && skill ? updateSkill(skill.id, data) : createSkill(data)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['skills'] });
      onClose();
    },
  });

  const onSubmit = (values: FormValues) => {
    mutation.mutate({
      name: values.name,
      description: values.description || null,
      category: values.category || null,
      levelCount: values.levelCount,
      isUniversal: values.isUniversal,
    });
  };

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent>
        <SheetHeader>
          <SheetTitle>{isEdit ? t('skills.editSkill') : t('skills.addSkill')}</SheetTitle>
        </SheetHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4 py-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courses.name')}</FormLabel>
                  <FormControl>
                    <Input {...field} />
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
                  <FormLabel>{t('skills.category')}</FormLabel>
                  <FormControl>
                    <Input
                      {...field}
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value || null)}
                    />
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
                  <FormLabel>{t('courses.description')}</FormLabel>
                  <FormControl>
                    <Input
                      {...field}
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value || null)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="levelCount"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('skills.levelCount')} (1–7)</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min={1}
                      max={7}
                      {...field}
                      onChange={(e) => field.onChange(e.target.valueAsNumber)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="isUniversal"
              render={({ field }) => (
                <FormItem className="flex items-center gap-3">
                  <FormControl>
                    <Switch checked={field.value} onCheckedChange={field.onChange} />
                  </FormControl>
                  <Label>{t('skills.isUniversal')}</Label>
                </FormItem>
              )}
            />

            {mutation.isError && <p className="text-sm text-destructive">{t('common.error')}</p>}

            <SheetFooter>
              <Button type="button" variant="outline" onClick={onClose}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {t('common.save')}
              </Button>
            </SheetFooter>
          </form>
        </Form>
      </SheetContent>
    </Sheet>
  );
}

export function SkillCatalogue() {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const isBackOffice = user?.isBackOffice ?? false;

  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingSkill, setEditingSkill] = useState<Skill | null>(null);
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: deleteSkill,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['skills'] }),
  });

  const handleDelete = (skill: Skill) => {
    if (window.confirm(t('skills.deleteConfirm', { name: skill.name }))) {
      deleteMutation.mutate(skill.id);
    }
  };

  const { data: skills, isLoading } = useQuery({
    queryKey: ['skills'],
    queryFn: fetchSkills,
  });

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  const grouped = skills ? groupByCategory(skills) : {};
  const categories = Object.keys(grouped).sort();

  const openAdd = () => {
    setEditingSkill(null);
    setSheetOpen(true);
  };

  const openEdit = (skill: Skill) => {
    setEditingSkill(skill);
    setSheetOpen(true);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('skills.title')}</h1>
        {isBackOffice && (
          <Button onClick={openAdd}>
            <Plus className="size-4 mr-2" />
            {t('skills.addSkill')}
          </Button>
        )}
      </div>

      {categories.length === 0 && <p className="text-muted-foreground">{t('skills.noSkills')}</p>}

      {categories.map((category) => (
        <div key={category} className="space-y-3">
          <h2 className="text-xl font-semibold">{category}</h2>
          <div className="rounded-md border">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="p-3 text-left font-medium">{t('courses.name')}</th>
                  <th className="p-3 text-left font-medium">{t('courses.description')}</th>
                  <th className="p-3 text-left font-medium">{t('skills.levelCount')}</th>
                  {isBackOffice && <th className="p-3 w-24" />}
                </tr>
              </thead>
              <tbody>
                {grouped[category].map((skill) => (
                  <tr key={skill.id} className="border-b">
                    <td className="p-3 font-medium">{skill.name}</td>
                    <td className="p-3 text-muted-foreground">{skill.description || '-'}</td>
                    <td className="p-3">
                      {skill.levelCount === 1 ? t('skills.checkbox') : `${skill.levelCount} ${t('skills.levels')}`}
                    </td>
                    {isBackOffice && (
                      <td className="p-3">
                        <div className="flex gap-1">
                          <Button variant="ghost" size="sm" onClick={() => openEdit(skill)}>
                            <Pencil className="size-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="text-destructive hover:text-destructive"
                            onClick={() => handleDelete(skill)}
                            disabled={deleteMutation.isPending}
                          >
                            <Trash2 className="size-4" />
                          </Button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ))}

      <SkillFormSheet skill={editingSkill} open={sheetOpen} onClose={() => setSheetOpen(false)} />
    </div>
  );
}
