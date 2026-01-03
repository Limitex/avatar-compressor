import type { BaseLayoutProps } from 'fumadocs-ui/layouts/shared';
import { i18n, type Locale } from './i18n';

export function baseOptions(lang: Locale): BaseLayoutProps {
  const t = {
    en: { docs: 'Docs' },
    ja: { docs: 'ドキュメント' },
  };

  return {
    nav: {
      title: 'Avatar Compressor',
      url: `/${lang}`,
    },
    links: [
      {
        text: t[lang].docs,
        url: `/${lang}/docs`,
      },
      {
        text: 'GitHub',
        url: 'https://github.com/limitex/avatar-compressor',
      },
    ],
    i18n: {
      defaultLanguage: i18n.defaultLanguage,
      languages: [...i18n.languages],
    },
  };
}
