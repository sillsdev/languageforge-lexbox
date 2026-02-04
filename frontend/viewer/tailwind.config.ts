/* eslint-disable @typescript-eslint/naming-convention */

import {getIconCollections} from '@egoist/tailwindcss-icons';
import {iconsPlugin} from '@egoist/tailwindcss-icons';
import containerQueries from '@tailwindcss/container-queries';
import typography from '@tailwindcss/typography';
import defaultTheme from 'tailwindcss/defaultTheme';
import {MOBILE_BREAKPOINT} from './src/css-breakpoints';

export default {
  content: [
    './src/**/!(WebComponent).{html,svelte,ts}',
    './.storybook/**/*.{html,svelte,ts}',
    //exclude icons.d.ts, because it contains all the icon classes which would cause them all to be included in the bundle
    '!./src/lib/icon-class.ts'
  ],
  plugins: [
    iconsPlugin({
      // Root source: https://github.com/Templarian/MaterialDesign
      // Our source (that pulls from ☝️): https://www.npmjs.com/package/@iconify-json/mdi
      // Search showing aliases and version (of root source) icons were introduced: https://pictogrammers.com/library/mdi/
      collections: getIconCollections(['mdi']),
    }),
    typography,
    containerQueries,
  ],
  theme: {
    extend: {
      screens: {
        'md': `${MOBILE_BREAKPOINT}px`,
        'max-xs': {'max': '400px'},
        'max-sm': {'max': '639px'},
        'max-md': {'max': '767px'},

        // Breakpoints for the entry form aka editor
        'xs-form': {'max': '800px'},
        'sm-form': {'max': '1279px'},
        'lg-form': {'min': '1280px'},

        // Breakpoints for the project view layout
        'sm-view': {'max': '800px'},
        'lg-view': '801px',
      },
      colors: {
        border: 'hsl(var(--border) / <alpha-value>)',
        input: 'hsl(var(--input) / <alpha-value>)',
        ring: 'hsl(var(--ring) / <alpha-value>)',
        background: 'hsl(var(--background) / <alpha-value>)',
        foreground: 'hsl(var(--foreground) / <alpha-value>)',
        primary: {
          DEFAULT: 'hsl(var(--primary) / <alpha-value>)',
          foreground: 'hsl(var(--primary-foreground) / <alpha-value>)'
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground) / <alpha-value>)'
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive) / <alpha-value>)',
          foreground: 'hsl(var(--destructive-foreground) / <alpha-value>)'
        },
        muted: {
          DEFAULT: 'hsl(var(--muted) / <alpha-value>)',
          foreground: 'hsl(var(--muted-foreground) / <alpha-value>)'
        },
        accent: {
          DEFAULT: 'hsl(var(--accent) / <alpha-value>)',
          foreground: 'hsl(var(--accent-foreground) / <alpha-value>)'
        },
        popover: {
          DEFAULT: 'hsl(var(--popover) / <alpha-value>)',
          foreground: 'hsl(var(--popover-foreground) / <alpha-value>)'
        },
        card: {
          DEFAULT: 'hsl(var(--card) / <alpha-value>)',
          foreground: 'hsl(var(--card-foreground) / <alpha-value>)'
        },
        sidebar: {
          DEFAULT: 'hsl(var(--sidebar-background))',
          foreground: 'hsl(var(--sidebar-foreground))',
          primary: 'hsl(var(--sidebar-primary))',
          'primary-foreground': 'hsl(var(--sidebar-primary-foreground))',
          accent: 'hsl(var(--sidebar-accent))',
          'accent-foreground': 'hsl(var(--sidebar-accent-foreground))',
          border: 'hsl(var(--sidebar-border))',
          ring: 'hsl(var(--sidebar-ring))',
        },
      },
      borderRadius: {
        xl: 'calc(var(--radius) + 4px)',
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)'
      },
      fontFamily: {
        sans: ['Noto Sans', ...defaultTheme.fontFamily.sans],
      },
      keyframes: {
        'accordion-down': {
          from: {height: '0'},
          to: {height: 'var(--bits-accordion-content-height)'},
        },
        'accordion-up': {
          from: {height: 'var(--bits-accordion-content-height)'},
          to: {height: '0'},
        },
        'caret-blink': {
          '0%,70%,100%': {opacity: '1'},
          '20%,50%': {opacity: '0'},
        },
        'shimmer': {
          from: {
            'background-position': '100% 0;'
          },
          to: {
            'background-position': '-100% 0;'
          }
        },
      },
      animation: {
        'accordion-down': 'accordion-down 0.2s ease-out',
        'accordion-up': 'accordion-up 0.2s ease-out',
        'caret-blink': 'caret-blink 1.25s ease-out infinite',
        'shimmer': 'shimmer 2.0s linear infinite',
      },
      backgroundImage: {
        'shimmer': 'linear-gradient(90deg, hsl(var(--muted-foreground)/0.2) 20%, hsl(var(--foreground)/0.3) 50%, hsl(var(--muted-foreground)/0.2) 65%)',
      },
      backgroundSize: {
        'shimmer': '200% 100%',
      },
    },
  },
};
