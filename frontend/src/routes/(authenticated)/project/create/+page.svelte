<script lang="ts">
  import { goto } from '$app/navigation';
  import { FormError, lexSuperForm, SubmitButton, TextArea } from '$lib/forms';
  import Checkbox from '$lib/forms/Checkbox.svelte';
  import Form from '$lib/forms/Form.svelte';
  import Input from '$lib/forms/Input.svelte';
  import Select from '$lib/forms/Select.svelte';
  import { CreateProjectResult, DbErrorCode, ProjectType, RetentionPolicy } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { z } from 'zod';
  import { _createProject } from './+page';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { notifySuccess } from '$lib/notify';
  import { Duration } from '$lib/util/time';
  import { ProjectTypeIcon } from '$lib/components/ProjectType';

  export let data;
  const formSchema = z.object({
    name: z.string().min(1, $t('project.create.name_missing')),
    description: z.string().min(1, $t('project.create.description_missing')),
    type: z.nativeEnum(ProjectType).default(ProjectType.FlEx),
    retentionPolicy: z.nativeEnum(RetentionPolicy).default(RetentionPolicy.Training),
    languageCode: z
      .string()
      .min(3, $t('project.create.language_code_too_short'))
      .regex(/^[a-z-\d]+$/, $t('project.create.language_code_invalid')),
    code: z.string().toLowerCase().min(4, $t('project.create.code_too_short')),
    customCode: z.boolean().default(false),
  });
  //random guid
  const projectId = crypto.randomUUID();
  let { form, errors, message, enhance, submitting } = lexSuperForm(formSchema, async () => {
    const result = await _createProject({
      id: projectId,
      name: $form.name,
      code: $form.code,
      description: $form.description,
      type: $form.type,
      retentionPolicy: $form.retentionPolicy,
    });
    if (result.error) {
      if (result.error.forCode(DbErrorCode.Duplicate)) {
        $errors.code = [$t('project.create.code_exists')];
      } else {
        $message = result.error.message;
      }

      return;
    }
    if (result.data?.createProject.createProjectResponse?.result == CreateProjectResult.Created) {
      await goto(`/project/${$form.code}`);
    } else {
      notifySuccess($t('project.create.requested', { name: $form.name }), Duration.Long);
      await goto('/');
    }
  });
  const typeCodeMap: Partial<Record<ProjectType, string | undefined>> = {
    [ProjectType.FlEx]: 'flex',
    [ProjectType.WeSay]: 'dictionary',
    [ProjectType.OneStoryEditor]: 'onestory',
    [ProjectType.OurWord]: 'ourword',
  };

  const policyCodeMap: Partial<Record<RetentionPolicy, string | undefined>> = {
    [RetentionPolicy.Test]: 'test',
    [RetentionPolicy.Dev]: 'dev',
    [RetentionPolicy.Training]: 'train',
  };

  $: if (!$form.customCode) {
    let typeCode = typeCodeMap[$form.type] ?? 'misc';
    let policyCode = policyCodeMap[$form.retentionPolicy] ?? '';
    if (policyCode) policyCode = `-${policyCode}`;
    form.update(
      (form) => {
        form.code = `${form.languageCode}${policyCode}-${typeCode}`;
        return form;
      },
      { taint: false }
    );
  }
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('project.create.title')}
  </svelte:fragment>

  <Form {enhance}>
    <Input
      label={$t('project.create.name')}
      description={$t('project.create.name_description')}
      bind:value={$form.name}
      error={$errors.name}
      autofocus
    />
    <TextArea
      id="description"
      label={$t('project.create.description')}
      bind:value={$form.description}
      error={$errors.description}
    />
    <div class="relative">
      <Select id="type" label={$t('project.create.type')} bind:value={$form.type} error={$errors.type}>
        <option value={ProjectType.FlEx}>{$t('project_type.flex')}</option>
        <option value={ProjectType.WeSay}>{$t('project_type.weSay')}</option>
        <option value={ProjectType.OneStoryEditor}>{$t('project_type.oneStoryEditor')}</option>
        <option value={ProjectType.OurWord}>{$t('project_type.ourWord')}</option>
      </Select>
      <span class="absolute right-10 bottom-4 pointer-events-none">
        <ProjectTypeIcon type={$form.type} size="h-8" />
      </span>
    </div>

    <Select
      id="policy"
      label={$t('project.create.retention_policy')}
      bind:value={$form.retentionPolicy}
      error={$errors.retentionPolicy}
    >
      <option value={RetentionPolicy.Verified}>{$t('retention_policy.language_project')}</option>
      <option value={RetentionPolicy.Training}>{$t('retention_policy.training')}</option>
      <option value={RetentionPolicy.Test}>{$t('retention_policy.test')}</option>
      <AdminContent>
        <option value={RetentionPolicy.Dev}>{$t('retention_policy.dev')}</option>
      </AdminContent>
    </Select>

    <Input
      label={$t('project.create.language_code')}
      description={$t('project.create.language_code_description')}
      bind:value={$form.languageCode}
      error={$errors.languageCode}
    />
    <AdminContent>
      <Checkbox label={$t('project.create.custom_code')} bind:value={$form.customCode} />
    </AdminContent>
    <Input
      label={$t('project.create.code')}
      bind:value={$form.code}
      error={$errors.code}
      readonly={!$form.customCode}
    />
    <FormError error={$message} />
    <SubmitButton loading={$submitting}>
        {#if data.user.canCreateProject || data.user.role === 'admin'}
            {$t('project.create.submit')}
        {:else}
            {$t('project.create.request')}
        {/if}
    </SubmitButton>
  </Form>
</Page>
