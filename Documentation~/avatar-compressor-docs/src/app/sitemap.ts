import type { MetadataRoute } from 'next';
import { i18n } from '@/lib/i18n';
import { sources } from '@/lib/source';

export const revalidate = false;

export default function sitemap(): MetadataRoute.Sitemap {
  const baseUrl = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';

  const url = (path: string): string => {
    const u = new URL(path, baseUrl);
    if (!u.pathname.endsWith('/')) u.pathname += '/';
    return u.toString();
  };

  const entries: MetadataRoute.Sitemap = [];

  // Add home pages for each locale
  for (const lang of i18n.languages) {
    entries.push({
      url: url(`/${lang}`),
      changeFrequency: 'monthly',
      priority: 1,
      alternates: {
        languages: {
          'x-default': url('/en'),
          ...Object.fromEntries(
            i18n.languages.map((l) => [l, url(`/${l}`)]),
          ),
        },
      },
    });
  }

  const excludeSlugs = new Set(['releases', 'changelog']);

  // Add documentation pages for each locale
  for (const lang of i18n.languages) {
    const source = sources[lang];
    if (!source) continue;

    const pages = source.getPages();
    for (const page of pages) {
      if (page.slugs.some((s) => excludeSlugs.has(s))) continue;

      const slugPath = page.slugs.join('/');
      entries.push({
        url: url(page.url),
        changeFrequency: 'weekly',
        priority: 0.8,
        alternates: {
          languages: {
            'x-default': url(`/en/docs/${slugPath}`),
            ...Object.fromEntries(
              i18n.languages.map((l) => [l, url(`/${l}/docs/${slugPath}`)]),
            ),
          },
        },
      });
    }
  }

  return entries;
}
