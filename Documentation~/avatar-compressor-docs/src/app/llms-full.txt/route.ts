import { getLLMText, sources } from '@/lib/source';
import { i18n } from '@/lib/i18n';

export const revalidate = false;

export async function GET() {
  const allPages: Promise<string>[] = [];

  for (const lang of i18n.languages) {
    const source = sources[lang];
    const pages = source.getPages().map(getLLMText);
    allPages.push(...pages);
  }

  const scanned = await Promise.all(allPages);

  return new Response(scanned.join('\n\n'));
}
