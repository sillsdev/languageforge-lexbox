<script lang="ts">
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {usePartsOfSpeech} from '$lib/parts-of-speech.svelte';
  import type {IPartOfSpeech} from '$lib/dotnet-types';
  import {Select} from '$lib/components/field-editors';
  import { t } from 'svelte-i18n-lingui';

  let {value = $bindable()}: { value?: IPartOfSpeech } = $props();

  const partsOfSpeech = usePartsOfSpeech();
  const writingSystemService = useWritingSystemService();
</script>

<Select
  bind:value
  options={partsOfSpeech.current}
  labelSelector={(pos) => writingSystemService.pickBestAlternative(pos.name, 'analysis')}
  placeholder={$t`Any part of speech`}
  clearable
  idSelector="id" />
