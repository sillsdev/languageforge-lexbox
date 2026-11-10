<script lang="ts">
  import {tick} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import DevContent from '$lib/layout/DevContent.svelte';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {useLocalAuthorName} from './local-author-name.svelte';

  const authorName = useLocalAuthorName();
  const platformFeatures = usePlatformFeaturesService();
  let open = $state(false);
  let name = $state('');
  let submitted = $state(false);
  let changing = $state(false);
  let fromMenu = $state(false);
  let input = $state<HTMLInputElement | null>(null);
  let resolveAsk: ((saved: boolean) => void) | undefined;
  useBackHandler({addToStack: () => open, onBack: () => close(false), key: 'name-dialog'});

  const error = $derived(submitted && !name.trim() ? $t`Enter your name` : undefined);

  /** Resolves true once a name is saved, false when cancelled. */
  export function ask(options: {fromMenu?: boolean} = {}): Promise<boolean> {
    changing = !!authorName.current;
    fromMenu = !!options.fromMenu;
    name = authorName.current;
    submitted = false;
    open = true;
    if (!name) void platformFeatures.service.suggestedAuthorName().then(async suggested => {
      if (name || !suggested) return;
      name = suggested;
      await tick();
      input?.select();
    });
    return new Promise(resolve => resolveAsk = resolve);
  }

  function close(saved: boolean) {
    resolveAsk?.(saved);
    resolveAsk = undefined;
    open = false;
  }

  async function submit(e: Event) {
    e.preventDefault();
    submitted = true;
    if (error) return;
    await authorName.set(name.trim());
    close(true);
  }
</script>

<Dialog.Root bind:open onOpenChange={isOpen => { if (!isOpen) close(false); }}>
  <Dialog.DialogContent>
    <form onsubmit={submit} class="contents">
      <Dialog.DialogHeader>
        <Dialog.DialogTitle>{$t`Your name`}</Dialog.DialogTitle>
        <Dialog.DialogDescription>{$t`Shown next to your changes in projects you don't sync with Lexbox.`}</Dialog.DialogDescription>
      </Dialog.DialogHeader>
      <div class="flex flex-col gap-2">
        <Label for="author-name">{$t`Name`}</Label>
        <Input id="author-name" bind:ref={input} bind:value={name} maxlength={60} autocomplete="name" autocapitalize="words"
          onfocus={e => e.currentTarget.select()} autofocus
          aria-invalid={!!error} aria-describedby={error ? 'author-name-error' : undefined} />
        {#if error}
          <p id="author-name-error" class="text-destructive text-sm">{error}</p>
        {/if}
        {#if changing}
          <p class="text-sm text-muted-foreground">{$t`Changes you already made keep the old name.`}</p>
        {/if}
        {#if fromMenu}
          <DevContent>
            <p class="text-sm text-muted-foreground">Dev mode: users only see this menu item once they've entered a name.</p>
          </DevContent>
        {/if}
      </div>
      <Dialog.DialogFooter>
        <Button type="button" variant="secondary" onclick={() => close(false)}>{$t`Cancel`}</Button>
        <Button type="submit">{changing ? $t`Save` : $t`Continue`}</Button>
      </Dialog.DialogFooter>
    </form>
  </Dialog.DialogContent>
</Dialog.Root>
