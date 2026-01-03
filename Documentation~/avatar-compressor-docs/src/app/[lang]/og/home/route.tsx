import { ImageResponse } from 'next/og';
import { generate as DefaultImage } from 'fumadocs-ui/og';
import { i18n, getLocale, type Locale } from '@/lib/i18n';

export const revalidate = false;

const descriptions: Record<Locale, string> = {
  en: 'VRChat avatar compression toolkit - Reduce file size and VRAM usage while preserving quality',
  ja: 'VRChatアバター圧縮ツールキット - 品質を維持しながらファイルサイズとVRAM使用量を削減',
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
