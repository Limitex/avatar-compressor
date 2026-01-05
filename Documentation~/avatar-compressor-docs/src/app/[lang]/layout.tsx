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
  const description = descriptions[locale] ?? descriptions.en;

  const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';

  return {
    metadataBase: new URL(siteUrl),
    title: {
      default: 'Avatar Compressor',
      template: '%s | Avatar Compressor',
    },
    description,
    alternates: {
      canonical: `${siteUrl}/${lang}`,
      languages: {
        en: `${siteUrl}/en`,
        ja: `${siteUrl}/ja`,
      },
    },
    openGraph: {
      type: 'website',
      siteName: 'Avatar Compressor',
      title: 'Avatar Compressor',
      description,
      url: `${siteUrl}/${lang}`,
      locale: locale === 'ja' ? 'ja_JP' : 'en_US',
      images: `/${lang}/og/home`,
    },
    twitter: {
      card: 'summary_large_image',
      title: 'Avatar Compressor',
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
