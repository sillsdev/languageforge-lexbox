<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import flexLogo from '$lib/assets/flex-logo.png';
  import {FIELDWORKS_AUTHOR_KEY, SYSTEM_AUTHOR_KEY, UNKNOWN_AUTHOR_KEY, authorFilterKey, wellKnownAuthorKeyToLabel} from './utils';

  let {authorId, authorName, iconClass = 'size-5', class: className}: {
    authorId?: string;
    authorName?: string;
    iconClass?: string;
    class?: string;
  } = $props();

  const key = $derived(authorFilterKey({authorId, authorName}));
  // Well-known authors (System) get a translated label; FieldWorks and people fall back to their stored name.
  const label = $derived(wellKnownAuthorKeyToLabel(key) ?? authorName);
</script>

<span class={cn('inline-flex items-center gap-1', className)}>
  {#if key === UNKNOWN_AUTHOR_KEY}
    <span class="italic opacity-75">{$t`Unknown`}</span>
  {:else}
    {label}
  {/if}
  {#if key === FIELDWORKS_AUTHOR_KEY}
    <Icon class={iconClass} src={flexLogo} alt={$t`FieldWorks logo`} />
  {:else if key === SYSTEM_AUTHOR_KEY}
    <Icon icon="i-mdi-cog" class={iconClass} />
  {/if}
</span>
