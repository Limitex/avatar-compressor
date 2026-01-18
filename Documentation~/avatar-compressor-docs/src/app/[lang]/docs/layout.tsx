import { getSource } from '@/lib/source';
import { DocsLayout } from 'fumadocs-ui/layouts/docs';
import { baseOptions } from '@/lib/layout.shared';
import { BoothIcon } from '@/components/booth-icon';
import { getLocale } from '@/lib/i18n';

export default async function Layout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ lang: string }>;
}) {
  const { lang } = await params;
  const locale = getLocale(lang);
  const source = getSource(locale);

  return (
    <DocsLayout
      tree={source.getPageTree()}
      {...baseOptions(locale)}
      links={[
        {
          type: 'icon',
          text: 'Booth',
          icon: <BoothIcon />,
          url: 'https://ltx.booth.pm/items/7856254',
        },
      ]}
    >
      {children}
    </DocsLayout>
  );
}
