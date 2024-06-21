<script lang="ts">
  import { mdiCheck, mdiContentSaveCheckOutline, mdiSync } from "@mdi/js";
  import { getContext } from "svelte";
  import { Button, DurationUnits, Icon, Popover, Toggle, humanizeDuration } from "svelte-ux";
  import type { SaveEvent, SaveEventEmmiter } from "../services/save-event-service";
  import type { UnionToIntersection } from "type-fest";

  const onSave = getContext<SaveEventEmmiter>('saveEvents');

  let saving = false;
  let savingShownLongEnough = true;
  let savingShownLongEnoughTimeout: ReturnType<typeof setTimeout> | undefined = undefined;

  let justSaved = false;
  let lastSaved: Date | undefined = undefined;
  let showSavedTimeout: ReturnType<typeof setTimeout>;

  let lastStatus: UnionToIntersection<SaveEvent>['status'];
  let statusIsFresh = false;
  let statusIsFreshTimeout: ReturnType<typeof setTimeout>;

  $: showSaving = saving || !savingShownLongEnough;
  $: showSaved = !showSaving && justSaved;
  $: showStatus = !showSaved && statusIsFresh;

  $: statusIcon = showSaving ? mdiSync : mdiContentSaveCheckOutline;

  function reset() {
    saving = false;
    savingShownLongEnough = true;
    justSaved = false;
    statusIsFresh = false;
  }

  $: {
    if ($onSave) {
      if ('status' in $onSave) {
        reset();
        lastStatus = $onSave.status;
        statusIsFresh = true;
        clearTimeout(statusIsFreshTimeout);
        statusIsFreshTimeout = setTimeout(() => {
          statusIsFresh = false;
        }, 3000);
      }

      if ('saved' in $onSave) {
        saving = false;
        justSaved = true;
        lastSaved = new Date();
        clearTimeout(showSavedTimeout);
        showSavedTimeout = setTimeout(() => {
          justSaved = false;
        }, 3000);
      }

      if ('saving' in $onSave) {
        saving = true;
        if (!savingShownLongEnoughTimeout) {
          savingShownLongEnough = false;
          savingShownLongEnoughTimeout = setTimeout(() => {
            savingShownLongEnough = true;
            savingShownLongEnoughTimeout = undefined;
          }, 1000)
        }
      }
    }
  }
</script>

<Toggle let:on={open} let:toggle let:toggleOff>
  <div class="col-start-2 text-right">
    <div class="inline-block">
      <Popover {open} on:close={toggleOff} placement="bottom-start">
        <div class="px-4 py-2 bg-surface-200 text-sm text-surface-content border border-info shadow-md rounded">
          {#if lastSaved}
            {@const savedAgo = Date.now() - lastSaved.getTime()}
            Last saved
            {#if savedAgo < 30_000}
              a few seconds
            {:else if savedAgo < 90_000}
              a minute
            {:else}
              {humanizeDuration({ duration: { milliseconds: savedAgo }, minUnits: DurationUnits.Minute })}
            {/if}
            ago
          {:else if lastStatus === 'saved-to-disk'}
            <span class="inline-flex items-center gap-2">
              Saved to disk
              <Icon data={mdiCheck} size="1em" />
            </span>
          {:else if lastStatus === 'failed-to-save'}
            Save failed
          {/if}
        </div>
      </Popover>
      <Button class="font-normal" color="info" iconOnly rounded="full" icon={statusIcon} on:click={toggle}>
        <span class="max-md:hidden contents">
          {#if showStatus}
            {#if lastStatus === 'saved-to-disk'}
              <span class="text-sm">Project saved to disk</span>
            {:else if lastStatus === 'failed-to-save'}
              <span class="text-sm text-danger">Save failed</span>
            {/if}
          {:else if showSaved}
            <span class="text-sm">
              Just saved
            </span>
            <Icon data={mdiCheck} size="1em" />
          {:else if showSaving}
            <span class="text-sm">Saving...</span>
          {/if}
        </span>
      </Button>
    </div>
  </div>
</Toggle>
