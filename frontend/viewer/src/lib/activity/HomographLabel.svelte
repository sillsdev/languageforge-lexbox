<script lang="ts">
  import HomographNumber from '$lib/components/dictionary/HomographNumber.svelte';
  import {splitHomographSubscript} from '$lib/components/dictionary/homograph-subscript';

  let {text}: {text: string} = $props();

  const label = $derived(splitHomographSubscript(text));
</script>

<!-- Activity labels from the backend carry a headword's homograph number as a Unicode subscript (plain text, so it also works in titles); render it like HomographNumber -->
{label.text}<HomographNumber homographNumber={label.homographNumber} />
