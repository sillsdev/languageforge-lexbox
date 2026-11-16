<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import Input, {inputVariants} from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {useProjectsService} from '$lib/services/service-provider';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import NameDialog from '$lib/author/NameDialog.svelte';
  import {useLocalAuthorName} from '$lib/author/local-author-name.svelte';

  let {refreshProjects}: {refreshProjects: () => Promise<void>} = $props();

  const projectsService = useProjectsService();
  const platformFeatures = usePlatformFeaturesService();
  const localAuthorName = useLocalAuthorName();
  let nameDialog = $state<NameDialog>();

  let open = $state(false);
  let loading = $state(false);
  let submitted = $state(false);
  let error: string | undefined = $state();
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'create-project-dialog'});

  let name = $state('');
  let vernacularWs = $state('');
  let analysisWs = $state('');
  let askForAuthorName = $state(false);
  let authorName = $state('');

  function slugify(value: string): string {
    return value.trim().toLowerCase().replace(/[^a-z0-9-]+/g, '-').replace(/^-+/, '');
  }

  let code = $derived(`${slugify(name)}-${slugify(vernacularWs)}`);

  let nameError = $derived(name.trim() ? undefined : $t`Name is required`);
  let vernacularWsError = $derived(vernacularWs.trim() ? undefined : $t`Vernacular writing system is required`);
  let authorNameError = $derived(!askForAuthorName || authorName.trim() ? undefined : $t`Enter your name`);
  let valid = $derived(!nameError && !vernacularWsError && !authorNameError);
  // Surfaced only after a submit attempt, then they clear live as each field is fixed.
  let fieldErrors = $derived(submitted ? [nameError, vernacularWsError, authorNameError].filter(e => e !== undefined) : []);

  export function openDialog() {
    error = undefined;
    submitted = false;
    name = '';
    vernacularWs = '';
    analysisWs = '';
    loading = false;
    askForAuthorName = !localAuthorName.current;
    authorName = '';
    if (askForAuthorName) void platformFeatures.service.suggestedAuthorName().then(suggested => { if (!authorName && suggested) authorName = suggested; });
    open = true;
  }

  async function createProject(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    submitted = true;
    if (!valid) return;
    loading = true;
    error = undefined;
    try {
      if (askForAuthorName) await localAuthorName.set(authorName.trim());
      await projectsService.createProject(name.trim(), code.trim(), vernacularWs.trim(), analysisWs.trim() || undefined);
      await refreshProjects();
      open = false;
    } catch (e) {
      error = e instanceof Error ? e.message : $t`Unknown error creating project`;
      throw e;
    } finally {
      loading = false;
    }
  }

  function handleKeydown(event: KeyboardEvent) {
    // Only from the inputs — Enter on a focused button must keep its native action (e.g. Cancel)
    if (event.key === 'Enter' && event.target instanceof HTMLInputElement) {
      void createProject(event);
    }
  }
</script>

<Dialog.Root bind:open={open}>
  <Dialog.DialogContent
    onkeydown={handleKeydown}
    hideClose={loading}
    interactOutsideBehavior={loading ? 'ignore' : 'close'}
    escapeKeydownBehavior={loading ? 'ignore' : 'close'}>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`New Project`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <div class="gap-x-4 gap-y-2 grid grid-cols-[auto_1fr] items-center">
      <Label class="contents">
        {$t`Name`}
        <Input bind:value={name} autocapitalize="on" autofocus />
      </Label>
      <Label class="contents">
        {$t`Code`}
        <Input readonly value={code} class="bg-muted dark:bg-muted" />
      </Label>
      <Label class="contents">
        {$t`Vernacular writing system code`}
        <Input bind:value={vernacularWs} placeholder={$t`e.g. th, sw`} />
      </Label>
      <Label class="contents">
        {$t`Analysis writing system code`}
        <Input bind:value={analysisWs} placeholder="en" />
      </Label>
      {#if askForAuthorName}
        <Label class="contents">
          {$t`Your name`}
          <Input bind:value={authorName} maxlength={60} autocomplete="name" autocapitalize="words" />
        </Label>
      {:else}
        <span class="text-sm leading-none font-medium">{$t`Your name`}</span>
        <button type="button" class={cn(inputVariants({variant: 'shell'}), 'px-3 bg-muted dark:bg-muted hover:bg-input dark:hover:bg-muted/50 cursor-pointer focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]')}
          onclick={() => void nameDialog?.ask()}>
          <span class="truncate">{localAuthorName.current}</span>
          <span class="flex items-center gap-1.5 shrink-0 font-medium"><Icon icon="i-mdi-pencil" class="size-4" />{$t`Change`}</span>
        </button>
      {/if}
      <p class="col-start-2 text-sm text-muted-foreground">{$t`Shown next to your changes in all projects you don't sync with Lexbox.`}</p>
      <div class="col-span-2 text-end space-y-2">
        {#each fieldErrors as fieldError (fieldError)}
          <p class="text-destructive">{fieldError}</p>
        {/each}
        {#if error}
          <p class="text-destructive">{error}</p>
        {/if}
      </div>
    </div>
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary" disabled={loading}>{$t`Cancel`}</Button>
      <Button onclick={createProject} disabled={loading} {loading}>{$t`Create`}</Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
<NameDialog bind:this={nameDialog} />
