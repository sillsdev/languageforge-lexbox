<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {useWritingSystemService, type WritingSystemSelection} from '$lib/writing-system-service.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {watch} from 'runed';

  const wsService = useWritingSystemService();

  let {value = $bindable(), wsType}: { value: string[], wsType: WritingSystemSelection } = $props();
  let writingSystems = $derived(wsService.pickWritingSystems(wsType));
  watch(() => writingSystems, () => {
    value = writingSystems.map(ws => ws.wsId);
  });
</script>
<Select.Root type="multiple" bind:value>
  <Select.Trigger class="flex-1">
    {#if value.length === 0}
      <span class="text-muted-foreground">{$t`Writing System`}</span>
    {:else if value.length === writingSystems.length}
      <span class="text-muted-foreground">
        {$t`Any Ws`}
      </span>
    {:else}
      {writingSystems.filter(w => value.includes(w.wsId)).map(w => w.abbreviation).join(', ')}
    {/if}
  </Select.Trigger>
  <Select.Content>
    <Select.Group>
      <Select.GroupHeading>{$t`Writing Systems`}</Select.GroupHeading>
      {#each writingSystems as ws}
        <Select.Item value={ws.wsId}>
          {ws.abbreviation} ({ws.wsId})
        </Select.Item>
      {/each}
    </Select.Group>
  </Select.Content>
</Select.Root>
