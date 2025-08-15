<script lang="ts">
  import type { IWritingSystem } from '$lib/dotnet-types';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import { Button } from '$lib/components/ui/button';
  import { Input } from '$lib/components/ui/input';
  import { Label } from '$lib/components/ui/label';

  const miniLcmApi = useMiniLcmApi();
  interface Props {
    writingSystem: IWritingSystem;
    newWs?: boolean;
    onchange?: (writingSystem: IWritingSystem) => void;
    oncreate?: (writingSystem: IWritingSystem) => void;
  }

  let {
    writingSystem,
    newWs = false,
    onchange,
    oncreate,
  }: Props = $props();

  function updateInitialWs() {

    initialWs = JSON.parse(JSON.stringify(writingSystem));
  }
  async function onChange() {
    if (newWs) return;
    await miniLcmApi.updateWritingSystem(initialWs, writingSystem);
    updateInitialWs();
    onchange?.(writingSystem);
  }

  async function createNew() {
    await miniLcmApi.createWritingSystem(writingSystem.type, writingSystem);
    oncreate?.(writingSystem);
  }
  let initialWs = $derived(JSON.parse(JSON.stringify(writingSystem)));
</script>

<form class="flex flex-col gap-2 p-2">
  <Label>
    Language Code
    <Input readonly={!newWs} onchange={() => onChange()} bind:value={writingSystem.wsId} />
  </Label>
  <!--    todo changing the name for FieldWorks writing systems is not yet supported-->
  <Label>
    Name
    <Input readonly={!newWs} onchange={() => onChange()} bind:value={writingSystem.name} />
  </Label>
  <Label>
    Abbreviation
    <Input readonly={!newWs} onchange={() => onChange()} bind:value={writingSystem.abbreviation} />
  </Label>
  <Label>
    Font
    <Input readonly={!newWs} onchange={() => onChange()} bind:value={writingSystem.font} />
  </Label>
  {#if newWs}
    <Button variant="outline" onclick={createNew}>Create new Writing System</Button>
  {:else}
    <span>Changes are saved automatically</span>
  {/if}
</form>
