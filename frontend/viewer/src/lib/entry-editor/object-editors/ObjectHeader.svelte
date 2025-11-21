<script lang="ts">
  import {cn} from '$lib/utils';
  import {useCurrentView} from '$lib/views/view-service';
  import {pt} from '$lib/views/view-text';
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = {
    type: 'sense' | 'example';
    index?: number;
    children?: Snippet;
  } & HTMLAttributes<HTMLDivElement>;

  let { type, index, children, class: className, ...restProps }: Props = $props();

  const currentView = useCurrentView();
</script>

{#if type === 'sense'}
  <div class={cn('col-span-full flex items-center py-2 mb-1', className)} {...restProps}>
    <h2 class="text-lg text-muted-foreground">
      <span>{pt($t`Sense`, $t`Meaning`, $currentView)}</span>
      {#if index}<span>{index}</span>{/if}
    </h2>
    <hr class="grow border-t-2 mx-4" />
    {@render children?.()}
  </div>
{:else if type === 'example'}
  <div class={cn('col-span-full flex items-center mb-2', className)} {...restProps}>
    <h3 class="text-muted-foreground">
      <span>{$t`Example`}</span>
      {#if index}<span>{index}</span>{/if}
    </h3>
    <hr class="grow mx-4" />
    {@render children?.()}
  </div>
{/if}
