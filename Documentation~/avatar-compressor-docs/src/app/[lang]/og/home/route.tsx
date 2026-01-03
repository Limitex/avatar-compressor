import { ImageResponse } from 'next/og';
import { generate as DefaultImage } from 'fumadocs-ui/og';
import { i18n, getLocale, type Locale } from '@/lib/i18n';

export const revalidate = false;

const descriptions: Record<Locale, string> = {
  en: 'VRChat non-destructive avatar modification: Become a lightweight avatar that more players can see',
  ja: 'VRChatアバター非破壊改変 より多くのプレイヤーに見てもらえる軽量アバターになろう',
};

export async function GET(_req: Request, { params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const locale = getLocale(lang);
  const description = descriptions[locale];

  return new ImageResponse(
    (
      <DefaultImage
        title="Avatar Compressor"
        description={description}
        site="Avatar Compressor"
      />
    ),
    {
      width: 1200,
      height: 630,
    },
  );
}

export function generateStaticParams() {
  return i18n.languages.map((lang) => ({ lang }));
}
