import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Settings as SettingsIcon, User, Palette, Globe, Save, Monitor, Sun, Moon } from 'lucide-react';
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Input,
  Label,
} from '@itenium-forge/ui';
import { useAuthStore, useThemeStore } from '@/stores';
import { Separator } from '@/components/ui-extras';

const PROFILE_STORAGE_KEY = 'skillforge-profile';

interface ProfileData {
  firstName: string;
  lastName: string;
}

function loadProfile(userId: string): ProfileData {
  try {
    const stored = localStorage.getItem(`${PROFILE_STORAGE_KEY}-${userId}`);
    if (stored) return JSON.parse(stored) as ProfileData;
  } catch {
    // ignore
  }
  return { firstName: '', lastName: '' };
}

function saveProfile(userId: string, data: ProfileData) {
  localStorage.setItem(`${PROFILE_STORAGE_KEY}-${userId}`, JSON.stringify(data));
}

// ─── Profile Section ───────────────────────────────────────────────────────────

function ProfileSection() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const [saved, setSaved] = useState(false);

  const [form, setForm] = useState<ProfileData>(() =>
    user?.id ? loadProfile(user.id) : { firstName: '', lastName: '' },
  );

  function handleSave() {
    if (!user?.id) return;
    saveProfile(user.id, form);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <User className="h-5 w-5 text-primary" />
          Profile
        </CardTitle>
        <CardDescription>
          Update your display name. Changes are stored locally until a profile sync endpoint is available.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="firstName">{t('userAdmin.firstName')}</Label>
            <Input
              id="firstName"
              value={form.firstName}
              onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
              placeholder="John"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="lastName">{t('userAdmin.lastName')}</Label>
            <Input
              id="lastName"
              value={form.lastName}
              onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
              placeholder="Doe"
            />
          </div>
        </div>
        <p className="text-xs text-muted-foreground">
          Note: Profile changes are saved locally. They will sync on your next login once the profile endpoint is available.
        </p>
        <div className="flex items-center gap-3">
          <Button onClick={handleSave} className="gap-2">
            <Save className="h-4 w-4" />
            {saved ? 'Saved!' : t('settings.save')}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

// ─── Account Section ───────────────────────────────────────────────────────────

function AccountSection() {
  const { t } = useTranslation();
  const { user } = useAuthStore();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <SettingsIcon className="h-5 w-5 text-primary" />
          Account
        </CardTitle>
        <CardDescription>Your account credentials (read-only — managed by the identity provider).</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label>{t('userAdmin.username')}</Label>
            <Input value={user?.name ?? ''} readOnly className="bg-muted/50 cursor-not-allowed" />
          </div>
          <div className="space-y-2">
            <Label>{t('userAdmin.email')}</Label>
            <Input value={user?.email ?? ''} readOnly className="bg-muted/50 cursor-not-allowed" />
          </div>
        </div>
        <p className="text-xs text-muted-foreground">
          To change your username or email, contact your administrator.
        </p>
      </CardContent>
    </Card>
  );
}

// ─── Theme Section ─────────────────────────────────────────────────────────────

function ThemeSection() {
  const { t } = useTranslation();
  const { theme, setTheme } = useThemeStore();

  const themes = [
    { value: 'light' as const, label: t('settings.light'), icon: Sun },
    { value: 'dark' as const, label: t('settings.dark'), icon: Moon },
    { value: 'system' as const, label: t('settings.system'), icon: Monitor },
  ];

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Palette className="h-5 w-5 text-primary" />
          {t('settings.appearance')}
        </CardTitle>
        <CardDescription>Choose how SkillForge looks for you.</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-3 gap-3">
          {themes.map(({ value, label, icon: Icon }) => (
            <button
              key={value}
              onClick={() => setTheme(value)}
              className={`flex flex-col items-center gap-2 rounded-lg border-2 p-4 transition-all hover:bg-muted/50 ${
                theme === value
                  ? 'border-primary bg-primary/5'
                  : 'border-border'
              }`}
            >
              <Icon className={`h-6 w-6 ${theme === value ? 'text-primary' : 'text-muted-foreground'}`} />
              <span className={`text-sm font-medium ${theme === value ? 'text-primary' : 'text-muted-foreground'}`}>
                {label}
              </span>
            </button>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

// ─── Language Section ──────────────────────────────────────────────────────────

const LANGUAGES = [
  { code: 'en', label: 'English', flag: '🇬🇧' },
  { code: 'nl', label: 'Nederlands', flag: '🇳🇱' },
];

function LanguageSection() {
  const { t, i18n } = useTranslation();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Globe className="h-5 w-5 text-primary" />
          {t('settings.language')}
        </CardTitle>
        <CardDescription>Select your preferred language.</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {LANGUAGES.map(({ code, label, flag }) => (
            <button
              key={code}
              onClick={() => i18n.changeLanguage(code)}
              className={`flex items-center gap-3 rounded-lg border-2 p-3 transition-all hover:bg-muted/50 ${
                i18n.language === code || i18n.language.startsWith(code)
                  ? 'border-primary bg-primary/5'
                  : 'border-border'
              }`}
            >
              <span className="text-xl">{flag}</span>
              <span
                className={`text-sm font-medium ${
                  i18n.language === code || i18n.language.startsWith(code)
                    ? 'text-primary'
                    : 'text-muted-foreground'
                }`}
              >
                {label}
              </span>
            </button>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

// ─── Main Component ────────────────────────────────────────────────────────────

export function Settings() {
  const { t } = useTranslation();

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <h1 className="flex items-center gap-2 text-3xl font-bold">
          <SettingsIcon className="h-8 w-8 text-primary" />
          {t('settings.title')}
        </h1>
        <p className="mt-1 text-muted-foreground">Manage your account settings and preferences.</p>
      </div>

      <Separator />

      <ProfileSection />
      <AccountSection />
      <ThemeSection />
      <LanguageSection />
    </div>
  );
}
