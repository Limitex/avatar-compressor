import { sources } from '@/lib/source';
import { createFromSource } from 'fumadocs-core/search/server';
import { i18n, isValidLocale, type Locale } from '@/lib/i18n';

export const revalidate = false;

const searchLanguageMap: Record<Locale, string | undefined> = {
  en: 'english',
  ja: undefined, // Japanese is not supported by Orama, use default
};

const searchHandlers: Record<Locale, () => Promise<Response>> = {} as Record<Locale, () => Promise<Response>>;
for (const locale of i18n.languages) {
  const source = sources[locale];
  const searchLang = searchLanguageMap[locale];
  const { staticGET } = createFromSource(source, searchLang ? { language: searchLang } : undefined);
  searchHandlers[locale] = staticGET;
}

export async function GET(
  _req: Request,
  { params }: { params: Promise<{ lang: string }> },
) {
  const { lang } = await params;
  if (!isValidLocale(lang)) {
    return new Response('Not found', { status: 404 });
  }
  return searchHandlers[lang]();
}

export function generateStaticParams() {
  return i18n.languages.map((locale) => ({ lang: locale }));
}
