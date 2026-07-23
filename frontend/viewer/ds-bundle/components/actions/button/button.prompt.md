# Button

Primary action control. Svelte: `import { Button } from '$lib/components/ui/button';`

**Props**: `variant` = `default | secondary | destructive | outline | ghost | link` · `size` = `xs | sm | default | lg | extended-fab | icon | icon-xs | icon-sm | icon-xl` · `loading` · `icon` (mdi class) · `href` (renders an anchor).

**Class recipe** (for reproducing the look): base = `inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium h-10 px-4 py-2 shadow-xs transition-all`, then the variant color pair, e.g. default = `bg-primary text-primary-foreground hover:bg-primary/90`.
