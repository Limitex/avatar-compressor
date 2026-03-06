import { getPageImage, getSource, sources } from '@/lib/source';
import { DocsBody, DocsDescription, DocsPage, DocsTitle } from 'fumadocs-ui/layouts/docs/page';
import { notFound } from 'next/navigation';
import { getMDXComponents } from '@/mdx-components';
import type { Metadata } from 'next';
import { createRelativeLink } from 'fumadocs-ui/mdx';
import { i18n, getLocale } from '@/lib/i18n';

export default async function Page(props: { params: Promise<{ lang: string; slug?: string[] }> }) {
  const params = await props.params;
  const locale = getLocale(params.lang);
  const source = getSource(locale);
  const page = source.getPage(params.slug);
  if (!page) notFound();

  const MDX = page.data.body;

  return (
    <DocsPage toc={page.data.toc} full={page.data.full}>
      <DocsTitle>{page.data.title}</DocsTitle>
      <DocsDescription>{page.data.description}</DocsDescription>
      <DocsBody>
        <MDX
          components={getMDXComponents({
            a: createRelativeLink(source, page),
          }, locale)}
        />
      </DocsBody>
    </DocsPage>
  );
}

export function generateStaticParams() {
  const params: { lang: string; slug?: string[] }[] = [];

  for (const lang of i18n.languages) {
    const source = sources[lang];
    const pages = source.generateParams();
    for (const page of pages) {
      params.push({ lang, slug: page.slug });
    }
  }

  return params;
}

const noIndexPaths = new Set(['changelog', 'releases']);

export async function generateMetadata(props: {
  params: Promise<{ lang: string; slug?: string[] }>;
}): Promise<Metadata> {
  const params = await props.params;
  const locale = getLocale(params.lang);
  const source = getSource(locale);
  const page = source.getPage(params.slug);
  if (!page) notFound();

  const ogImage = getPageImage(page, locale).url;
  const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';
  const slugPath = params.slug ? params.slug.join('/') + '/' : '';
  const pageUrl = `${siteUrl}/${locale}/docs/${slugPath}`;

  const shouldNoIndex = noIndexPaths.has(params.slug?.join('/') ?? '');

  return {
    title: page.data.title,
    description: page.data.description,
    ...(shouldNoIndex && { robots: { index: false } }),
    alternates: shouldNoIndex
      ? {}
      : {
          canonical: pageUrl,
          languages: {
            'x-default': `${siteUrl}/en/docs/${slugPath}`,
            en: `${siteUrl}/en/docs/${slugPath}`,
            ja: `${siteUrl}/ja/docs/${slugPath}`,
          },
        },
    openGraph: {
      type: 'article',
      siteName: 'Avatar Compressor',
      title: page.data.title,
      description: page.data.description,
      images: ogImage,
      url: pageUrl,
      locale: locale === 'ja' ? 'ja_JP' : 'en_US',
    },
    twitter: {
      card: 'summary_large_image',
      title: page.data.title,
      description: page.data.description,
      images: ogImage,
    },
  };
}
