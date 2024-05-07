<script lang="ts">
  import { mdiBookAlphabet, mdiClose } from "@mdi/js";
  import { getContext } from "svelte";
  import { Button, Popover, Toggle } from "svelte-ux";
  import type { Writable, Readable } from "svelte/store";

  const characters = getContext<Readable<string[] | null>>('indexExamplars');
  const selectedCharacter = getContext<Writable<string | undefined>>('selectedIndexExamplars');
</script>

<Toggle let:on={open} let:toggle let:toggleOff>
  <Popover {open} on:close={toggleOff} placement="bottom-start" offset={4} padding={6}>
    <div class="flex flex-row flex-wrap justify-evenly gap-2 p-3 rounded bg-surface-100 border shadow-lg overflow-auto max-w-56">
      {#each $characters ?? [] as character}
        <button class="contents" on:mouseup={() => {
            if ($selectedCharacter === character) $selectedCharacter = undefined;
            else $selectedCharacter = character;
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
      {#if $selectedCharacter}
        <button class="contents" on:mouseup={() => $selectedCharacter = undefined}>
          <Button
            fullWidth
            icon={mdiClose}
            class="border mt-2"
            size="sm">
            Clear
          </Button>
        </button>
      {/if}
    </div>
  </Popover>
  <Button icon={$selectedCharacter ? undefined : mdiBookAlphabet} variant={$selectedCharacter ? 'fill-outline' : open ? 'outline' : undefined} iconOnly
    class="border h-[37.6px] p-1.5 aspect-square"
    classes={{icon: 'text-[0.9em]'}}
    rounded on:click={toggle}>
    {#if $selectedCharacter}
      {$selectedCharacter}
    {/if}
  </Button>
</Toggle>
