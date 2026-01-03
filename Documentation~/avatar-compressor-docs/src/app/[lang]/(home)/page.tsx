import Link from 'next/link';
import {
  Zap,
  Shield,
  Puzzle,
  ArrowRight,
  Gauge,
  HardDrive,
  Eye,
} from 'lucide-react';
import { getLocale, type Locale } from '@/lib/i18n';

type Translations = {
  badge: string;
  heroDescription: string;
  heroDescriptionSub: string;
  getStarted: string;
  viewOnGitHub: string;
  vramReduction: string;
  originalFiles: string;
  setup: string;
  whyTitle: string;
  whyDescription: string;
  features: { title: string; description: string }[];
  optimizeTitle: string;
  benefits: { label: string; description: string }[];
  ctaTitle: string;
  ctaDescription: string;
  installationGuide: string;
  footerBy: string;
  documentation: string;
};

const translations: Record<Locale, Translations> = {
  en: {
    badge: 'NDMF Plugin for VRChat',
    heroDescription: 'Reduce VRAM usage and file size while preserving visual quality.',
    heroDescriptionSub: 'Non-destructive compression powered by NDMF.',
    getStarted: 'Get Started',
    viewOnGitHub: 'View on GitHub',
    vramReduction: 'VRAM Reduction',
    originalFiles: 'Original Files Modified',
    setup: 'Setup',
    whyTitle: 'Why Avatar Compressor?',
    whyDescription: 'Smart compression that respects your workflow',
    features: [
      {
        title: 'Non-Destructive',
        description:
          'Your original assets are never modified. Compression is applied only at build time through NDMF, so you can freely adjust settings anytime.',
      },
      {
        title: 'Intelligent Analysis',
        description:
          'Automatically determines optimal compression for each texture based on complexity. Detailed textures stay sharp while simple ones get compressed.',
      },
      {
        title: 'Tool Compatibility',
        description:
          'Works seamlessly with Modular Avatar and Avatar Optimizer. Runs before these tools in the NDMF pipeline to ensure compatibility.',
      },
    ],
    optimizeTitle: 'Optimize Your Avatar',
    benefits: [
      {
        label: 'Faster Load Times',
        description: 'Smaller textures mean faster avatar loading for you and others',
      },
      {
        label: 'Lower VRAM Usage',
        description: 'Reduce memory footprint without sacrificing visual quality',
      },
      {
        label: 'Better Performance',
        description: 'Optimized avatars contribute to smoother VRChat experiences',
      },
    ],
    ctaTitle: 'Ready to Compress?',
    ctaDescription: 'Get started in minutes. Add the repository to your package manager and start optimizing.',
    installationGuide: 'Installation Guide',
    footerBy: 'Avatar Compressor by',
    documentation: 'Documentation',
  },
  ja: {
    badge: 'VRChat用NDMFプラグイン',
    heroDescription: '視覚的な品質を維持しながら、VRAMの使用量とファイルサイズを削減します。',
    heroDescriptionSub: 'NDMFによる非破壊圧縮。',
    getStarted: 'はじめる',
    viewOnGitHub: 'GitHubで見る',
    vramReduction: 'VRAM削減',
    originalFiles: '元ファイル変更なし',
    setup: 'セットアップ',
    whyTitle: 'なぜAvatar Compressor？',
    whyDescription: 'ワークフローを尊重するスマートな圧縮',
    features: [
      {
        title: '非破壊',
        description:
          'オリジナルのアセットは一切変更されません。圧縮はNDMFを通じてビルド時にのみ適用されるため、いつでも自由に設定を調整できます。',
      },
      {
        title: 'インテリジェント分析',
        description:
          '複雑さに基づいて各テクスチャに最適な圧縮を自動的に判断します。詳細なテクスチャはシャープに保たれ、シンプルなものは圧縮されます。',
      },
      {
        title: 'ツール互換性',
        description:
          'Modular AvatarやAvatar Optimizerとシームレスに連携します。互換性を確保するため、NDMFパイプラインでこれらのツールの前に実行されます。',
      },
    ],
    optimizeTitle: 'アバターを最適化',
    benefits: [
      {
        label: 'ロード時間の短縮',
        description: '小さなテクスチャにより、あなたと他のユーザーのアバターのロードが高速化されます',
      },
      {
        label: 'VRAM使用量の削減',
        description: '視覚的な品質を犠牲にすることなく、メモリ使用量を削減します',
      },
      {
        label: 'パフォーマンスの向上',
        description: '最適化されたアバターは、よりスムーズなVRChat体験に貢献します',
      },
    ],
    ctaTitle: '圧縮する準備はできましたか？',
    ctaDescription: '数分で始められます。パッケージマネージャーにリポジトリを追加して、最適化を開始しましょう。',
    installationGuide: 'インストールガイド',
    footerBy: 'Avatar Compressor by',
    documentation: 'ドキュメント',
  },
};

