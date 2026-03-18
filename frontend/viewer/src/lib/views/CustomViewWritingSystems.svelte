<script lang="ts">
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import * as Checkbox from '$lib/components/ui/checkbox';
  import {t} from 'svelte-i18n-lingui';
  import {useWritingSystemService} from '$project/data';

  import type {WritingSystemSelection} from '$project/data';
  import type {IViewWritingSystem} from '$lib/dotnet-types';

  type WsKind = Extract<WritingSystemSelection, 'vernacular' | 'analysis'>;

  interface Props {
    kind: WsKind;
    selection?: IViewWritingSystem[];
  }

  let {kind, selection = $bindable()}: Props = $props();

  const writingSystemService = useWritingSystemService();
  const writingSystems = $derived(kind === 'vernacular' ? writingSystemService.vernacular : writingSystemService.analysis);
  const isAllMode = $derived(!selection);
  const selectionSet = $derived(new Set(selection?.map((ws) => ws.wsId)));

  function getLabel(wsId: string): string {
    const ws = writingSystems.find((ws) => ws.wsId === wsId);
    return ws ? `${ws.name} (${ws.abbreviation})` : wsId;
  }

  function isChecked(wsId: string): boolean {
    if (isAllMode) return true;
    return selectionSet.has(wsId);
  }

  function handleModeChange(value: string) {
    if (value !== 'all' && value !== 'custom') return;
    const currentMode = isAllMode ? 'all' : 'custom';
    if (value === currentMode) return;
    if (value === 'custom') {
      // Switching to custom: all ws selected
      selection = writingSystems.map(ws => ({wsId: ws.wsId}));
    } else {
      selection = undefined;
    }
  }

  function handleToggle(wsId: string, checked: boolean) {
    if (checked) {
      if (!selection?.some((ws) => ws.wsId === wsId)) {
        selection = [...(selection ?? []), {wsId}];
      }
    } else {
      selection = selection?.filter((ws) => ws.wsId !== wsId) ?? [];
    }
  }
</script>

<div class="rounded-md border bg-muted/20 px-3 pt-3 md:pb-3">
  <div class="pb-1 md:pb-3 text-xs font-medium text-muted-foreground">
    {#if kind === 'vernacular'}
      {$t`Vernacular`}
    {:else}
      {$t`Analysis`}
    {/if}
  </div>
  <RadioGroup.Root value={isAllMode ? 'all' : 'custom'} onValueChange={handleModeChange}>
    <RadioGroup.Item value="all" label={$t`All`} />
    <RadioGroup.Item value="custom" label={$t`Custom`} />
  </RadioGroup.Root>

  <Checkbox.Group class="md:mt-4 ms-2">
    {#each writingSystems as ws (ws.wsId)}
      {@const itemCheckboxId = `${kind}-${ws.wsId}`}
      {@const checked = isChecked(ws.wsId)}
      <Checkbox.Item id={itemCheckboxId} {checked} disabled={isAllMode} onCheckedChange={(c) => handleToggle(ws.wsId, !!c)} label={getLabel(ws.wsId)} />
    {/each}
  </Checkbox.Group>
</div>
