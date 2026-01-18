import type { BaseLayoutProps } from 'fumadocs-ui/layouts/shared';
import { i18n, type Locale } from './i18n';
import { BoothIcon } from '@/components/booth-icon';

export function baseOptions(lang: Locale): BaseLayoutProps {
  const t = {
    en: { docs: 'Documentation' },
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
        type: 'icon',
        text: 'Booth',
        icon: <BoothIcon />,
        url: 'https://ltx.booth.pm/items/7856254',
      },
    ],
    i18n: {
      defaultLanguage: i18n.defaultLanguage,
      languages: [...i18n.languages],
    },
    githubUrl: 'https://github.com/limitex/avatar-compressor',
  };
}