function PixelGrid() {
  return (
    <div className="absolute inset-0 overflow-hidden pointer-events-none">
      <div className="grid grid-cols-8 gap-0.5 absolute top-1/4 right-[10%] w-16 h-16 opacity-30">
        {Array.from({ length: 64 }).map((_, i) => (
          <div
            key={i}
            className="compression-pixel aspect-square rounded-sm"
            style={{ animationDelay: `${i * 0.05}s` }}
          />
        ))}
      </div>
      <div className="grid grid-cols-8 gap-0.5 absolute bottom-1/3 left-[5%] w-12 h-12 opacity-20">
        {Array.from({ length: 64 }).map((_, i) => (
          <div
            key={i}
            className="compression-pixel aspect-square rounded-sm"
            style={{ animationDelay: `${i * 0.08}s` }}
          />
        ))}
      </div>
      <div className="grid grid-cols-8 gap-0.5 absolute top-1/2 right-[25%] w-8 h-8 opacity-25">
        {Array.from({ length: 64 }).map((_, i) => (
          <div
            key={i}
            className="compression-pixel aspect-square rounded-sm"
            style={{ animationDelay: `${i * 0.03}s` }}
          />
        ))}
      </div>
    </div>
  );
}

function HeroSection({ lang, t }: { lang: Locale; t: Translations }) {
  return (
    <section className="relative min-h-[90vh] flex items-center justify-center hero-grid-bg hero-gradient overflow-hidden">
      <PixelGrid />

      <div className="relative z-10 max-w-5xl mx-auto px-6 text-center">
        <div className="animate-fade-in-up">
          <span className="inline-block px-4 py-2 mb-6 text-sm font-medium rounded-full border border-[var(--accent-cyan)]/30 bg-[var(--accent-cyan)]/5 text-[var(--accent-cyan)]">
            {t.badge}
          </span>
        </div>

        <h1 className="font-extrabold tracking-tight leading-none text-5xl sm:text-6xl md:text-7xl lg:text-8xl mb-6 animate-fade-in-up [animation-delay:0.1s]">
          <span className="text-slate-900 dark:text-white">Avatar</span>
          <br />
          <span className="glow-text text-[var(--accent-cyan)]">Compressor</span>
        </h1>

        <p className="text-lg sm:text-xl md:text-2xl text-slate-600 dark:text-neutral-400 max-w-2xl mx-auto mb-10 animate-fade-in-up [animation-delay:0.2s] leading-relaxed">
          {t.heroDescription}
          <span className="text-slate-700 dark:text-neutral-300"> {t.heroDescriptionSub}</span>
        </p>

        <div className="flex flex-col sm:flex-row gap-4 justify-center animate-fade-in-up [animation-delay:0.3s]">
          <Link
            href={`/${lang}/docs`}
            className="cta-button relative inline-flex items-center justify-center gap-2 no-underline px-8 py-4 rounded-xl font-bold text-white dark:text-black bg-gradient-to-br from-[var(--accent-cyan)] to-[var(--accent-cyan-dim)] hover:-translate-y-0.5 hover:shadow-[0_10px_40px_-10px_var(--accent-cyan)] transition-all duration-300 overflow-hidden"
          >
            {t.getStarted}
            <ArrowRight size={18} />
          </Link>
          <Link
            href="https://github.com/limitex/avatar-compressor"
            className="inline-flex items-center justify-center gap-2 no-underline px-8 py-4 rounded-xl font-semibold text-slate-700 dark:text-white bg-white/60 dark:bg-transparent border border-slate-300 dark:border-white/20 hover:border-[var(--accent-cyan)] hover:text-[var(--accent-cyan)] hover:bg-white dark:hover:bg-[var(--accent-cyan)]/5 transition-all duration-300 backdrop-blur-sm"
            target="_blank"
            rel="noopener noreferrer"
          >
            {t.viewOnGitHub}
          </Link>
        </div>

        <div className="mt-20 grid grid-cols-3 gap-8 max-w-lg mx-auto animate-fade-in-up [animation-delay:0.5s]">
          <div className="text-center">
            <div className="text-3xl sm:text-4xl font-bold mb-1 bg-gradient-to-br from-[var(--accent-cyan)] to-[var(--accent-purple)] bg-clip-text text-transparent">
              50%+
            </div>
            <div className="text-sm text-slate-500 dark:text-neutral-500">{t.vramReduction}</div>
          </div>
          <div className="text-center">
            <div className="text-3xl sm:text-4xl font-bold mb-1 bg-gradient-to-br from-[var(--accent-cyan)] to-[var(--accent-purple)] bg-clip-text text-transparent">
              0
            </div>
            <div className="text-sm text-slate-500 dark:text-neutral-500">{t.originalFiles}</div>
          </div>
          <div className="text-center">
            <div className="text-3xl sm:text-4xl font-bold mb-1 bg-gradient-to-br from-[var(--accent-cyan)] to-[var(--accent-purple)] bg-clip-text text-transparent">
              1-Click
            </div>
            <div className="text-sm text-slate-500 dark:text-neutral-500">{t.setup}</div>
          </div>
        </div>
      </div>

      <div className="absolute bottom-0 left-0 right-0 h-32 bg-gradient-to-t from-[var(--background)] to-transparent" />
    </section>
  );
}

