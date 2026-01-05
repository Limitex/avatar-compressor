import type { MetadataRoute } from 'next';
import { i18n } from '@/lib/i18n';
import { sources } from '@/lib/source';

export const revalidate = false;

export default function sitemap(): MetadataRoute.Sitemap {
  const baseUrl = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';

  const url = (path: string): string => new URL(path, baseUrl).toString();

  const entries: MetadataRoute.Sitemap = [];

  // Add root page (redirects to default language)
  entries.push({
    url: url('/'),
    changeFrequency: 'monthly',
    priority: 0.5,
  });

  // Add home pages for each locale
  for (const lang of i18n.languages) {
    entries.push({
      url: url(`/${lang}`),
      changeFrequency: 'monthly',
      priority: 1,
    });
  }

  // Add documentation pages for each locale
  for (const lang of i18n.languages) {
    const source = sources[lang];
    if (!source) continue;

    const pages = source.getPages();
    for (const page of pages) {
      entries.push({
        url: url(page.url),
        changeFrequency: 'weekly',
        priority: 0.8,
      });
    }
  }

  return entries;
}
