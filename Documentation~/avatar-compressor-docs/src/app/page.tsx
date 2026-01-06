import { permanentRedirect } from 'next/navigation';
import { i18n } from '@/lib/i18n';

export default function RootPage() {
  permanentRedirect(`/${i18n.defaultLanguage}`);
}
