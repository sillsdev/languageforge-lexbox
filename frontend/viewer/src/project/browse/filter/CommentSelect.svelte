<script lang="ts" module>
  export type CommentFilter = 'any' | 'unread' | 'unresolved';
</script>

<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {t} from 'svelte-i18n-lingui';

  let {value = $bindable(), labelledBy}: {value: CommentFilter | null, labelledBy?: string} = $props();
  const triggerId = $props.id();

  const options = $derived([
    {id: 'any', label: $t`With comments`},
    {id: 'unread', label: $t`With unread comments`},
    {id: 'unresolved', label: $t`With unresolved comments`},
  ] satisfies {id: CommentFilter, label: string}[]);
  const selected = $derived(options.find(o => o.id === value));
</script>

<Select.Root type="single" value={value ?? undefined} onValueChange={(id) => value = options.find(o => o.id === id)?.id ?? null}>
  <Select.Trigger id={triggerId} class="w-full" onClear={() => value = null}
    aria-labelledby={labelledBy ? `${labelledBy} ${triggerId}` : undefined}>
    {#if selected}
      {selected.label}
    {:else}
      <span class="text-muted-foreground">{$t`With or without comments`}</span>
    {/if}
  </Select.Trigger>
  <Select.Content>
    {#each options as option (option.id)}
      <Select.Item value={option.id} label={option.label}>
        {option.label}
      </Select.Item>
    {/each}
  </Select.Content>
</Select.Root>
