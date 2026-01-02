import type { BaseLayoutProps } from 'fumadocs-ui/layouts/shared';

export function baseOptions(): BaseLayoutProps {
  return {
    nav: {
      title: 'Avatar Compressor',
    },
    links: [
      {
        text: 'Docs',
        url: '/docs',
      },
      {
        text: 'GitHub',
        url: 'https://github.com/limitex/avatar-compressor',
      },
    ],
  };
}
