# Input

Single-line text field. Svelte: `import { Input } from '$lib/components/ui/input';`

**Props**: `variant` = `default | file | ghost | shell` · standard input attributes · `bind:value`.

**Class recipe**: `border-input bg-background flex h-10 w-full rounded-md border px-3 py-2 text-base shadow-xs md:text-sm focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]`. `ghost` = borderless transparent; `shell` = wrapper that owns focus styling for a nested `.real-input`.
