import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { fetchOrphanedConsultants } from '@/api/client';

export function OrphanedConsultants() {
  const { t } = useTranslation();
  const { data: consultants = [], isLoading } = useQuery({
    queryKey: ['orphaned-consultants'],
    queryFn: fetchOrphanedConsultants,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('users.orphaned.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('users.orphaned.description')}</p>
      </div>
      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('users.tableName')}</th>
              <th className="p-3 text-left font-medium">{t('users.tableEmail')}</th>
              <th className="p-3 text-left font-medium">{t('users.tableRole')}</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={3} className="p-3 text-center text-muted-foreground">
                  {t('common.loading')}
                </td>
              </tr>
            ) : consultants.length === 0 ? (
              <tr>
                <td colSpan={3} className="p-3 text-center text-muted-foreground">
                  {t('users.orphaned.noOrphaned')}
                </td>
              </tr>
            ) : (
              consultants.map((consultant) => (
                <tr key={consultant.id} className="border-b">
                  <td className="p-3">
                    {consultant.firstName} {consultant.lastName}
                  </td>
                  <td className="p-3 text-muted-foreground">{consultant.email}</td>
                  <td className="p-3">{consultant.roles[0] ?? '-'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
