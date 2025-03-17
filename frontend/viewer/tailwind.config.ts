/* eslint-disable @typescript-eslint/naming-convention */

import {getIconCollections} from '@egoist/tailwindcss-icons';
import {iconsPlugin} from '@egoist/tailwindcss-icons';
import containerQueries from '@tailwindcss/container-queries';
import typography from '@tailwindcss/typography';
import type {Config} from 'tailwindcss';
import svelteUx from 'svelte-ux/plugins/tailwind.cjs';
import tailwindcssAnimate from 'tailwindcss-animate';
import {fontFamily} from 'tailwindcss/defaultTheme';

export default {
  darkMode: ['class'],
  content: [
    './src/**/!(WebComponent).{html,svelte,ts}',
    './node_modules/svelte-ux/**/*.{svelte,js}',
    //exclude icons.d.ts, because it contains all the icon classes which would cause them all to be included in the bundle
    '!./src/lib/icon-class.ts'
  ],
  safelist: ['dark'],
  variants: {
    extend: {},
  },
  plugins: [
    iconsPlugin({
      // Root source: https://github.com/Templarian/MaterialDesign
      // Our source (that pulls from ☝️): https://www.npmjs.com/package/@iconify-json/mdi
      // Search showing aliases and version (of root source) icons were introduced: https://pictogrammers.com/library/mdi/
      collections: getIconCollections(['mdi']),
    }),
    svelteUx({colorSpace: 'hsl'}),
    typography,
    containerQueries,
    tailwindcssAnimate,
  ],
  ux: {
    themeRoot: ':root, :host',
    themes: {
      'light': {
        'color-scheme': 'light',
        '--base-text': '#394E6A',
        'primary': '#0050CC',
        'secondary': '#D6E6FF',
        'accent': '#75d7ce',
        'neutral': '#70acc7',
        'surface-100': '#ffffff',
        'surface-200': '#f4f5f6',
        'surface-300': '#d1d5db',
        'surface-content': '#394E6A',
        'info': '#0E94FF',
        'warning': '#ff6c00',
        '--rounded-btn': '1.9rem',
        '--tab-radius': '0.7rem',
      },
      'dark': {
        'color-scheme': 'dark',
        '--base-text': '#e6e6e6',
        'primary': '#4882D0',
        'secondary': '#152747',
        'accent': '#1b4d4d',
        'neutral': '#331800',
        'neutral-content': '#FFE7A3',
        'surface-100': '#151515',
        'surface-200': '#252426',
        'surface-300': '#2e2d2f',
        'surface-content': '#dca54c',
        'info': '#66c6ff',
        'success': '#87d039',
        'warning': '#ff6c00',
        'danger': '#ff6f6f',
      }
    }
  },

  theme: {
    container: {
      center: true,
      padding: '2rem',
      screens: {
        '2xl': '1400px'
      }
    },
    extend: {
      screens: {
        'max-sm': {'max': '639px'},
        'max-md': {'max': '767px'},

        // Breakpoints for the entry form aka editor
        'xs-form': {'max': '800px'},
        'sm-form': {'max': '1279px'},
        'lg-form': {'min': '1280px'},

        // Breakpoints for the project view layout
        'sm-view': {'max': '800px'},
        'lg-view': '801px',
        //only active when the user has a mouse (eg they can hover over an element)
        'pointer': {'raw': '(hover: hover)'}
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
        sans: [...fontFamily.sans],
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
      },
      animation: {
        'accordion-down': 'accordion-down 0.2s ease-out',
        'accordion-up': 'accordion-up 0.2s ease-out',
        'caret-blink': 'caret-blink 1.25s ease-out infinite',
      },
    },
  },
} satisfies Config;
