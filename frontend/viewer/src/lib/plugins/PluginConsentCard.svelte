<script lang="ts">
  import * as Card from '$lib/components/ui/card';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {useFeatures} from '$lib/services/feature-service';
  import type {PluginManifest} from './plugin-manifest';

  interface Props {
    pluginName: string;
    manifest: PluginManifest | undefined;
    onApprove: () => void;
    onGoBack: () => void;
  }

  const {pluginName, manifest, onApprove, onGoBack}: Props = $props();

  const features = useFeatures();
  const missingRequirements = $derived((manifest?.requires ?? []).filter(feature => !features[feature]));
</script>

<div class="absolute inset-0 grid place-items-center p-4 overflow-y-auto" data-testid="plugin-consent">
  <Card.Root class="max-w-md">
    <Card.Header>
      <Card.Title class="flex items-center gap-2">
        <Icon icon="i-mdi-shield-alert-outline" class="text-amber-500" />
        {$t`Run this plugin?`}
      </Card.Title>
      <Card.Description>{pluginName}</Card.Description>
    </Card.Header>
    <Card.Content class="space-y-3 text-sm">
      <p>{$t`Plugins are code written by people on your team (often with AI help). This one hasn't run on this device yet, or it changed since it last ran.`}</p>
      <ul class="space-y-1">
        <li class="flex items-center gap-2">
          <Icon icon="i-mdi-check" class="text-green-600" />
          {$t`Runs in a sandbox, separate from the app`}
        </li>
        <li class="flex items-center gap-2">
          {#if manifest?.permissions.includes('edit')}
            <Icon icon="i-mdi-check" class="text-green-600" />
            {$t`Can change your dictionary — it will ask you first`}
          {:else}
            <Icon icon="i-mdi-check" class="text-green-600" />
            {$t`Read-only: cannot change your dictionary`}
          {/if}
        </li>
        <li class="flex items-center gap-2">
          {#if manifest?.permissions.includes('internet')}
            <Icon icon="i-mdi-alert" class="text-amber-500" />
            {$t`Can access the internet (and could send dictionary data there)`}
          {:else}
            <Icon icon="i-mdi-check" class="text-green-600" />
            {$t`Not allowed to use the internet`}
          {/if}
        </li>
        {#each missingRequirements as feature (feature)}
          <li class="flex items-center gap-2">
            <Icon icon="i-mdi-alert" class="text-amber-500" />
            {$t`Needs “${feature}”, which this project doesn't support — it may not work properly`}
          </li>
        {/each}
      </ul>
    </Card.Content>
    <Card.Footer class="gap-2 justify-end">
      <Button variant="outline" onclick={onGoBack}>{$t`Go back`}</Button>
      <Button icon="i-mdi-play" onclick={onApprove}>{$t`Run plugin`}</Button>
    </Card.Footer>
  </Card.Root>
</div>
