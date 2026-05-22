import type { BaseLayoutProps } from "fumadocs-ui/layouts/shared";
import { i18n, type Locale } from "./i18n";
import { BoothIcon } from "@/components/booth-icon";
import { BOOTH_URL, GITHUB_URL } from "./github";

export function baseOptions(lang: Locale): BaseLayoutProps {
  const t = {
    en: { docs: "Documentation" },
    ja: { docs: "ドキュメント" },
  };

  return {
    nav: {
      title: "LAC: Avatar Compressor",
      url: `/${lang}`,
    },
    links: [
      {
        text: t[lang].docs,
        url: `/${lang}/docs`,
      },
      {
        type: "icon",
        text: "Booth",
        icon: <BoothIcon />,
        url: BOOTH_URL,
      },
    ],
    i18n: {
      defaultLanguage: i18n.defaultLanguage,
      languages: [...i18n.languages],
    },
    githubUrl: GITHUB_URL,
  };
}
