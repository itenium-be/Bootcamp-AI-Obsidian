import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ThumbsUp, ThumbsDown, CheckCircle, Plus, ExternalLink } from 'lucide-react';
import {
  Button,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Label,
} from '@itenium-forge/ui';
import {
  fetchResources,
  fetchSkills,
  createResource,
  rateResource,
  completeResource,
  fetchMyGoals,
  ResourceTypeValues,
  type Resource,
  type Goal,
} from '@/api/client';
import { useAuthStore } from '@/stores';

export function Resources() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();

  // Filters
  const [filterSkillId, setFilterSkillId] = useState<string>('');
  const [filterFromNiveau, setFilterFromNiveau] = useState<string>('');
  const [filterToNiveau, setFilterToNiveau] = useState<string>('');

  // Add resource modal
  const [showAddModal, setShowAddModal] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newUrl, setNewUrl] = useState('');
  const [newType, setNewType] = useState<string>('1');
  const [newSkillId, setNewSkillId] = useState<string>('');
  const [newFromNiveau, setNewFromNiveau] = useState<string>('1');
  const [newToNiveau, setNewToNiveau] = useState<string>('1');

  // Complete resource modal
  const [completingResource, setCompletingResource] = useState<Resource | null>(null);
  const [selectedGoalId, setSelectedGoalId] = useState<string>('');

  const { data: skills } = useQuery({
    queryKey: ['skills'],
    queryFn: fetchSkills,
  });

  const { data: resources, isLoading } = useQuery({
    queryKey: ['resources', filterSkillId, filterFromNiveau, filterToNiveau],
    queryFn: () =>
      fetchResources({
        skillId: filterSkillId ? parseInt(filterSkillId, 10) : undefined,
        fromNiveau: filterFromNiveau ? parseInt(filterFromNiveau, 10) : undefined,
        toNiveau: filterToNiveau ? parseInt(filterToNiveau, 10) : undefined,
      }),
  });

  const { data: myGoals } = useQuery({
    queryKey: ['my-goals', user?.id],
    queryFn: () => fetchMyGoals(user!.id),
    enabled: !!user?.id && !!completingResource,
  });

  const createMutation = useMutation({
    mutationFn: createResource,
    onSuccess: () => {
      toast.success(t('resources.addSuccess'));
      queryClient.invalidateQueries({ queryKey: ['resources'] });
      setShowAddModal(false);
      setNewTitle('');
      setNewUrl('');
      setNewType('1');
      setNewSkillId('');
      setNewFromNiveau('1');
      setNewToNiveau('1');
    },
    onError: () => toast.error(t('common.error')),
  });

  const rateMutation = useMutation({
    mutationFn: ({ resourceId, rating }: { resourceId: string; rating: 'up' | 'down' }) =>
      rateResource(resourceId, rating),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['resources'] });
    },
    onError: () => toast.error(t('common.error')),
  });

  const completeMutation = useMutation({
    mutationFn: ({ resourceId, goalId }: { resourceId: string; goalId: string }) =>
      completeResource(resourceId, goalId),
    onSuccess: () => {
      toast.success(t('resources.completeSuccess'));
      setCompletingResource(null);
      setSelectedGoalId('');
    },
    onError: () => toast.error(t('common.error')),
  });

  const handleAddResource = () => {
    if (!newTitle || !newUrl || !newSkillId) {
      toast.error(t('resources.fillRequired'));
      return;
    }
    createMutation.mutate({
      title: newTitle,
      url: newUrl,
      type: parseInt(newType, 10),
      skillId: parseInt(newSkillId, 10),
      fromNiveau: parseInt(newFromNiveau, 10),
      toNiveau: parseInt(newToNiveau, 10),
    });
  };

  const handleComplete = () => {
    if (!completingResource || !selectedGoalId) return;
    completeMutation.mutate({ resourceId: completingResource.id, goalId: selectedGoalId });
  };

  const isLearner = !user?.isBackOffice;

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('resources.title')}</h1>
          <p className="text-muted-foreground">{t('resources.subtitle')}</p>
        </div>
        <Button onClick={() => setShowAddModal(true)}>
          <Plus className="size-4 mr-2" />
          {t('resources.addResource')}
        </Button>
      </div>

      {/* Filters */}
      <div className="flex gap-4 flex-wrap">
        <div className="flex flex-col gap-1">
          <Label>{t('resources.filterBySkill')}</Label>
          <Select value={filterSkillId} onValueChange={setFilterSkillId}>
            <SelectTrigger className="w-48">
              <SelectValue placeholder={t('resources.allSkills')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">{t('resources.allSkills')}</SelectItem>
              {skills?.map((skill) => (
                <SelectItem key={skill.id} value={String(skill.id)}>
                  {skill.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1">
          <Label>{t('resources.fromNiveau')}</Label>
          <Input
            type="number"
            min={1}
            max={7}
            value={filterFromNiveau}
            onChange={(e) => setFilterFromNiveau(e.target.value)}
            placeholder="1"
            className="w-24"
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label>{t('resources.toNiveau')}</Label>
          <Input
            type="number"
            min={1}
            max={7}
            value={filterToNiveau}
            onChange={(e) => setFilterToNiveau(e.target.value)}
            placeholder="7"
            className="w-24"
          />
        </div>
      </div>

      {/* Resource Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {resources?.map((resource) => (
          <ResourceCard
            key={resource.id}
            resource={resource}
            isLearner={isLearner}
            onRate={(rating) => rateMutation.mutate({ resourceId: resource.id, rating })}
            onComplete={() => {
              setCompletingResource(resource);
              setSelectedGoalId('');
            }}
          />
        ))}
        {resources?.length === 0 && (
          <div className="col-span-3 py-12 text-center text-muted-foreground">{t('resources.noResources')}</div>
        )}
      </div>

      {/* Add Resource Modal */}
      <Dialog open={showAddModal} onOpenChange={setShowAddModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('resources.addResource')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-1">
              <Label>{t('resources.title_field')}</Label>
              <Input value={newTitle} onChange={(e) => setNewTitle(e.target.value)} placeholder={t('resources.titlePlaceholder')} />
            </div>
            <div className="space-y-1">
              <Label>{t('resources.url')}</Label>
              <Input value={newUrl} onChange={(e) => setNewUrl(e.target.value)} placeholder="https://..." />
            </div>
            <div className="space-y-1">
              <Label>{t('resources.type')}</Label>
              <Select value={newType} onValueChange={setNewType}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(ResourceTypeValues).map(([label, value]) => (
                    <SelectItem key={value} value={String(value)}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>{t('resources.skill')}</Label>
              <Select value={newSkillId} onValueChange={setNewSkillId}>
                <SelectTrigger>
                  <SelectValue placeholder={t('resources.selectSkill')} />
                </SelectTrigger>
                <SelectContent>
                  {skills?.map((skill) => (
                    <SelectItem key={skill.id} value={String(skill.id)}>
                      {skill.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex gap-4">
              <div className="space-y-1 flex-1">
                <Label>{t('resources.fromNiveau')}</Label>
                <Input
                  type="number"
                  min={1}
                  max={7}
                  value={newFromNiveau}
                  onChange={(e) => setNewFromNiveau(e.target.value)}
                />
              </div>
              <div className="space-y-1 flex-1">
                <Label>{t('resources.toNiveau')}</Label>
                <Input
                  type="number"
                  min={1}
                  max={7}
                  value={newToNiveau}
                  onChange={(e) => setNewToNiveau(e.target.value)}
                />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddModal(false)}>
              {t('common.cancel')}
            </Button>
            <Button onClick={handleAddResource} disabled={createMutation.isPending}>
              {t('common.save')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Complete Resource Modal */}
      <Dialog open={!!completingResource} onOpenChange={() => setCompletingResource(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('resources.markComplete')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <p className="text-sm text-muted-foreground">{t('resources.selectGoal')}</p>
            <Select value={selectedGoalId} onValueChange={setSelectedGoalId}>
              <SelectTrigger>
                <SelectValue placeholder={t('resources.chooseGoal')} />
              </SelectTrigger>
              <SelectContent>
                {myGoals
                  ?.filter((g: Goal) => g.status === 'Active')
                  .map((goal: Goal) => (
                    <SelectItem key={goal.id} value={goal.id}>
                      {t('resources.goalLabel', { skillId: goal.skillId, niveau: `${goal.currentNiveau}→${goal.targetNiveau}` })}
                    </SelectItem>
                  ))}
              </SelectContent>
            </Select>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCompletingResource(null)}>
              {t('common.cancel')}
            </Button>
            <Button onClick={handleComplete} disabled={!selectedGoalId || completeMutation.isPending}>
              {t('resources.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface ResourceCardProps {
  resource: Resource;
  isLearner: boolean;
  onRate: (rating: 'up' | 'down') => void;
  onComplete: () => void;
}

function ResourceCard({ resource, isLearner, onRate, onComplete }: ResourceCardProps) {
  const { t } = useTranslation();

  return (
    <div className="rounded-lg border bg-card p-4 space-y-3">
      <div className="flex items-start justify-between gap-2">
        <div>
          <h3 className="font-semibold">{resource.title}</h3>
          <span className="text-xs text-muted-foreground bg-muted px-2 py-0.5 rounded-full">{resource.type}</span>
        </div>
        <a href={resource.url} target="_blank" rel="noopener noreferrer" className="text-muted-foreground hover:text-primary">
          <ExternalLink className="size-4" />
        </a>
      </div>

      <div className="text-xs text-muted-foreground">
        {t('resources.niveauRange', { from: resource.fromNiveau, to: resource.toNiveau })}
      </div>

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <button
            onClick={() => onRate('up')}
            className="flex items-center gap-1 text-sm hover:text-green-600 transition-colors"
          >
            <ThumbsUp className="size-4" />
            <span>{resource.thumbsUp}</span>
          </button>
          <button
            onClick={() => onRate('down')}
            className="flex items-center gap-1 text-sm hover:text-red-600 transition-colors"
          >
            <ThumbsDown className="size-4" />
            <span>{resource.thumbsDown}</span>
          </button>
        </div>
        {isLearner && (
          <Button size="sm" variant="outline" onClick={onComplete}>
            <CheckCircle className="size-4 mr-1" />
            {t('resources.markComplete')}
          </Button>
        )}
      </div>
    </div>
  );
}
