import { defineI18n } from 'fumadocs-core/i18n';

export const i18n = defineI18n({
  defaultLanguage: 'en',
  languages: ['en', 'ja'],
});

export type Locale = (typeof i18n.languages)[number];

export function isValidLocale(locale: string): locale is Locale {
  return i18n.languages.includes(locale as Locale);
}

export function getLocale(lang: string): Locale {
  return isValidLocale(lang) ? lang : i18n.defaultLanguage;
}

export const localeNames: Record<Locale, string> = {
  en: 'English',
  ja: '日本語',
};
