import Link from 'next/link';
import { ArrowRight, ExternalLink, Github } from 'lucide-react';
import { BoothIcon } from '@/components/booth-icon';
import VPMRepositoryLink from '@/components/vpm-repository-link';
import { ComponentShowcase, type ShowcaseItem } from '@/components/component-showcase';
import { getLocale, i18n, type Locale } from '@/lib/i18n';

const VPM_REPO_URL = 'https://vpm.limitex.dev/index.json';

// Replace `PLACEHOLDER` slots with real entries as new components ship.
// The carousel hides side previews automatically when only one item is configured.
const PLACEHOLDER: ShowcaseItem = { placeholder: true };
const SHOWCASE_ITEMS: ShowcaseItem[] = [
  {
    src: '/img/components/texture-compressor.webp',
    alt: 'LAC Texture Compressor — Unity Inspector',
    width: 664,
    height: 1607,
  },
  PLACEHOLDER,
];

export function generateStaticParams() {
  return i18n.languages.map((lang) => ({ lang }));
}

type Translations = {
  tagline: string;
  getStarted: string;
  viewOnGitHub: string;
  viewOnBooth: string;
  installHeading: string;
  installDescription: string;
  installManual: string;
  pairingHeading: string;
  pairingDescription: string;
  recommendedLabel: string;
  aaoDescription: string;
  compatibleLabel: string;
  maDescription: string;
  ctaTitle: string;
  ctaDescription: string;
  installationGuide: string;
};

const translations: Record<Locale, Translations> = {
  en: {
    tagline:
      'Non-destructive VRChat avatar compression powered by NDMF. Become a lightweight avatar that more players can see.',
    getStarted: 'Get Started',
    viewOnGitHub: 'View on GitHub',
    viewOnBooth: 'View on Booth',
    installHeading: 'Install',
    installDescription: 'Add the Limitex VPM repository to your package manager.',
    installManual: 'Or add manually:',
    pairingHeading: 'Works with NDMF tools',
    pairingDescription:
      'Combine LAC with other NDMF tools to optimize your avatar end-to-end.',
    recommendedLabel: 'Recommended',
    aaoDescription:
      'Non-destructive avatar optimization utilities by anatawa12. AAO targets structure; LAC targets compression.',
    compatibleLabel: 'Compatible',
    maDescription:
      'Drag-and-drop avatar assembly by bd_. Works with LAC out of the box.',
    ctaTitle: 'Ready to compress?',
    ctaDescription: 'Walk through the full setup in the Quick Start guide.',
    installationGuide: 'Installation Guide',
  },
  ja: {
    tagline:
      'NDMF ベースの非破壊 VRChat アバター圧縮。より多くのプレイヤーに見てもらえる軽量アバターになろう。',
    getStarted: 'はじめる',
    viewOnGitHub: 'GitHub で見る',
    viewOnBooth: 'Booth で見る',
    installHeading: 'インストール',
    installDescription: 'Limitex VPM リポジトリをパッケージマネージャーに追加します。',
    installManual: 'または手動で追加:',
    pairingHeading: 'NDMF ツールと併用',
    pairingDescription:
      '他の NDMF ツールと組み合わせて、アバター全体を最適化しましょう。',
    recommendedLabel: '推奨',
    aaoDescription:
      'anatawa12 氏による非破壊なアバター最適化ユーティリティ群。AAO は構造最適化を、LAC は圧縮を担当します。',
    compatibleLabel: '互換',
    maDescription:
      'bd_ 氏によるドラッグ&ドロップ式のアバター組み立て。LAC とそのまま併用できます。',
    ctaTitle: '今すぐ試す',
    ctaDescription: '完全なセットアップ手順は Quick Start で確認できます。',
    installationGuide: 'インストールガイド',
  },
};

const buttonBase =
  'inline-flex w-full items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-medium no-underline transition-colors sm:w-auto sm:min-w-[150px]';
const primaryButton = `${buttonBase} bg-fd-primary text-fd-primary-foreground hover:bg-fd-primary/90`;
const secondaryButton = `${buttonBase} border border-fd-border bg-fd-background text-fd-foreground hover:bg-fd-accent hover:text-fd-accent-foreground`;

