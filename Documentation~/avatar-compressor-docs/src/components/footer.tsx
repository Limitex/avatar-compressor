import Link from 'next/link';
import { Github } from 'lucide-react';
import { BoothIcon } from '@/components/booth-icon';
import type { Locale } from '@/lib/i18n';
import { BOOTH_URL, GITHUB_URL } from '@/lib/github';

type Translations = {
  tagline: string;
};

const translations: Record<Locale, Translations> = {
  en: {
    tagline: 'Non-destructive VRChat avatar compression powered by NDMF.',
  },
  ja: {
    tagline: 'NDMF ベースの非破壊 VRChat アバター圧縮。',
  },
};

const linkClass =
  'text-fd-muted-foreground no-underline transition-colors hover:text-fd-foreground';

export function Footer({ lang }: { lang: Locale }) {
  const t = translations[lang];
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-fd-border">
      <div className="mx-auto w-full max-w-(--fd-layout-width) px-4 py-12 sm:py-16">
        <div className="grid gap-10 md:grid-cols-[2fr_1fr_1fr] md:gap-8">
          <div>
            <Link
              href={`/${lang}`}
              className="text-base font-semibold text-fd-foreground no-underline"
            >
              LAC: Avatar Compressor
            </Link>
            <p className="mt-3 max-w-sm text-sm text-fd-muted-foreground">
              {t.tagline}
            </p>
          </div>

          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-fd-foreground">
              Documentation
            </h3>
            <ul className="mt-4 space-y-2 text-sm">
              <li>
                <Link href={`/${lang}/docs`} className={linkClass}>
                  Documentation
                </Link>
              </li>
              <li>
                <Link href={`/${lang}/docs/installation`} className={linkClass}>
                  Installation
                </Link>
              </li>
              <li>
                <Link href={`/${lang}/docs/changelog`} className={linkClass}>
                  Changelog
                </Link>
              </li>
            </ul>
          </div>

          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-fd-foreground">
              Resources
            </h3>
            <ul className="mt-4 space-y-2 text-sm">
              <li>
                <a
                  href={GITHUB_URL}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={linkClass}
                >
                  GitHub
                </a>
              </li>
              <li>
                <a
                  href={BOOTH_URL}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={linkClass}
                >
                  Booth
                </a>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-12 flex flex-col items-center justify-between gap-4 border-t border-fd-border pt-8 sm:flex-row">
          <p className="text-xs text-fd-muted-foreground">
            © {year} Limitex
          </p>
          <div className="flex items-center gap-3">
            <a
              href={GITHUB_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="text-fd-muted-foreground transition-colors hover:text-fd-foreground"
              aria-label="GitHub"
            >
              <Github size={18} />
            </a>
            <a
              href={BOOTH_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="text-fd-muted-foreground transition-colors hover:text-fd-foreground"
              aria-label="Booth"
            >
              <BoothIcon />
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}
