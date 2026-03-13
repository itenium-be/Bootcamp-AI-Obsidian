import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { fetchSkills, fetchSkill, type Skill } from '@/api/client';

const PROFILE_OPTIONS = [
  { value: undefined, label: 'All Profiles' },
  { value: 1, label: 'Java' },
  { value: 2, label: '.NET' },
  { value: 3, label: 'PO & Analysis' },
  { value: 4, label: 'QA' },
];

const PROFILE_NAMES: Record<number, string> = {
  1: 'Java',
  2: '.NET',
  3: 'PO & Analysis',
  4: 'QA',
};

function SkillDetailPanel({ skillId, onClose }: { skillId: number; onClose: () => void }) {
  const { data: skill, isLoading } = useQuery({
    queryKey: ['skill', skillId],
    queryFn: () => fetchSkill(skillId),
    staleTime: 30_000,
  });

  if (isLoading) {
    return <div className="p-4 text-muted-foreground text-sm">Loading...</div>;
  }

  if (!skill) {
    return null;
  }

  return (
    <div className="rounded-lg border bg-card p-6 space-y-4">
      <div className="flex items-start justify-between">
        <div>
          <h2 className="text-xl font-bold">{skill.name}</h2>
          <p className="text-sm text-muted-foreground">{skill.category}</p>
        </div>
        <button type="button" onClick={onClose} className="text-muted-foreground hover:text-foreground text-sm">
          Close
        </button>
      </div>

      {skill.description && <p className="text-sm">{skill.description}</p>}

      <div>
        <p className="text-sm font-medium">Level count: {skill.levelCount}</p>
        {skill.levelDescriptors.length > 0 && (
          <ol className="mt-2 space-y-1 list-decimal list-inside">
            {skill.levelDescriptors.map((desc, i) => (
              <li key={i} className="text-sm text-muted-foreground">
                <span className="font-medium text-foreground">Niveau {i + 1}:</span> {desc}
              </li>
            ))}
          </ol>
        )}
      </div>

      {skill.profiles.length > 0 && (
        <div>
          <p className="text-sm font-medium mb-1">Profiles</p>
          <div className="flex flex-wrap gap-1">
            {skill.profiles.map((p) => (
              <span key={p} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">
                {PROFILE_NAMES[p] ?? p}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* FR8: Non-blocking prerequisite warnings (Story #17) */}
      {skill.prerequisiteWarnings.length > 0 && (
        <div className="space-y-2">
          <p className="text-sm font-medium text-amber-700">Prerequisite Warnings</p>
          {skill.prerequisiteWarnings.map((w) => (
            <div
              key={w.skillId}
              className="text-sm bg-amber-50 border border-amber-200 rounded px-3 py-2 text-amber-800"
            >
              {w.warningText}
            </div>
          ))}
          <p className="text-xs text-muted-foreground">
            This skill is not locked — you can explore and study it freely.
          </p>
        </div>
      )}
    </div>
  );
}

export function Skills() {
  const { t } = useTranslation();
  const [selectedProfile, setSelectedProfile] = useState<number | undefined>(undefined);
  const [selectedSkillId, setSelectedSkillId] = useState<number | null>(null);
  const [search, setSearch] = useState('');

  const { data: skills, isLoading } = useQuery({
    queryKey: ['skills', selectedProfile],
    queryFn: () => fetchSkills(selectedProfile),
    staleTime: 30_000,
  });

  const filtered = skills?.filter(
    (s) =>
      s.name.toLowerCase().includes(search.toLowerCase()) ||
      s.category.toLowerCase().includes(search.toLowerCase()),
  );

  // Group by category
  const byCategory = filtered?.reduce<Record<string, Skill[]>>((acc, skill) => {
    const cat = skill.category;
    if (!acc[cat]) acc[cat] = [];
    acc[cat].push(skill);
    return acc;
  }, {});

  if (isLoading) {
    return <div className="p-6">{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">Skill Catalogue</h1>
        <span className="text-sm text-muted-foreground">{skills?.length ?? 0} skills</span>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3">
        <input
          type="search"
          placeholder={t('common.search')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="border rounded px-3 py-1.5 text-sm bg-background"
        />
        <select
          value={selectedProfile ?? ''}
          onChange={(e) =>
            setSelectedProfile(e.target.value === '' ? undefined : Number(e.target.value))
          }
          className="border rounded px-3 py-1.5 text-sm bg-background"
        >
          {PROFILE_OPTIONS.map((opt) => (
            <option key={opt.value ?? 'all'} value={opt.value ?? ''}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      {/* Skill detail panel */}
      {selectedSkillId && (
        <SkillDetailPanel skillId={selectedSkillId} onClose={() => setSelectedSkillId(null)} />
      )}

      {/* Skills grouped by category */}
      {byCategory &&
        Object.entries(byCategory).map(([category, catSkills]) => (
          <div key={category} className="space-y-3">
            <h2 className="text-lg font-semibold">{category}</h2>
            <div className="rounded-md border">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="p-3 text-left text-sm font-medium">Name</th>
                    <th className="p-3 text-left text-sm font-medium">Description</th>
                    <th className="p-3 text-left text-sm font-medium">Levels</th>
                    <th className="p-3 text-left text-sm font-medium">Profiles</th>
                  </tr>
                </thead>
                <tbody>
                  {catSkills.map((skill) => (
                    <tr
                      key={skill.id}
                      className="border-b hover:bg-muted/30 cursor-pointer"
                      onClick={() => setSelectedSkillId(selectedSkillId === skill.id ? null : skill.id)}
                    >
                      <td className="p-3 font-medium text-sm">{skill.name}</td>
                      <td className="p-3 text-sm text-muted-foreground">
                        {skill.description ? skill.description.substring(0, 80) + (skill.description.length > 80 ? '…' : '') : '-'}
                      </td>
                      <td className="p-3 text-sm">{skill.levelCount}</td>
                      <td className="p-3">
                        <div className="flex flex-wrap gap-1">
                          {Array.isArray(skill.profiles) && skill.profiles.map((p: number) => (
                            <span
                              key={p}
                              className="text-xs bg-primary/10 text-primary px-1.5 py-0.5 rounded"
                            >
                              {PROFILE_NAMES[p] ?? p}
                            </span>
                          ))}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        ))}

      {(!filtered || filtered.length === 0) && (
        <div className="text-center py-12 text-muted-foreground">
          {t('common.noResults')}
        </div>
      )}
    </div>
  );
}
