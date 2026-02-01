import defaultMdxComponents from 'fumadocs-ui/mdx';
import type { MDXComponents } from 'mdx/types';
import { Tab, Tabs } from 'fumadocs-ui/components/tabs';
import { Step, Steps } from 'fumadocs-ui/components/steps';
import { Accordion, Accordions } from 'fumadocs-ui/components/accordion';
import VPMRepositoryLink from '@/components/vpm-repository-link';
import { ImagePreview } from "@/components/image-preview";
import { ChangelogContent } from "@/components/changelog-content";
import { ReleasesContent } from "@/components/releases-content";
import type { Locale } from '@/lib/i18n';

export function getMDXComponents(components?: MDXComponents, locale?: Locale): MDXComponents {
  return {
    ...defaultMdxComponents,
    Tab,
    Tabs,
    Step,
    Steps,
    Accordion,
    Accordions,
    VPMRepositoryLink,
    ImagePreview,
    ChangelogContent: () => <ChangelogContent locale={locale} />,
    ReleasesContent: () => <ReleasesContent locale={locale} />,
    ...components,
  };
}
