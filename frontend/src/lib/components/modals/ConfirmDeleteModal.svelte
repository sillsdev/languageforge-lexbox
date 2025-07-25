<script module lang="ts">
  export type DeleteModalI18nShape = {
    title: string;
    submit: string;
    // eslint-disable-next-line @typescript-eslint/naming-convention
    enter_to_delete: {
      label: string;
      value: string;
    };
  };
</script>

<script lang="ts">
  import { Input } from '$lib/forms';
  import { tTypeScoped, type I18nShapeKey } from '$lib/i18n';
  import { z } from 'zod';
  import { FormModal } from '$lib/components/modals';
  import type { FormModalResult, FormSubmitCallback } from '$lib/components/modals/FormModal.svelte';
  import { TrashIcon } from '$lib/icons';
  import tt from '$lib/i18n';

  interface Props {
    i18nScope: I18nShapeKey<DeleteModalI18nShape>;
  }

  const { i18nScope }: Props = $props();
  let name: string | undefined = $state();

  export async function open(_name: string, onSubmit: FormSubmitCallback<Schema>): Promise<FormModalResult<Schema>> {
    name = _name;
    return await deletionFormModal!.open(onSubmit);
  }

  let t = $derived(tTypeScoped<DeleteModalI18nShape>(i18nScope));

  const verify = z.object({
    keyphrase: z
      .string()
      .refine((value) => value.match(`^${$t('enter_to_delete.value')}$`), $tt('form.value_is_incorrect')),
  });

  type Schema = typeof verify;

  // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
  let deletionFormModal: FormModal<Schema> | undefined = $state();
  let deletionForm = $derived(deletionFormModal?.form());
</script>

<div class="contents">
  <FormModal bind:this={deletionFormModal} schema={verify} submitVariant="btn-error">
    {#snippet title()}
      <span>{$t('title')}</span>
    {/snippet}
    {#snippet children({ errors })}
      <Input
        id="keyphrase"
        type="text"
        autofocus
        label={$t(
          'enter_to_delete.label',
          /*
          Compiler: https://github.com/cibernox/babel-plugin-precompile-intl/blob/1afecaa725f9d59d785666ebccc065a9c41d8b74/src/index.ts#L299
          Formatter: https://github.com/cibernox/precompile-intl-runtime/blob/cdaee6ebaa7c2e690db4dff3b8545ebaa79704fc/src/stores/formatters.ts#L44
          `name` is optional in the translation template, so its name must get `sort()`ed after `_value`
          */
          { _value: $t('enter_to_delete.value'), name: name ?? '' },
        )}
        error={errors.keyphrase}
        bind:value={$deletionForm!.keyphrase}
      />
    {/snippet}
    {#snippet submitText()}
      {$t('submit')}
      <TrashIcon />
    {/snippet}
  </FormModal>
</div>
