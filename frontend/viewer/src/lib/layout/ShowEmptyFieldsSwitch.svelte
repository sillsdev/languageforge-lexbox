<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Switch} from '$lib/components/ui/switch';

  interface Props {
    value: boolean;
  }

  let {value = $bindable()}: Props = $props();
</script>

<label class="flex gap-2 items-center text-sm">
  <Switch bind:checked={value}/>
  {$t`Show empty fields`}
</label>
