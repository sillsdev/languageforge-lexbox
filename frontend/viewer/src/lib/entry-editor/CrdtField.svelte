<script lang="ts">
  import {
    mdiCloudAlertOutline,
    mdiCloudDownloadOutline,
    mdiContentSaveCheckOutline,
  } from "@mdi/js";
  import { createEventDispatcher, getContext } from "svelte";
  import { Button, Notification, Popover, Tooltip, portal } from "svelte-ux";
  import type { Readable } from "svelte/store";

  const demoValues = getContext("demoValues") as Readable<{
    generateExternalChanges: boolean,
  }>;

  // For demo purposes
  let i = 1;
  let timeout: number;
  $: {
    clearTimeout(timeout);
    if (unsavedChanges && $demoValues.generateExternalChanges) {
      timeout = setTimeout(() => {
        if (unsavedChanges && !unacceptedChanges) value = (value ?? "") + i++;
      }, 1000);
    }
  }

  const dispatch = createEventDispatcher<{
    change: { value: string };
  }>();

  export let value: string;
  export let viewMergeButtonPortal: HTMLElement;
  let editorValue = value;

  export let unsavedChanges = false;
  let unacceptedChanges = false;
  let viewingMerge = true;

  $: {
    value;
    onExternalChange();
  }

  function onExternalChange() {
    if (unsavedChanges) {
      unacceptedChanges = true;
      viewingMerge = true;
    } else {
      editorValue = value;
    }
  }

  function acceptChanges() {
    editorValue = value;
    unsavedChanges = false;
    unacceptedChanges = false;
  }

  function saveChanges() {
    value = editorValue;
    unsavedChanges = false;
    unacceptedChanges = false;
    dispatch("change", { value });
  }

  function onEditorValueChange(newValue: string | number) {
    editorValue = String(newValue);
    unsavedChanges = editorValue !== value;
  }

  function softSave() {
    if (!unacceptedChanges) {
      saveChanges();
    }
  }
</script>

<div
  class="ws-field-wrapper relative"
  class:unresolved-merge={unacceptedChanges}
>
  {#if unsavedChanges}
    <Tooltip title="Unsaved changes" delay={0} placement="top" offset={4}>
      <span
        class="absolute -left-5 top-1/2 -translate-y-1/2 bg-blue-700 rounded-full h-2.5 w-2.5"
      ></span>
    </Tooltip>
  {/if}
  {#if unacceptedChanges}
    <Popover matchWidth bind:open={viewingMerge} offset={4}>
      <Notification actions="split">
        <div slot="title">A different user changed this field</div>
        <div slot="description" class="flex flex-col gap-1">
          <div class="flex flex-col">
            <span class="font-bold text-warning">Their version:</span>
            <span class="theirs">{value}</span>
          </div>
          <div class="flex flex-col">
            <span class="font-bold text-success">Your version:</span>
            <span class="yours">{editorValue}</span>
          </div>
        </div>
        <div slot="actions" class="h-full">
          <div class="grid border-l divide-y h-full">
            <Button
              on:click={acceptChanges}
              icon={mdiCloudDownloadOutline}
              class="text-warning"
              >Accept <span class="underline">their</span> changes</Button
            >
            <Button
              on:click={saveChanges}
              icon={mdiContentSaveCheckOutline}
              class="text-success"
              >Keep <span class="underline">your</span> changes</Button
            >
          </div>
        </div>
      </Notification>
    </Popover>
  {/if}
  <slot save={softSave} {onEditorValueChange} {editorValue} />
  {#if unacceptedChanges}
    <div class="hidden">
      <div class="contents" use:portal={{ target: viewMergeButtonPortal }}>
        <Tooltip title={`A different user has made changes. Click to view.`}>
          <Button
            on:click={() => (viewingMerge = !viewingMerge)}
            icon={mdiCloudAlertOutline}
            class="p-1 text-warning"
          />
        </Tooltip>
      </div>
    </div>
  {/if}
</div>
