import { GoogleAnalytics } from '@next/third-parties/google';
import type { Metadata } from 'next';
import '../global.css';
import { Outfit, JetBrains_Mono } from 'next/font/google';
import { i18n, isValidLocale, type Locale } from '@/lib/i18n';
import { notFound } from 'next/navigation';
import { I18nRootProvider } from '@/components/i18n-provider-wrapper';

const outfit = Outfit({
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap',
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-mono',
  display: 'swap',
});

const titles: Record<Locale, string> = {
  en: 'Avatar Compressor - Non-Destructive VRChat Avatar Compression Utilities',
  ja: 'Avatar Compressor - VRChat 非破壊アバター圧縮ユーティリティ',
};

const descriptions: Record<Locale, string> = {
  en: 'VRChat non-destructive avatar modification: Become a lightweight avatar that more players can see',
  ja: 'VRChatアバター非破壊改変 より多くのプレイヤーに見てもらえる軽量アバターになろう',
};

export async function generateMetadata({
  params,
}: {
  params: Promise<{ lang: string }>;
}): Promise<Metadata> {
  const { lang } = await params;
  const locale = lang as Locale;
  const title = titles[locale] ?? titles.en;
  const description = descriptions[locale] ?? descriptions.en;

  const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';

  return {
    metadataBase: new URL(siteUrl),
    title: {
      default: title,
      template: '%s | Avatar Compressor',
    },
    description,
    alternates: {
      canonical: `${siteUrl}/${lang}/`,
      languages: {
        'x-default': `${siteUrl}/en/`,
        en: `${siteUrl}/en/`,
        ja: `${siteUrl}/ja/`,
      },
    },
    openGraph: {
      type: 'website',
      siteName: 'Avatar Compressor',
      title,
      description,
      url: `${siteUrl}/${lang}/`,
      locale: locale === 'ja' ? 'ja_JP' : 'en_US',
      images: `/${lang}/og/home`,
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description,
      images: `/${lang}/og/home`,
    },
  };
}

export function generateStaticParams() {
  return i18n.languages.map((lang) => ({ lang }));
}

export default async function LangLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ lang: string }>;
}) {
  const { lang } = await params;

  if (!isValidLocale(lang)) {
    notFound();
  }

  return (
    <html
      lang={lang}
      className={`${outfit.variable} ${jetbrainsMono.variable}`}
      style={{ fontFamily: 'var(--font-outfit), system-ui, sans-serif' }}
      suppressHydrationWarning
    >
      <body className="flex flex-col min-h-screen">
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify({
              '@context': 'https://schema.org',
              '@type': 'WebSite',
              name: 'Avatar Compressor',
              url: process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000',
              inLanguage: lang === 'ja' ? 'ja' : 'en',
            }).replace(/</g, '\\u003c'),
          }}
        />
        <I18nRootProvider lang={lang as Locale}>
          {children}
        </I18nRootProvider>
        {process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID && (
          <GoogleAnalytics gaId={process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID} />
        )}
      </body>
    </html>
  );
}
