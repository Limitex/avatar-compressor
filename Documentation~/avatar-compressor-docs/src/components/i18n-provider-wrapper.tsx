'use client';

import { RootProvider } from 'fumadocs-ui/provider/next';
import { i18n, localeNames, type Locale } from '@/lib/i18n';

export function I18nRootProvider({
  lang,
  children,
}: {
  lang: Locale;
  children: React.ReactNode;
}) {
  return (
    <RootProvider
      i18n={{
        locale: lang,
        locales: i18n.languages.map((l) => ({
          name: localeNames[l],
          locale: l,
        })),
      }}
      search={{
        options: {
          type: 'static',
          api: `/api/search/${lang}/`,
        },
      }}
    >
      {children}
    </RootProvider>
  );
}
