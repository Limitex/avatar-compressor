import { HomeLayout } from 'fumadocs-ui/layouts/home';
import { baseOptions } from '@/lib/layout.shared';
import { getLocale } from '@/lib/i18n';
import { Footer } from '@/components/footer';

export default async function Layout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ lang: string }>;
}) {
  const { lang } = await params;
  const locale = getLocale(lang);
  return (
    <HomeLayout {...baseOptions(locale)}>
      {children}
      <Footer lang={locale} />
    </HomeLayout>
  );
}
