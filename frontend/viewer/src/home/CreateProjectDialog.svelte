<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {useProjectsService} from '$lib/services/service-provider';

  // Mirrors LexBox's server-side project-code rule (LexCore Project.ProjectCodeRegex); keep in sync.
  const projectCodePattern = /^[a-z0-9][a-z0-9-]*$/;

  let {refreshProjects}: {refreshProjects: () => Promise<void>} = $props();

  const projectsService = useProjectsService();

  let open = $state(false);
  let loading = $state(false);
  let submitted = $state(false);
  let error: string | undefined = $state();
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'create-project-dialog'});

  let name = $state('');
  let code = $state('');
  let codeEdited = $state(false);
  let vernacularWs = $state('');

  function slugify(value: string): string {
    return value.trim().toLowerCase().replace(/[^a-z0-9-]+/g, '-').replace(/^-+/, '');
  }

  let derivedCode = $derived(codeEdited ? code : slugify(name));

  let nameError = $derived(name.trim() ? undefined : $t`Name is required`);
  let codeError = $derived.by(() => {
    if (!derivedCode.trim()) return $t`Code is required`;
    if (!projectCodePattern.test(derivedCode)) return $t`Only lowercase letters, numbers and '-' are allowed`;
    return undefined;
  });
  let vernacularWsError = $derived(vernacularWs.trim() ? undefined : $t`Vernacular writing system is required`);
  let valid = $derived(!nameError && !codeError && !vernacularWsError);
  // Surfaced only after a submit attempt, then they clear live as each field is fixed.
  let fieldErrors = $derived(submitted ? [nameError, codeError, vernacularWsError].filter(e => e !== undefined) : []);

  export function openDialog() {
    error = undefined;
    submitted = false;
    name = '';
    code = '';
    codeEdited = false;
    vernacularWs = '';
    loading = false;
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
      await projectsService.createProject(name.trim(), derivedCode.trim(), vernacularWs.trim());
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
    <div class="gap-2 grid grid-cols-[auto_1fr]">
      <Label class="grid grid-cols-subgrid col-span-2 gap-4 items-center">
        {$t`Name`}
        <Input bind:value={name} autocapitalize="on" autofocus />
      </Label>
      <Label class="grid grid-cols-subgrid col-span-2 gap-4 items-center">
        {$t`Code`}
        <Input bind:value={code} oninput={() => codeEdited = true} placeholder={derivedCode} />
      </Label>
      <Label class="grid grid-cols-subgrid col-span-2 gap-4 items-center">
        {$t`Vernacular writing system`}
        <Input bind:value={vernacularWs} placeholder={$t`e.g. en, fr`} />
      </Label>
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
