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
          })}
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

export async function generateMetadata(props: {
  params: Promise<{ lang: string; slug?: string[] }>;
}): Promise<Metadata> {
  const params = await props.params;
  const locale = getLocale(params.lang);
  const source = getSource(locale);
  const page = source.getPage(params.slug);
  if (!page) notFound();

  const ogImage = getPageImage(page, locale).url;

  return {
    title: page.data.title,
    description: page.data.description,
    openGraph: {
      title: page.data.title,
      description: page.data.description,
      images: ogImage,
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
