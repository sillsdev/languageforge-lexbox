<script lang="ts" module>
  import {gt} from 'svelte-i18n-lingui';

  const missingOptions = [
    { id: 'semanticDomains', label: gt`Semantic domain` },
    { id: 'partOfSpeech', label: gt`Part of speech` },
    { id: 'publication', label: gt`Publication` },
    { id: 'senses', label: gt`Senses` },
    { id: 'examples', label: gt`Example sentences` },
  ]

  export type MissingOption = typeof missingOptions[number];
</script>

<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {t} from 'svelte-i18n-lingui';

  let {value = $bindable()}: { value: MissingOption | null } = $props();

</script>
<Select.Root type="single" value={value?.id} onValueChange={(newId) => value = missingOptions.find(f => f.id === newId) ?? null}>
  <Select.Trigger class="flex-1" onClear={() => value = null}>
    {value?.label ? $t`Missing: ${value.label}` : $t`Entries missing...`}
  </Select.Trigger>
  <Select.Content>
    {#each missingOptions as option (option.id)}
      <Select.Item value={option.id} label={option.label}>
        {option.label}
      </Select.Item>
    {/each}
  </Select.Content>
</Select.Root>