const featureIcons = [Shield, Eye, Puzzle];
const featureColors = ['var(--accent-cyan)', 'var(--accent-purple)', 'var(--accent-magenta)'];

function FeaturesSection({ t }: { t: Translations }) {
  return (
    <section className="py-24 px-6 bg-slate-50/80 dark:bg-neutral-950/50">
      <div className="max-w-6xl mx-auto">
        <div className="text-center mb-16">
          <h2 className="font-extrabold tracking-tight leading-tight text-3xl sm:text-4xl md:text-5xl text-slate-900 dark:text-white mb-4">
            {t.whyTitle}
          </h2>
          <p className="text-slate-600 dark:text-neutral-400 text-lg max-w-xl mx-auto">
            {t.whyDescription}
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-6">
          {t.features.map((feature, index) => {
            const Icon = featureIcons[index];
            const color = featureColors[index];
            return (
              <div
                key={feature.title}
                className="feature-card p-8 bg-white dark:bg-white/[0.03] backdrop-blur-sm border border-slate-200/60 dark:border-white/[0.06] rounded-2xl transition-all duration-300 ease-out hover:-translate-y-1 hover:border-[var(--accent-cyan)]/40 animate-fade-in-up"
                style={{ animationDelay: `${index * 0.1 + 0.2}s` }}
              >
                <div
                  className="feature-icon w-14 h-14 rounded-xl flex items-center justify-center mb-6 transition-all duration-300 ease-out"
                  style={{ backgroundColor: `color-mix(in srgb, ${color} 12%, transparent)` }}
                >
                  <Icon size={28} style={{ color }} />
                </div>
                <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-3">{feature.title}</h3>
                <p className="text-slate-600 dark:text-neutral-400 leading-relaxed">{feature.description}</p>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}

const benefitIcons = [Gauge, HardDrive, Zap];

function BenefitsSection({ t }: { t: Translations }) {
  return (
    <section className="py-24 px-6 relative overflow-hidden bg-white dark:bg-transparent">
      <div className="absolute inset-0 hero-gradient opacity-40 dark:opacity-50" />

      <div className="relative max-w-4xl mx-auto">
        <div className="gradient-border p-10 sm:p-14 rounded-2xl">
          <h2 className="font-extrabold tracking-tight leading-tight text-2xl sm:text-3xl md:text-4xl text-slate-900 dark:text-white mb-12 text-center">
            {t.optimizeTitle}
          </h2>

          <div className="grid sm:grid-cols-3 gap-8">
            {t.benefits.map((benefit, index) => {
              const Icon = benefitIcons[index];
              return (
                <div
                  key={benefit.label}
                  className="text-center animate-fade-in-up"
                  style={{ animationDelay: `${index * 0.1 + 0.3}s` }}
                >
                  <div className="w-12 h-12 rounded-full bg-[var(--accent-cyan)]/10 flex items-center justify-center mx-auto mb-4">
                    <Icon size={24} className="text-[var(--accent-cyan)]" />
                  </div>
                  <h3 className="font-semibold text-slate-900 dark:text-white mb-2">{benefit.label}</h3>
                  <p className="text-sm text-slate-600 dark:text-neutral-400">{benefit.description}</p>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}

function CTASection({ lang, t }: { lang: Locale; t: Translations }) {
  return (
    <section className="py-24 px-6 bg-slate-50/80 dark:bg-neutral-950/50">
      <div className="max-w-3xl mx-auto text-center">
        <h2 className="font-extrabold tracking-tight leading-tight text-3xl sm:text-4xl md:text-5xl text-slate-900 dark:text-white mb-6">
          {t.ctaTitle}
        </h2>
        <p className="text-slate-600 dark:text-neutral-400 text-lg mb-10 max-w-xl mx-auto">
          {t.ctaDescription}
        </p>
        <Link
          href={`/${lang}/docs/installation`}
          className="cta-button relative inline-flex items-center justify-center gap-2 no-underline px-8 py-4 rounded-xl font-bold text-white dark:text-black bg-gradient-to-br from-[var(--accent-cyan)] to-[var(--accent-cyan-dim)] hover:-translate-y-0.5 hover:shadow-[0_10px_40px_-10px_var(--accent-cyan)] transition-all duration-300 overflow-hidden"
        >
          {t.installationGuide}
          <ArrowRight size={18} />
        </Link>
      </div>
    </section>
  );
}

function Footer({ lang, t }: { lang: Locale; t: Translations }) {
  return (
    <footer className="py-12 px-6 border-t border-slate-200 dark:border-neutral-800/50 bg-white dark:bg-transparent">
      <div className="max-w-6xl mx-auto flex flex-col sm:flex-row justify-between items-center gap-4">
        <div className="text-slate-500 dark:text-neutral-500 text-sm">
          {t.footerBy}{' '}
          <a
            href="https://github.com/limitex"
            className="text-slate-600 dark:text-neutral-400 hover:text-[var(--accent-cyan)] transition-colors"
            target="_blank"
            rel="noopener noreferrer"
          >
            Limitex
          </a>
        </div>
        <div className="flex gap-6">
          <a
            href="https://github.com/limitex/avatar-compressor"
            className="text-slate-500 dark:text-neutral-500 hover:text-[var(--accent-cyan)] transition-colors text-sm"
            target="_blank"
            rel="noopener noreferrer"
          >
            GitHub
          </a>
          <Link
            href={`/${lang}/docs`}
            className="text-slate-500 dark:text-neutral-500 hover:text-[var(--accent-cyan)] transition-colors text-sm"
          >
            {t.documentation}
          </Link>
        </div>
      </div>
    </footer>
  );
}

export default async function HomePage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const locale = getLocale(lang);
  const t = translations[locale];

  return (
    <main className="home-page flex-1">
      <HeroSection lang={locale} t={t} />
      <FeaturesSection t={t} />
      <BenefitsSection t={t} />
      <CTASection lang={locale} t={t} />
      <Footer lang={locale} t={t} />
      <div className="noise-overlay fixed inset-0 pointer-events-none opacity-[0.015] z-[1000]" />
    </main>
  );
}
