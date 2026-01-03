import { getPageImage, getSource, sources } from '@/lib/source';
import { notFound } from 'next/navigation';
import { ImageResponse } from 'next/og';
import { generate as DefaultImage } from 'fumadocs-ui/og';
import { i18n, getLocale, type Locale } from '@/lib/i18n';

export const revalidate = false;

export async function GET(
  _req: Request,
  { params }: { params: Promise<{ lang: string; slug: string[] }> },
) {
  const { lang, slug } = await params;
  const locale = getLocale(lang);
  const source = getSource(locale);
  const page = source.getPage(slug.slice(0, -1));
  if (!page) notFound();

  return new ImageResponse(
    <DefaultImage title={page.data.title} description={page.data.description} site="Avatar Compressor" />,
    {
      width: 1200,
      height: 630,
    },
  );
}

export function generateStaticParams() {
  const params: { lang: string; slug: string[] }[] = [];

  for (const locale of i18n.languages) {
    const source = sources[locale];
    const pages = source.getPages();
    for (const page of pages) {
      params.push({
        lang: locale,
        slug: getPageImage(page, locale).segments,
      });
    }
  }

  return params;
}
