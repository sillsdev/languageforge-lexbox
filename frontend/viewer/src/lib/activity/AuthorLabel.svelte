<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import flexLogo from '$lib/assets/flex-logo.png';
  import {FIELDWORKS_AUTHOR_KEY, SYSTEM_AUTHOR_KEY, authorDisplayName, authorFilterKey} from './utils';

  let {authorId, authorName, class: className}: {
    authorId?: string;
    authorName?: string;
    class?: string;
  } = $props();

  const key = $derived(authorFilterKey({authorId, authorName}));
  const label = $derived(authorDisplayName({authorId, authorName}));
  // Nobody chose these labels (no name was stored), so they read as placeholders
  const placeholder = $derived(!authorName && key !== SYSTEM_AUTHOR_KEY);
</script>

<span class={cn('inline-flex items-center gap-1', className)}>
  {#if placeholder}
    <span class="italic opacity-75">{label}</span>
  {:else}
    {label}
  {/if}
  {#if key === FIELDWORKS_AUTHOR_KEY}
    <Icon class="size-5" src={flexLogo} alt={$t`FieldWorks logo`} />
  {:else if key === SYSTEM_AUTHOR_KEY}
    <Icon icon="i-mdi-cog" class="size-4" />
  {/if}
</span>
