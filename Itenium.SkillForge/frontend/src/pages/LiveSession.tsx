import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CheckCircle, Plus, X, Save } from 'lucide-react';
import {
  Button,
  Textarea,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Input,
} from '@itenium-forge/ui';
import {
  startSession,
  getSessionFocus,
  endSession,
  updateSessionNotes,
  createValidation,
  createGoal,
  fetchSkills,
  type Goal,
  type ReadinessFlag,
  type Skill,
} from '@/api/client';
import { useAuthStore } from '@/stores';

interface LiveSessionProps {
  consultantId: string;
}

export function LiveSession({ consultantId }: LiveSessionProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();

  const [sessionId, setSessionId] = useState<string | null>(null);
  const [notes, setNotes] = useState('');
  const [notesTimer, setNotesTimer] = useState<ReturnType<typeof setTimeout> | null>(null);

  // Validate skill modal
  const [validatingGoal, setValidatingGoal] = useState<Goal | null>(null);
  const [targetNiveau, setTargetNiveau] = useState<number>(1);

  // Add goal modal
  const [showAddGoal, setShowAddGoal] = useState(false);
  const [goalSkillId, setGoalSkillId] = useState('');
  const [goalCurrentNiveau, setGoalCurrentNiveau] = useState('1');
  const [goalTargetNiveau, setGoalTargetNiveau] = useState('2');
  const [goalDeadline, setGoalDeadline] = useState('');

  const { data: skills } = useQuery({ queryKey: ['skills'], queryFn: fetchSkills });

  // Start session on mount
  const startMutation = useMutation({
    mutationFn: () => startSession(consultantId),
    onSuccess: (session) => {
      setSessionId(session.id);
      toast.success(t('session.started'));
    },
    onError: () => toast.error(t('common.error')),
  });

  useEffect(() => {
    startMutation.mutate();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [consultantId]);

  const { data: focus, refetch: refetchFocus } = useQuery({
    queryKey: ['session-focus', sessionId],
    queryFn: () => getSessionFocus(sessionId!),
    enabled: !!sessionId,
    refetchInterval: 10000,
  });

  const endMutation = useMutation({
    mutationFn: () => endSession(sessionId!),
    onSuccess: () => {
      toast.success(t('session.ended'));
      queryClient.invalidateQueries({ queryKey: ['session-focus'] });
      void navigate({ to: '/' });
    },
    onError: () => toast.error(t('common.error')),
  });

  const notesMutation = useMutation({
    mutationFn: (text: string) => updateSessionNotes(sessionId!, text),
  });

  const handleNotesChange = (value: string) => {
    setNotes(value);
    if (notesTimer) clearTimeout(notesTimer);
    const timer = setTimeout(() => {
      if (sessionId) notesMutation.mutate(value);
    }, 1000);
    setNotesTimer(timer);
  };

  const validationMutation = useMutation({
    mutationFn: ({ skillId, fromNiveau, toNiv }: { skillId: number; fromNiveau: number; toNiv: number }) =>
      createValidation({
        skillId,
        consultantId,
        fromNiveau,
        toNiveau: toNiv,
        sessionId,
        notes: null,
      }),
    onSuccess: () => {
      toast.success(t('session.validationSaved'));
      setValidatingGoal(null);
      void refetchFocus();
    },
    onError: () => toast.error(t('common.error')),
  });

  const addGoalMutation = useMutation({
    mutationFn: () =>
      createGoal({
        consultantId,
        coachId: user!.id,
        skillId: parseInt(goalSkillId, 10),
        currentNiveau: parseInt(goalCurrentNiveau, 10),
        targetNiveau: parseInt(goalTargetNiveau, 10),
        deadline: goalDeadline,
        linkedResourceIds: null,
      }),
    onSuccess: () => {
      toast.success(t('session.goalCreated'));
      setShowAddGoal(false);
      void refetchFocus();
    },
    onError: () => toast.error(t('common.error')),
  });

  const handleValidate = () => {
    if (!validatingGoal) return;
    validationMutation.mutate({
      skillId: validatingGoal.skillId,
      fromNiveau: validatingGoal.currentNiveau,
      toNiv: targetNiveau,
    });
  };

  const handleEndSession = useCallback(() => {
    if (notesTimer) {
      clearTimeout(notesTimer);
      if (sessionId && notes) notesMutation.mutate(notes);
    }
    endMutation.mutate();
  }, [notesTimer, sessionId, notes, notesMutation, endMutation]);

  if (startMutation.isPending || !sessionId) {
    return (
      <div className="flex items-center justify-center h-screen">
        <p className="text-muted-foreground">{t('session.starting')}</p>
      </div>
    );
  }

  return (
    // Focused UI: no sidebar/nav, full screen
    <div className="min-h-screen bg-background p-6 max-w-4xl mx-auto space-y-6">
      {/* Session Header */}
      <div className="flex items-center justify-between border-b pb-4">
        <div>
          <h1 className="text-2xl font-bold">{t('session.title')}</h1>
          <p className="text-muted-foreground text-sm">{t('session.consultant')}: {consultantId}</p>
        </div>
        <Button variant="destructive" onClick={handleEndSession} disabled={endMutation.isPending}>
          <X className="size-4 mr-2" />
          {t('session.endSession')}
        </Button>
      </div>

      {/* Pending Readiness Flags */}
      {focus && focus.pendingReadinessFlags.length > 0 && (
        <section className="space-y-2">
          <h2 className="text-lg font-semibold">{t('session.pendingValidations')} ({focus.pendingReadinessFlags.length})</h2>
          {focus.pendingReadinessFlags.map((flag: ReadinessFlag) => (
            <div key={flag.id} className="rounded-md border bg-yellow-50 dark:bg-yellow-950 p-3 text-sm">
              {t('session.flagRaised', { date: new Date(flag.raisedAt).toLocaleDateString() })}
            </div>
          ))}
        </section>
      )}

      {/* Active Goals / Skill Cards */}
      <section className="space-y-2">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">{t('session.activeGoals')}</h2>
          <Button size="sm" variant="outline" onClick={() => setShowAddGoal(true)}>
            <Plus className="size-4 mr-1" />
            {t('session.addGoal')}
          </Button>
        </div>

        {(!focus || focus.activeGoals.length === 0) && (
          <p className="text-muted-foreground text-sm">{t('session.noActiveGoals')}</p>
        )}

        <div className="grid gap-3 sm:grid-cols-2">
          {focus?.activeGoals.map((goal: Goal) => {
            const skill = skills?.find((s: Skill) => s.id === goal.skillId);
            return (
              <div
                key={goal.id}
                className="rounded-lg border bg-card p-4 cursor-pointer hover:border-primary transition-colors"
                onClick={() => {
                  setValidatingGoal(goal);
                  setTargetNiveau(goal.currentNiveau + 1);
                }}
              >
                <div className="font-medium">{skill?.name ?? `Skill #${goal.skillId}`}</div>
                <div className="text-sm text-muted-foreground">
                  {t('session.niveau')}: {goal.currentNiveau} → {goal.targetNiveau}
                </div>
                <div className="text-xs text-muted-foreground mt-1">
                  {t('session.deadline')}: {new Date(goal.deadline).toLocaleDateString()}
                </div>
                <div className="mt-2">
                  <div className="h-2 bg-muted rounded-full overflow-hidden">
                    <div
                      className="h-full bg-primary rounded-full"
                      style={{
                        width: `${Math.min(100, ((goal.currentNiveau - 1) / Math.max(1, goal.targetNiveau - 1)) * 100)}%`,
                      }}
                    />
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {/* Inline Notes */}
      <section className="space-y-2">
        <div className="flex items-center gap-2">
          <h2 className="text-lg font-semibold">{t('session.notes')}</h2>
          {notesMutation.isPending && <Save className="size-4 animate-spin text-muted-foreground" />}
        </div>
        <Textarea
          value={notes}
          onChange={(e) => handleNotesChange(e.target.value)}
          placeholder={t('session.notesPlaceholder')}
          rows={4}
          className="resize-none"
        />
      </section>

      {/* Validate Skill Modal */}
      <Dialog open={!!validatingGoal} onOpenChange={() => setValidatingGoal(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('session.validateSkill')}</DialogTitle>
          </DialogHeader>
          {validatingGoal && (
            <div className="space-y-4 py-4">
              <div>
                <p className="text-sm">{t('session.currentNiveau')}: <strong>{validatingGoal.currentNiveau}</strong></p>
              </div>
              <div className="space-y-2">
                <Label>{t('session.targetNiveau')}</Label>
                <div className="flex gap-2">
                  {Array.from({ length: validatingGoal.targetNiveau }, (_, i) => i + 1).map((n) => (
                    <button
                      key={n}
                      onClick={() => setTargetNiveau(n)}
                      className={`size-10 rounded-md border font-medium text-sm transition-colors ${
                        targetNiveau === n
                          ? 'bg-primary text-primary-foreground border-primary'
                          : 'bg-card hover:border-primary'
                      }`}
                    >
                      {n}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setValidatingGoal(null)}>
              {t('common.cancel')}
            </Button>
            <Button onClick={handleValidate} disabled={validationMutation.isPending}>
              <CheckCircle className="size-4 mr-2" />
              {t('session.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Goal Modal */}
      <Dialog open={showAddGoal} onOpenChange={setShowAddGoal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('session.addGoal')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-1">
              <Label>{t('resources.skill')}</Label>
              <Select value={goalSkillId} onValueChange={setGoalSkillId}>
                <SelectTrigger>
                  <SelectValue placeholder={t('resources.selectSkill')} />
                </SelectTrigger>
                <SelectContent>
                  {skills?.map((skill: Skill) => (
                    <SelectItem key={skill.id} value={String(skill.id)}>
                      {skill.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex gap-4">
              <div className="space-y-1 flex-1">
                <Label>{t('session.currentNiveau')}</Label>
                <Input
                  type="number"
                  min={1}
                  value={goalCurrentNiveau}
                  onChange={(e) => setGoalCurrentNiveau(e.target.value)}
                />
              </div>
              <div className="space-y-1 flex-1">
                <Label>{t('session.targetNiveau')}</Label>
                <Input
                  type="number"
                  min={1}
                  value={goalTargetNiveau}
                  onChange={(e) => setGoalTargetNiveau(e.target.value)}
                />
              </div>
            </div>
            <div className="space-y-1">
              <Label>{t('session.deadline')}</Label>
              <Input type="date" value={goalDeadline} onChange={(e) => setGoalDeadline(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddGoal(false)}>
              {t('common.cancel')}
            </Button>
            <Button onClick={() => addGoalMutation.mutate()} disabled={!goalSkillId || addGoalMutation.isPending}>
              {t('common.save')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
