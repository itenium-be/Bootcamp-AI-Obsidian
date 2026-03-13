import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Card, CardContent, Button } from '@itenium-forge/ui';
import { GraduationCap, Award, Trophy, Sparkles, Medal } from 'lucide-react';
import { fetchCertificates, type Certificate } from '@/api/client';

function CertificateCard({ cert }: { cert: Certificate }) {
  const { t } = useTranslation();

  return (
    <div className="border-2 border-yellow-400 rounded-xl bg-gradient-to-br from-yellow-50 to-amber-50 dark:from-yellow-950 dark:to-amber-950 p-6 shadow-md hover:shadow-lg transition-shadow relative overflow-hidden">
      {/* Decorative background element */}
      <div className="absolute top-0 right-0 w-32 h-32 opacity-5">
        <GraduationCap className="w-full h-full text-yellow-600" />
      </div>

      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-full bg-yellow-100 dark:bg-yellow-900 p-2">
            <GraduationCap className="size-6 text-yellow-600 dark:text-yellow-400" />
          </div>
          <div>
            <p className="text-xs font-medium text-yellow-700 dark:text-yellow-400 uppercase tracking-wide">
              SkillForge
            </p>
            <p className="text-xs text-muted-foreground">Certificate of Completion</p>
          </div>
        </div>
        <Medal className="size-5 text-yellow-500" />
      </div>

      {/* Course name */}
      <div className="mb-4">
        <h3 className="text-xl font-bold leading-tight">{cert.courseName}</h3>
        <p className="text-sm text-muted-foreground mt-1">{cert.learnerName}</p>
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between border-t border-yellow-200 dark:border-yellow-800 pt-3 mt-3">
        <div>
          <p className="text-xs text-muted-foreground">{t('myCertificates.earnedOn')}</p>
          <p className="text-sm font-medium">
            {new Date(cert.issuedAt).toLocaleDateString(undefined, {
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </p>
        </div>
        <div className="text-right">
          <p className="text-xs text-muted-foreground">{t('myCertificates.certificateNumber')}</p>
          <p className="text-xs font-mono font-medium text-yellow-700 dark:text-yellow-400">{cert.certificateNumber}</p>
        </div>
      </div>

      {/* Branding row */}
      <div className="flex items-center gap-1 mt-3">
        <Award className="size-3 text-yellow-600" />
        <span className="text-xs text-yellow-700 dark:text-yellow-400 font-medium">Itenium SkillForge</span>
        <Sparkles className="size-3 text-yellow-500 ml-auto" />
      </div>
    </div>
  );
}

export function MyCertificates() {
  const { t } = useTranslation();

  const { data: certificates, isLoading } = useQuery({
    queryKey: ['certificates'],
    queryFn: fetchCertificates,
  });

  const total = certificates?.length ?? 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">{t('myCertificates.title')}</h1>
        <p className="text-muted-foreground">{t('myCertificates.subtitle')}</p>
      </div>

      {/* Stats card */}
      {total > 0 && (
        <Card>
          <CardContent className="pt-4 flex items-center gap-4">
            <div className="rounded-full bg-yellow-100 dark:bg-yellow-900 p-3">
              <Trophy className="size-6 text-yellow-600 dark:text-yellow-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{total}</p>
              <p className="text-sm text-muted-foreground">
                {total === 1 ? 'Certificate earned' : 'Certificates earned'}
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Certificate grid */}
      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2">
          {Array.from({ length: 2 }).map((_, i) => (
            <div
              key={i}
              className="border-2 border-yellow-200 rounded-xl bg-yellow-50 dark:bg-yellow-950 p-6 animate-pulse h-52"
            />
          ))}
        </div>
      ) : total === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 gap-4 text-muted-foreground">
          <Trophy className="size-16 opacity-20" />
          <div className="text-center">
            <p className="text-lg font-medium">{t('myCertificates.noCertificates')}</p>
            <p className="text-sm mt-1">{t('myCertificates.completeCourseToEarn')}</p>
          </div>
          <Button asChild>
            <Link to="/catalog">
              <GraduationCap className="size-4 mr-2" />
              Browse Courses
            </Link>
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {(certificates ?? []).map((cert) => (
            <CertificateCard key={cert.id} cert={cert} />
          ))}
        </div>
      )}
    </div>
  );
}
