import { source } from '@/lib/source';
import { createFromSource } from 'fumadocs-core/search/server';

// Static export: generate search index at build time
export const revalidate = false;

export const { staticGET: GET } = createFromSource(source, {
  // https://docs.orama.com/docs/orama-js/supported-languages
  language: 'english',
});
