import { docs, docsJa } from 'fumadocs-mdx:collections/server';
import { type InferPageType, loader } from 'fumadocs-core/source';
import { lucideIconsPlugin } from 'fumadocs-core/source/lucide-icons';
import { i18n, type Locale } from './i18n';

// See https://fumadocs.dev/docs/headless/source-api for more info
export const sources = {
  en: loader({
    baseUrl: '/en/docs',
    source: docs.toFumadocsSource(),
    plugins: [lucideIconsPlugin()],
  }),
  ja: loader({
    baseUrl: '/ja/docs',
    source: docsJa.toFumadocsSource(),
    plugins: [lucideIconsPlugin()],
  }),
} as const;

export function getSource(lang: Locale) {
  return sources[lang] ?? sources[i18n.defaultLanguage];
}

export function getPageImage(page: InferPageType<(typeof sources)['en']>, lang: Locale) {
  const segments = [...page.slugs, 'image.png'];

  return {
    segments,
    url: `/${lang}/og/docs/${segments.join('/')}`,
  };
}

export async function getLLMText(page: InferPageType<(typeof sources)['en']>) {
  const processed = await page.data.getText('processed');

  return `# ${page.data.title}

${processed}`;
}
