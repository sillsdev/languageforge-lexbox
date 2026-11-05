<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import type {ExampleCapability, ExamplePlugin} from '../../examples';
  import {useStudio} from '../plugin-studio.svelte';

  interface Props {
    example: ExamplePlugin;
  }

  const {example}: Props = $props();
  const studio = useStudio();

  const capabilityLabels: Record<ExampleCapability, string> = $derived({
    internet: $t`Uses the internet`,
    microphone: $t`Records from the microphone`,
    camera: $t`Uses the camera`,
    'entry-menu': $t`Runs from an entry's menu`,
  });

  const asks = $derived.by(() => {
    const manifest = studio.draft.manifest;
    if (!manifest) return [];
    const lines = [
      manifest.permissions.includes('edit')
        ? $t`Can change the dictionary, and asks before each change`
        : $t`Reads the dictionary, never changes it`,
      manifest.permissions.includes('internet') ? $t`Uses the internet` : $t`No internet`,
    ];
    if (manifest.contexts.includes('entry')) lines.push(capabilityLabels['entry-menu']);
    for (const capability of example.capabilities ?? []) {
      if (capability === 'microphone' || capability === 'camera') lines.push(capabilityLabels[capability]);
    }
    return lines;
  });
</script>

<div class="space-y-4">
  <p>{example.description}</p>
  <div class="space-y-2">
    <h3 class="text-sm font-semibold">{$t`What it asks for`}</h3>
    <ul class="space-y-1.5 text-sm">
      {#each asks as line (line)}
        <li class="rounded-md bg-muted px-3 py-2">{line}</li>
      {/each}
    </ul>
  </div>
  <div class="space-y-1">
    <h3 class="text-sm font-semibold">{$t`If you add it`}</h3>
    <p class="text-sm text-muted-foreground">
      {$t`Everyone on this project will see it under Plugins. You can change or remove it later.`}
    </p>
  </div>
  <Button variant="outline" icon="i-mdi-creation" onclick={() => studio.tab = 'ai'}>{$t`Make it your own with AI`}</Button>
</div>
