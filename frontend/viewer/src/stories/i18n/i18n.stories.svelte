<script module lang="ts">
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import LocalizationPicker from '$lib/i18n/LocalizationPicker.svelte';
  import {formatDate, FormatDate, formatNumber} from '$lib/components/ui/format';
  import {SvelteDate} from 'svelte/reactivity';
  import {T} from 'svelte-i18n-lingui';

  const { Story } = defineMeta({});

  let currentDate = new SvelteDate();
</script>

<Story name="Formatters">
  {#snippet template()}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">Date component:</div>
      <div><FormatDate date={currentDate} options={{timeStyle: 'medium'}}/></div>
      <div class="font-medium">Date function:</div>
      <div>{formatDate(currentDate, {timeStyle: 'medium'})}</div>
      <div class="font-medium">Number function:</div>
      <div>{formatNumber(currentDate.getTime())}</div>
    </div>
  {/snippet}
</Story>

<Story name="Component interpolation">
  {#snippet template()}
    <T msg="This date # and this emoji # are snippets">
      <FormatDate date={currentDate} options={{timeStyle: 'short', dateStyle: 'short'}}/>
      {#snippet second()}
        🤠
      {/snippet}
    </T>
  {/snippet}
</Story>
