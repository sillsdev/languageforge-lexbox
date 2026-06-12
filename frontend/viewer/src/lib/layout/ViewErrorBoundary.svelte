<script lang="ts">
  import type {Snippet} from 'svelte';
  import * as Alert from '$lib/components/ui/alert';
  import {Button} from '$lib/components/ui/button';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import {navigate} from 'svelte-routing';

  let {
    children,
    title,
    class: className,
  }: {
    children: Snippet;
    title?: string;
    class?: string;
  } = $props();

  function errorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    if (typeof error === 'string') return error;
    return String(error);
  }
</script>

<svelte:boundary>
  <div class={cn('flex flex-col min-h-0', className)}>
    {@render children()}
  </div>
  {#snippet failed(error, reset)}
    <div class={cn('flex flex-col h-full gap-4 p-4', className)}>
      <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 self-start" />
      <Alert.Root variant="destructive" class="max-w-lg">
        <Icon icon="i-mdi-alert-circle-outline" />
        <Alert.Title>{title ?? $t`Something went wrong`}</Alert.Title>
        <Alert.Description>{errorMessage(error)}</Alert.Description>
      </Alert.Root>
      <div class="flex flex-wrap gap-2">
        <Button variant="secondary" onclick={reset}>{$t`Try again`}</Button>
        <Button variant="outline" class="paratext:hidden" onclick={() => navigate('/')}>
          <Icon icon="i-mdi-close" />
          {$t`Close Dictionary`}
        </Button>
      </div>
    </div>
  {/snippet}
</svelte:boundary>
