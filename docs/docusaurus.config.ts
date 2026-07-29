import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type {Options as PresetOptions, ThemeConfig} from '@docusaurus/preset-classic';
import type {Options as DocsOptions} from '@docusaurus/plugin-content-docs';

// Docusaurus appends the path of each doc relative to this directory.
const editUrl = 'https://github.com/sillsdev/languageforge-lexbox/tree/develop/docs';
const repoUrl = 'https://github.com/sillsdev/languageforge-lexbox';

const config: Config = {
  title: 'FieldWorks Lite & Lexbox Docs',
  tagline: 'Guides for using FieldWorks Lite and Lexbox, and technical documentation for developers.',
  favicon: 'img/favicon.png',

  future: {
    v4: true,
  },

  // Deploy target and DNS are still a team decision.
  url: 'https://docs.lexbox.org',
  baseUrl: '/',

  organizationName: 'sillsdev',
  projectName: 'languageforge-lexbox',

  onBrokenLinks: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],

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

  plugins: [
    [
      '@docusaurus/plugin-content-docs',
      {
        id: 'user-guide',
        path: 'user-guide',
        routeBasePath: 'user-guide',
        sidebarPath: './sidebars.ts',
        editUrl,
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
      } satisfies DocsOptions,
    ],
  ],

  themeConfig: {
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'FieldWorks Lite & Lexbox',
      logo: {
        alt: 'Lexbox logo',
        src: 'img/logo.svg',
        srcDark: 'img/logo-dark.svg',
      },
      items: [
        {to: '/user-guide/', label: 'User guide', position: 'left'},
        {to: '/technical/', label: 'Technical', position: 'left'},
        {href: repoUrl, label: 'GitHub', position: 'right'},
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {label: 'Lexbox', href: 'https://lexbox.org'},
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
