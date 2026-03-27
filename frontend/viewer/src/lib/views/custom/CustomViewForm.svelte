<script lang="ts">
  import {watch} from 'runed';
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import {Button} from '$lib/components/ui/button';
  import {Input} from '$lib/components/ui/input';
  import {t} from 'svelte-i18n-lingui';

  import {FW_CLASSIC_VIEW, FW_LITE_VIEW} from '../view-data';
  import {ViewBase, type ICustomView} from '$lib/dotnet-types';
  import {validateForm} from './validation';
  import {randomId} from '$lib/utils';

  import CustomViewEntityFields from './CustomViewEntityFields.svelte';
  import CustomViewWritingSystems from './CustomViewWritingSystems.svelte';
  import {toApiViewFields, useViewService} from '../view-service.svelte';
  import {pt} from '../view-text';

  interface Props {
    value?: ICustomView | null;
    submitLabel: string;
    onSubmit: (result: ICustomView) => void | Promise<void>;
    onCancel: () => void;
  }

  const viewService = useViewService();

  let {value: editingValue, submitLabel, onSubmit, onCancel}: Props = $props();

  // svelte-ignore state_referenced_locally
  let fieldSelectionDirty = $state(!!editingValue);
  // svelte-ignore state_referenced_locally
  const value = $state(editingValue ?? defaultCustomView());

  function defaultCustomView(base: ViewBase = ViewBase.FwLite): ICustomView {
    const root = base === ViewBase.FieldWorks ? FW_CLASSIC_VIEW : FW_LITE_VIEW;
    return {
      id: randomId(),
      name: '',
      base: root.base,
      entryFields: toApiViewFields(root.entryFields),
      senseFields: toApiViewFields(root.senseFields),
      exampleFields: toApiViewFields(root.exampleFields),
    };
  }

  let saving = $state(false);
  let error = $state<string | undefined>(undefined);

  watch(
    () => value.base,
    () => {
      if (!fieldSelectionDirty && value) {
        const baseView = defaultCustomView(value.base);
        value.entryFields = baseView.entryFields;
        value.senseFields = baseView.senseFields;
        value.exampleFields = baseView.exampleFields;
      }
    },
  );

  async function submit() {
    error = undefined;
    const errorViewText = validateForm(value);
    if (errorViewText) {
      error = $t(pt(errorViewText, viewService.currentView));
      return;
    }

    saving = true;
    try {
      value.name = value.name.trim();
      await onSubmit(value);
    } finally {
      saving = false;
    }
  }
</script>

{#if value}
  <form
    class="flex flex-col gap-4"
    onsubmit={(e) => {
      e.preventDefault();
      void submit();
    }}
  >
    <div class="flex flex-col gap-2">
      <div class="text-sm font-medium">{$t`Name`}</div>
      <Input bind:value={value.name} placeholder={$t`My custom view...`} />
    </div>

    <div class="flex flex-col gap-2">
      <div class="text-sm font-medium">{$t`Based on`}</div>
      <RadioGroup.Root bind:value={value.base}>
        <RadioGroup.Item value={ViewBase.FwLite} label={FW_LITE_VIEW.name} />
        <RadioGroup.Item value={ViewBase.FieldWorks} label={FW_CLASSIC_VIEW.name} />
      </RadioGroup.Root>
    </div>

    <div class="flex flex-col gap-2">
      <div class="text-sm font-medium">{$t`Fields`}</div>
      <div class="grid gap-3 md:grid-cols-3">
        <CustomViewEntityFields
          entityType="entry"
          baseView={value.base}
          bind:items={value.entryFields}
          onchange={() => (fieldSelectionDirty = true)}
        />
        <CustomViewEntityFields
          entityType="sense"
          baseView={value.base}
          bind:items={value.senseFields}
          onchange={() => (fieldSelectionDirty = true)}
        />
        <CustomViewEntityFields
          entityType="example"
          baseView={value.base}
          bind:items={value.exampleFields}
          onchange={() => (fieldSelectionDirty = true)}
        />
      </div>
    </div>

    <div class="flex flex-col gap-2">
      <div class="text-sm font-medium">{$t`Writing systems`}</div>
      <div class="grid gap-3 md:grid-cols-2">
        <CustomViewWritingSystems kind="vernacular" bind:selection={value.vernacular} />
        <CustomViewWritingSystems kind="analysis" bind:selection={value.analysis} />
      </div>
    </div>

    {#if error}
      <div class="rounded-md border border-destructive/40 bg-destructive/5 p-2">
        <p class="text-sm text-destructive">{error}</p>
      </div>
    {/if}

    <div class="flex justify-end gap-2">
      <Button type="button" variant="outline" onclick={onCancel} disabled={saving}>{$t`Cancel`}</Button>
      <Button type="submit" loading={saving}>{submitLabel}</Button>
    </div>
  </form>
{/if}
