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
  import * as Select from '$lib/components/ui/select';
  import {DotnetService, type IServerStatus} from '$lib/dotnet-types';
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
  // A Lexbox account is the better author, so with one logged in it's the default and the local name comes last
  let accounts = $state<IServerStatus[]>();
  const LOCAL_AUTHOR = 'local';
  let author = $state(LOCAL_AUTHOR);
  const authorAccount = $derived(accounts?.find(a => a.server.id === author));
  const usesLocalName = $derived(!authorAccount);

  function slugify(value: string): string {
    return value.trim().toLowerCase().replace(/[^a-z0-9-]+/g, '-').replace(/^-+/, '');
  }

  let code = $derived(`${slugify(name)}-${slugify(vernacularWs)}`);

  let nameError = $derived(name.trim() ? undefined : $t`Name is required`);
  let vernacularWsError = $derived(vernacularWs.trim() ? undefined : $t`Vernacular writing system is required`);
  let authorNameError = $derived(!askForAuthorName || !usesLocalName || authorName.trim() ? undefined : $t`Enter your name`);
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
    accounts = undefined;
    author = LOCAL_AUTHOR;
    // Hosts without Lexbox login (e.g. Storybook) have no auth service
    const authService = window.lexbox.ServiceProvider.tryGetService(DotnetService.AuthService);
    void (authService?.servers() ?? Promise.resolve([])).then(servers => {
      accounts = servers.filter(s => s.loggedIn);
      if (accounts.length) author = accounts[0].server.id;
    });
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
      if (usesLocalName && askForAuthorName) await localAuthorName.set(authorName.trim());
      await projectsService.createProject(name.trim(), code.trim(), vernacularWs.trim(), authorAccount?.server, analysisWs.trim() || undefined);
      await refreshProjects();
      open = false;
    } catch (e) {
      error = e instanceof Error ? e.message : $t`Unknown error creating project`;
      throw e;
    } finally {
      loading = false;
    }
  }

  function accountLabel(account: IServerStatus): string {
    return $t`${account.loggedInAs ?? ''} (${account.server.displayName})`;
  }

  function localLabel(): string {
    return localAuthorName.current ? $t`${localAuthorName.current} (name only)` : $t`Type a name instead`;
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
      {#if accounts?.length}
        <Label for="project-author">{$t`Author`}</Label>
        <Select.Root type="single" bind:value={author}>
          <Select.Trigger id="project-author" class="w-full">
            <span class="truncate">{authorAccount ? accountLabel(authorAccount) : localLabel()}</span>
          </Select.Trigger>
          <Select.Content>
            {#each accounts as account (account.server.id)}
              <Select.Item value={account.server.id} label={accountLabel(account)} />
            {/each}
            <Select.Item value={LOCAL_AUTHOR} label={localLabel()} />
          </Select.Content>
        </Select.Root>
      {/if}
      <!-- waits for the accounts, so the row can't turn from an input into a select while the user types -->
      {#if accounts && usesLocalName && askForAuthorName}
        <Label class="contents">
          {$t`Your name`}
          <Input bind:value={authorName} maxlength={60} autocomplete="name" autocapitalize="words" />
        </Label>
      {:else if accounts?.length === 0}
        <span class="text-sm leading-none font-medium">{$t`Your name`}</span>
        <button type="button" class={cn(inputVariants({variant: 'shell'}), 'px-3 bg-muted dark:bg-muted hover:bg-input dark:hover:bg-muted/50 cursor-pointer focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]')}
          onclick={() => void nameDialog?.ask()}>
          <span class="truncate">{localAuthorName.current}</span>
          <span class="flex items-center gap-1.5 shrink-0 font-medium"><Icon icon="i-mdi-pencil" class="size-4" />{$t`Change`}</span>
        </button>
      {/if}
      {#if accounts && usesLocalName}
        <p class="col-start-2 text-sm text-muted-foreground">{$t`Shown next to your changes in all projects you don't sync with Lexbox.`}</p>
      {/if}
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
