<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {Alert, AlertDescription} from '$lib/components/ui/alert';
  import {Badge} from '$lib/components/ui/badge';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Input} from '$lib/components/ui/input';
  import {Label} from '$lib/components/ui/label';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import type {IPlugin} from '$lib/dotnet-types';
  import {parsePluginPermissions} from './plugin-srcdoc';
  import {examplePlugins} from './examples';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';

  interface Props {
    open: boolean;
    /** The plugin to edit; leave undefined to create a new one. */
    plugin?: IPlugin;
    onSubmit: (plugin: IPlugin) => Promise<void>;
  }

  let {open = $bindable(), plugin, onSubmit}: Props = $props();
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'plugin-editor-dialog'});

  let name = $state('');
  let html = $state('');
  let saving = $state(false);

  $effect(() => {
    if (open) {
      name = plugin?.name ?? '';
      html = plugin?.html ?? '';
    }
  });

  const permissions = $derived(html.trim() ? parsePluginPermissions(html) : []);
  const sizeKb = $derived(Math.round(html.length / 102.4) / 10);
  const canSave = $derived(!!name.trim() && !!html.trim() && !saving);

  function useExample(example: {name: string; html: string}) {
    if (!name.trim()) name = example.name;
    html = example.html;
  }

  async function save() {
    saving = true;
    try {
      await onSubmit({id: plugin?.id ?? crypto.randomUUID(), name: name.trim(), html});
      open = false;
    } finally {
      saving = false;
    }
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-3xl" interactOutsideBehavior="ignore">
    <Dialog.Header>
      <Dialog.Title>
        {#if plugin}
          {$t`Edit plugin`}
        {:else}
          {$t`New plugin`}
        {/if}
      </Dialog.Title>
      <Dialog.Description>
        {$t`A plugin is a single HTML file. Ask an AI to write one for you (see “Get AI prompt”), start from an example, or write it yourself.`}
      </Dialog.Description>
    </Dialog.Header>

    <div class="space-y-4">
      <Alert>
        <Icon icon="i-mdi-shield-alert-outline" class="text-amber-500" />
        <AlertDescription>
          {$t`Plugins are code and run for everyone on this project. Only add plugins you or your team created, or that come from someone you trust.`}
        </AlertDescription>
      </Alert>

      {#if !plugin}
        <div class="flex flex-wrap items-center gap-2">
          <span class="text-sm text-muted-foreground">{$t`Start from an example:`}</span>
          {#each examplePlugins as example (example.key)}
            <Button variant="outline" size="xs" onclick={() => useExample(example)}>{example.name}</Button>
          {/each}
        </div>
      {/if}

      <div class="space-y-2">
        <Label for="plugin-name">{$t`Name`}</Label>
        <Input id="plugin-name" bind:value={name} placeholder={$t`e.g. Dictionary stats`} />
      </div>

      <div class="space-y-2">
        <div class="flex items-center justify-between">
          <Label for="plugin-html">{$t`Plugin HTML`}</Label>
          <div class="flex items-center gap-2 text-xs text-muted-foreground">
            {#if permissions.includes('internet')}
              <Badge variant="destructive">
                <Icon icon="i-mdi-web" />
                {$t`Requests internet access`}
              </Badge>
            {:else if html.trim()}
              <Badge variant="outline">
                <Icon icon="i-mdi-web-off" />
                {$t`No internet access`}
              </Badge>
            {/if}
            {#if html.trim()}
              <span>{$t`${sizeKb} KB`}</span>
            {/if}
          </div>
        </div>
        <Textarea
          id="plugin-html"
          bind:value={html}
          placeholder={$t`Paste the plugin HTML here`}
          spellcheck="false"
          class="font-mono text-xs min-h-64 max-h-[50vh]"
        />
      </div>
    </div>

    <Dialog.Footer>
      <Button variant="outline" onclick={() => open = false}>{$t`Cancel`}</Button>
      <Button disabled={!canSave} loading={saving} onclick={() => void save()}>
        {#if plugin}
          {$t`Save changes`}
        {:else}
          {$t`Add plugin`}
        {/if}
      </Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
