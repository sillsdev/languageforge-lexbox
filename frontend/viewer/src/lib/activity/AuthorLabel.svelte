<script lang="ts" module>
  // Stable per-author colour for the person icon. A small curated palette (mid-tone hues that read in both
  // light and dark) indexed by a hash of the name — not raw HSL, which drifts into muddy/low-contrast colours.
  // Colours only the icon, never the name, so readability is untouched.
  const AUTHOR_COLORS = [
    'text-red-500', 'text-orange-500', 'text-amber-600', 'text-green-600',
    'text-teal-500', 'text-sky-500', 'text-violet-500', 'text-pink-500',
  ];
  function authorColor(name: string | undefined | null): string {
    if (!name) return 'text-muted-foreground';
    let hash = 0;
    for (let i = 0; i < name.length; i++) hash = (Math.imul(hash, 31) + name.charCodeAt(i)) | 0;
    return AUTHOR_COLORS[Math.abs(hash) % AUTHOR_COLORS.length];
  }
</script>

<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import flexLogo from '$lib/assets/flex-logo.png';
  import {FIELDWORKS_AUTHOR_KEY, SYSTEM_AUTHOR_KEY, UNKNOWN_AUTHOR_KEY, authorFilterKey, wellKnownAuthorKeyToLabel} from './utils';

  let {authorId, authorName, class: className}: {
    authorId?: string;
    authorName?: string;
    class?: string;
  } = $props();

  const key = $derived(authorFilterKey({authorId, authorName}));
  // Well-known authors (System) get a translated label; FieldWorks and people fall back to their stored name.
  const label = $derived(wellKnownAuthorKeyToLabel(key) ?? authorName);
</script>

<!-- Icon trails the name in every case (consistent side): FieldWorks → its logo, System → a cog, a person →
     a person icon coloured by a stable hash of their name (an at-a-glance "who"). Unknown → a muted person. -->
<span class={cn('inline-flex items-center gap-1', className)}>
  {#if key === UNKNOWN_AUTHOR_KEY}
    <span class="italic opacity-75 truncate">{$t`Unknown`}</span>
  {:else}
    <span class="truncate">{label}</span>
  {/if}
  {#if key === FIELDWORKS_AUTHOR_KEY}
    <Icon class="size-5 shrink-0" src={flexLogo} alt={$t`FieldWorks logo`} />
  {:else if key === SYSTEM_AUTHOR_KEY}
    <Icon icon="i-mdi-cog" class="size-4 shrink-0" />
  {:else if key === UNKNOWN_AUTHOR_KEY}
    <Icon icon="i-mdi-account-circle" class="size-4 shrink-0 text-muted-foreground" />
  {:else}
    <Icon icon="i-mdi-account-circle" class="size-4 shrink-0 {authorColor(authorName)}" />
  {/if}
</span>
