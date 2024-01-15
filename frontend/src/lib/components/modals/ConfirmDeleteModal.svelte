<script context="module" lang="ts">
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

  export let i18nScope: I18nShapeKey<DeleteModalI18nShape>;
  let name: string;

  export async function open(_name: string, onSubmit: FormSubmitCallback<Schema>): Promise<FormModalResult<Schema>> {
    name = _name;
    return await deletionFormModal.open(onSubmit);
  }

  $: t = tTypeScoped<DeleteModalI18nShape>(i18nScope);

  const verify = z.object({
    keyphrase: z.string().refine((value) => value.match(`^${$t('enter_to_delete.value')}$`)),
  });

  type Schema = typeof verify;

  let deletionFormModal: FormModal<Schema>;
  $: deletionForm = deletionFormModal?.form();
</script>

<div class="delete-modal contents">
  <FormModal bind:this={deletionFormModal} schema={verify} let:errors>
    <span slot="title">{$t('title')}</span>
    <Input
      id="keyphrase"
      type="text"
      label={$t(
        'enter_to_delete.label',
        /*
        Compiler: https://github.com/cibernox/babel-plugin-precompile-intl/blob/1afecaa725f9d59d785666ebccc065a9c41d8b74/src/index.ts#L299
        Formatter: https://github.com/cibernox/precompile-intl-runtime/blob/cdaee6ebaa7c2e690db4dff3b8545ebaa79704fc/src/stores/formatters.ts#L44
        `name` is optional in the translation template, so its name must get `sort()`ed after `_value`
        */
        { _value: $t('enter_to_delete.value'), name }
      )}
      error={errors.keyphrase}
      bind:value={$deletionForm.keyphrase}
    />
    <svelte:fragment slot="submitText">
      {$t('submit')}
      <TrashIcon />
    </svelte:fragment>
  </FormModal>
</div>
