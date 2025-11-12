<script lang="ts">
  import {usePartsOfSpeech, useWritingSystemService} from '$project/data';
  import type {IPartOfSpeech} from '$lib/dotnet-types';
  import {Select} from '$lib/components/field-editors';
  import { t } from 'svelte-i18n-lingui';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';

  let {value = $bindable()}: { value?: IPartOfSpeech } = $props();

  const partsOfSpeech = usePartsOfSpeech();
  const writingSystemService = useWritingSystemService();
  const currentView = useCurrentView();
</script>

<Select
  bind:value
  options={partsOfSpeech.current}
  labelSelector={(pos) => writingSystemService.pickBestAlternative(pos.name, 'analysis')}
  placeholder={pt($t`Any grammatical info.`, $t`Any part of speech`, $currentView)}
  clearable
  idSelector="id" />