export default async function HomePage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const locale = getLocale(lang);
  const t = translations[locale];

  return (
    <main className="flex flex-1 flex-col">
      <section className="mx-auto w-full max-w-(--fd-layout-width) px-4 pt-32 pb-16 sm:pt-40 sm:pb-24">
        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8">
          <div className="text-center lg:text-left">
            <h1 className="whitespace-nowrap text-3xl font-bold tracking-tight text-fd-foreground sm:text-4xl lg:text-4xl xl:text-5xl 2xl:text-6xl">
              LAC: Avatar Compressor
            </h1>
            <p className="mt-6 text-base text-fd-muted-foreground sm:text-lg">
              {t.tagline}
            </p>
            <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row lg:justify-start">
              <Link href={`/${locale}/docs`} className={primaryButton}>
                {t.getStarted}
                <ArrowRight size={16} />
              </Link>
              <a
                href="https://github.com/limitex/avatar-compressor"
                target="_blank"
                rel="noopener noreferrer"
                className={secondaryButton}
              >
                <Github size={16} />
                {t.viewOnGitHub}
              </a>
              <a
                href="https://ltx.booth.pm/items/7856254"
                target="_blank"
                rel="noopener noreferrer"
                className={secondaryButton}
              >
                <BoothIcon />
                {t.viewOnBooth}
              </a>
            </div>
          </div>

          <ComponentShowcase items={SHOWCASE_ITEMS} />
        </div>
      </section>

      <section className="mx-auto w-full max-w-(--fd-layout-width) px-4 py-24 sm:py-32">
        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8">
          <div className="text-center lg:order-2 lg:text-right">
            <h2 className="text-2xl font-semibold tracking-tight text-fd-foreground sm:text-3xl xl:text-4xl">
              {t.installHeading}
            </h2>
            <p className="mt-6 text-base text-fd-muted-foreground sm:text-lg">
              {t.installDescription}
            </p>
          </div>

          <div className="lg:order-1">
            <div className="flex flex-col items-center gap-3 sm:flex-row sm:justify-center lg:justify-start">
              <VPMRepositoryLink
                repoUrl={VPM_REPO_URL}
                label="Add to ALCOM"
                method="alcom"
                eventName="add_repository_home"
                className={primaryButton}
              />
              <VPMRepositoryLink
                repoUrl={VPM_REPO_URL}
                label="Add to VCC"
                method="vcc"
                eventName="add_repository_home"
                className={secondaryButton}
              />
            </div>
            <div className="mt-8 text-center lg:text-left">
              <p className="text-sm text-fd-muted-foreground">{t.installManual}</p>
              <code className="mt-2 block break-all rounded bg-fd-secondary px-3 py-2 text-xs text-fd-foreground">
                {VPM_REPO_URL}
              </code>
            </div>
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-(--fd-layout-width) px-4 py-24 sm:py-32">
        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8">
          <div className="text-center lg:text-left">
            <h2 className="text-2xl font-semibold tracking-tight text-fd-foreground sm:text-3xl xl:text-4xl">
              {t.pairingHeading}
            </h2>
            <p className="mt-6 text-base text-fd-muted-foreground sm:text-lg">
              {t.pairingDescription}
            </p>
          </div>

          <div className="flex flex-col divide-y divide-fd-border">
            <a
              href="https://vpm.anatawa12.com/avatar-optimizer/"
              target="_blank"
              rel="noopener noreferrer"
              className="group block py-6 no-underline first:pt-0"
            >
              <p className="text-xs font-semibold uppercase tracking-wider text-fd-primary">
                {t.recommendedLabel}
              </p>
              <h3 className="mt-1 flex items-center gap-2 text-lg font-semibold text-fd-foreground transition-colors group-hover:text-fd-primary">
                AAO: Avatar Optimizer
                <ExternalLink size={14} className="text-fd-muted-foreground transition-colors group-hover:text-fd-primary" />
              </h3>
              <p className="mt-2 text-sm text-fd-muted-foreground">
                {t.aaoDescription}
              </p>
            </a>

            <a
              href="https://modular-avatar.nadena.dev/"
              target="_blank"
              rel="noopener noreferrer"
              className="group block py-6 no-underline last:pb-0"
            >
              <p className="text-xs font-semibold uppercase tracking-wider text-fd-muted-foreground">
                {t.compatibleLabel}
              </p>
              <h3 className="mt-1 flex items-center gap-2 text-lg font-semibold text-fd-foreground transition-colors group-hover:text-fd-foreground/80">
                Modular Avatar
                <ExternalLink size={14} className="text-fd-muted-foreground transition-colors group-hover:text-fd-foreground/80" />
              </h3>
              <p className="mt-2 text-sm text-fd-muted-foreground">
                {t.maDescription}
              </p>
            </a>
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-(--fd-layout-width) px-4 pt-24 pb-32 text-center sm:pt-32 sm:pb-40">
        <h2 className="text-2xl font-semibold tracking-tight text-fd-foreground sm:text-3xl xl:text-4xl">
          {t.ctaTitle}
        </h2>
        <p className="mx-auto mt-4 max-w-xl text-fd-muted-foreground">{t.ctaDescription}</p>
        <Link href={`/${locale}/docs/installation`} className={`${secondaryButton} mt-8`}>
          {t.installationGuide}
          <ArrowRight size={16} />
        </Link>
      </section>
    </main>
  );
}
