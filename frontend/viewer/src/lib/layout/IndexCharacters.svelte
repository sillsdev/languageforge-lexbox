<script lang="ts">
  import { mdiBookAlphabet, mdiClose } from '@mdi/js';
  import { getContext } from 'svelte';
  import { Button, Popover, Toggle } from 'svelte-ux';
  import type { Writable, Readable } from 'svelte/store';

  const characters = getContext<Readable<string[] | null>>('indexExamplars');
  const selectedCharacter = getContext<Writable<string | undefined>>('selectedIndexExamplar');
</script>

{#if $characters?.length}
  <Toggle let:on={open} let:toggle let:toggleOff>
    <Popover {open} let:close on:close={toggleOff} placement="bottom-start" offset={4} padding={6}>
      <div class="index-container flex flex-row flex-wrap justify-evenly gap-2 p-3 rounded bg-surface-100 border shadow-lg overflow-auto">
        {#if $selectedCharacter}
          <!-- for some reason the web-component only gets clicks this way :( -->
          <button class="contents" on:mouseup={() => {
            $selectedCharacter = undefined;
            close();
          }}>
            <Button
              fullWidth
              icon={mdiClose}
              class="border mb-2"
              size="sm">
              Clear
            </Button>
          </button>
        {/if}
        {#each $characters ?? [] as character}
          <!-- for some reason the web-component only gets clicks this way :( -->
          <button class="contents" on:mouseup={() => {
              if ($selectedCharacter === character) $selectedCharacter = undefined;
              else $selectedCharacter = character;
              close();
            }}>
            <Button
              class="aspect-square w-8"
              variant={$selectedCharacter === character ? 'fill' : 'fill-light'}
              rounded="full"
              size="sm">
              {character}
            </Button>
          </button>
        {/each}
      </div>
    </Popover>
    <Button icon={$selectedCharacter ? undefined : mdiBookAlphabet} variant={$selectedCharacter ? 'fill-outline' : open ? 'outline' : undefined} iconOnly
      class="border text-field-sibling-button"
      rounded on:click={toggle}>
      {#if $selectedCharacter}
        {$selectedCharacter}
      {/if}
    </Button>
  </Toggle>
{/if}

<style lang="postcss">
  .index-container {
    max-width: min(24rem, 100vw - 2rem);
    max-height: 75vh;
  }
</style>
