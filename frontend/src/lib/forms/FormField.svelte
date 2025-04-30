<script lang="ts">
  import type { Snippet } from 'svelte';
  import { onMount } from 'svelte';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';
  import { NewTabLinkMarkdown } from '$lib/components/Markdown';
  import type { HelpLink } from '$lib/components/help';
  import SupHelp from '$lib/components/help/SupHelp.svelte';

  interface Props {
    label: string;
    description?: string | undefined;
    error?: string | string[] | undefined;
    id?: string;
    helpLink?: HelpLink | undefined;
    /**
     * For login pages, EditableText, admin pages etc. auto focus is not a real accessibility problem.
     * So we allow/support it and disable a11y-autofocus warnings in generic places.
     */
    autofocus?: boolean;
    children?: Snippet;
  }

  let {
    label,
    description = undefined,
    error = undefined,
    id = randomFormId(),
    helpLink = undefined,
    autofocus = false,
    children,
  }: Props = $props();

  let elem: HTMLDivElement = $state()!;

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
  {@render children?.()}
  {#if description}
    <label for={id} class="label pb-0 underline-links">
      <span class="label-text-alt description">
        <NewTabLinkMarkdown md={description} />
      </span>
    </label>
  {/if}
  <FormFieldError {id} {error} />
</div>
