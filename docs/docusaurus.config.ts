import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type {Options as PresetOptions, ThemeConfig} from '@docusaurus/preset-classic';
import type {Options as DocsOptions} from '@docusaurus/plugin-content-docs';

// Docusaurus appends the path of each doc relative to this directory.
const editUrl = 'https://github.com/sillsdev/languageforge-lexbox/tree/develop/docs';
const repoUrl = 'https://github.com/sillsdev/languageforge-lexbox';

const config: Config = {
  title: 'FieldWorks Lite & Lexbox Docs',
  tagline: 'Guides for using FieldWorks Lite and Lexbox, and technical documentation on how they work.',
  favicon: 'img/favicon.png',

  future: {
    v4: true,
  },

  // The final home is still undecided and docs.lexbox.org doesn't resolve yet.
  // CI sets the repo's GitHub Pages URL (see docs.yaml); without it, assets 404 under /<repo>/.
  url: process.env.DOCS_URL ?? 'https://docs.lexbox.org',
  baseUrl: process.env.DOCS_BASE_URL ?? '/',

  organizationName: 'sillsdev',
  projectName: 'languageforge-lexbox',

  onBrokenLinks: 'throw',
  onBrokenAnchors: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  markdown: {
    mermaid: true,
  },
  themes: [
    '@docusaurus/theme-mermaid',
    [
      '@easyops-cn/docusaurus-search-local',
      {
        hashed: true,
        docsDir: ['fw-lite', 'lexbox', 'technical'],
        docsRouteBasePath: ['fw-lite', 'lexbox', 'technical'],
        indexBlog: false,
        docsPluginIdForPreferredVersion: 'fw-lite',
        highlightSearchTermsOnTargetPage: true,
      },
    ],
  ],

  presets: [
    [
      'classic',
      {
        docs: false,
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies PresetOptions,
    ],
  ],

  // Route bases are deep-link targets: see docs/AGENTS.md before moving them.
  plugins: [
    [
      '@docusaurus/plugin-content-docs',
      {
        id: 'fw-lite',
        path: 'fw-lite',
        routeBasePath: 'fw-lite',
        sidebarPath: './sidebars.ts',
        editUrl,
        showLastUpdateTime: true,
      } satisfies DocsOptions,
    ],
    [
      '@docusaurus/plugin-content-docs',
      {
        id: 'lexbox',
        path: 'lexbox',
        routeBasePath: 'lexbox',
        sidebarPath: './sidebars.ts',
        editUrl,
        showLastUpdateTime: true,
      } satisfies DocsOptions,
    ],
    [
      '@docusaurus/plugin-content-docs',
      {
        id: 'technical',
        path: 'technical',
        routeBasePath: 'technical',
        sidebarPath: './sidebars.ts',
        editUrl,
        showLastUpdateTime: true,
      } satisfies DocsOptions,
    ],
  ],

  themeConfig: {
    colorMode: {
      respectPrefersColorScheme: true,
    },
    docs: {
      sidebar: {
        hideable: true,
      },
    },
    navbar: {
      title: 'FieldWorks Lite & Lexbox',
      logo: {
        alt: 'FieldWorks Lite & Lexbox',
        src: 'img/logo.svg',
        srcDark: 'img/logo-dark.svg',
      },
      items: [
        {to: '/fw-lite/', label: 'FieldWorks Lite', position: 'left'},
        {to: '/lexbox/', label: 'Lexbox', position: 'left'},
        {to: '/technical/', label: 'Technical', position: 'left'},
        {href: 'https://lexbox.org/fw-lite', label: 'Download FieldWorks Lite', position: 'right'},
        {href: repoUrl, label: 'GitHub', position: 'right'},
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {label: 'lexbox.org', href: 'https://lexbox.org'},
        {label: 'Download FieldWorks Lite', href: 'https://lexbox.org/fw-lite'},
        {label: 'Community forum', href: 'https://community.software.sil.org/c/fwlite/48'},
        {label: 'Email support', href: 'mailto:lexbox_support@groups.sil.org'},
        {label: 'GitHub', href: repoUrl},
      ],
      copyright: `Copyright © ${new Date().getFullYear()} SIL Global`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies ThemeConfig,
};

export default config;
