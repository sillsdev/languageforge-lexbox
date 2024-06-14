<script lang="ts">
import t from '$lib/i18n';
import {TitlePage} from '$lib/layout';
import {Form, FormError, Input, lexSuperForm, SubmitButton} from '$lib/forms';
import {z} from 'zod';
import {goto} from '$app/navigation';
import {_createOrg} from './+page';

const formSchema = z.object({
  name: z.string().trim().min(1, $t('org.create.name_missing')),
});
let {form, errors, message, enhance, submitting} = lexSuperForm(formSchema, async () => {
  const result = await _createOrg({
    name: $form.name,
  });
  if (result.error) {
    $message = result.error.message;
  } else {
    await goto(`/org/${result.data?.createOrganization.organization?.id}`);
  }
});
</script>

<TitlePage title={$t('org.create.title')}>
  <Form {enhance}>
    <Input
      label={$t('org.create.name')}
      bind:value={$form.name}
      error={$errors.name}
      autofocus
    />

    <FormError error={$message}/>
    <SubmitButton loading={$submitting}>
        {$t('org.create.submit')}
    </SubmitButton>
  </Form>
</TitlePage>
