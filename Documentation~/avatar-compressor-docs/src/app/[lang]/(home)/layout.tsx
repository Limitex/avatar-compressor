import { HomeLayout } from 'fumadocs-ui/layouts/home';
import { baseOptions } from '@/lib/layout.shared';
import { getLocale } from '@/lib/i18n';

export default async function Layout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ lang: string }>;
}) {
  const { lang } = await params;
  return <HomeLayout {...baseOptions(getLocale(lang))}>{children}</HomeLayout>;
}
