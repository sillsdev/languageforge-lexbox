<script lang="ts">
  import { onMount } from 'svelte';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import type { HelpLink } from '$lib/components/help';
  import SupHelp from '$lib/components/help/SupHelp.svelte';

  export let label: string;
  export let description: string | undefined = undefined;
  export let error: string | string[] | undefined = undefined;
  export let id: string = randomFormId();
  export let helpLink: HelpLink | undefined = undefined;
  /**
   * For login pages, EditableText, admin pages etc. auto focus is not a real accessibility problem.
   * So we allow/support it and disable a11y-autofocus warnings in generic places.
   */
  export let autofocus = false;

  let elem: HTMLDivElement;

  onMount(autofocusIfRequested);

  function autofocusIfRequested(): void {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    autofocus && elem.querySelector<HTMLElement>('input, select, textarea')?.focus();
  }
</script>

<div class="form-control w-full" bind:this={elem}>
  <label for={id} class="label">
    <span class="label-text">
      {label}
      {#if helpLink}
        <SupHelp {helpLink} />
      {/if}
    </span>
  </label>
  <slot />
  {#if description}
    <label for={id} class="label pb-0 underline-links">
      <span class="label-text-alt description">
        <NewTabLinkMarkdown md={description} />
      </span>
    </label>
  {/if}
  <FormFieldError {id} {error} />
</div>
