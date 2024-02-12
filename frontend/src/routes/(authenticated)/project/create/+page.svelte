<script lang="ts">
  import { goto } from '$app/navigation';
  import { Checkbox, Form, FormError, Input, ProjectTypeSelect, Select, SubmitButton, TextArea, debouncedRefine, lexSuperForm } from '$lib/forms';
  import { CreateProjectResult, DbErrorCode, ProjectRole, ProjectType, RetentionPolicy, type CreateProjectInput } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { TitlePage } from '$lib/layout';
  import { z } from 'zod';
  import { _createProject, _projectCodeAvailable } from './+page';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { useNotifications } from '$lib/notify';
  import { Duration } from '$lib/util/time';
  import { getSearchParamValues } from '$lib/util/query-params';
  import { isAdmin } from '$lib/user';
  import { onMount } from 'svelte';
  import MemberBadge from '$lib/components/Badges/MemberBadge.svelte';

  export let data;
  $: user = data.user;
  let requestingUser : typeof data.requestingUser;

  const { notifySuccess } = useNotifications();

  let codeValidation: z.ZodType = z.string().toLowerCase()
    .min(4, $t('project.create.code_too_short'))
    .refine(debouncedRefine((code) =>
      // user is not available when defining the schema
      // || isAdmin will be redundant soon after new JWTs roll out
      user.canCreateProjects ? _projectCodeAvailable(code) : Promise.resolve(true)),
      $t('project.create.code_exists'));

  const formSchema = z.object({
    name: z.string().min(1, $t('project.create.name_missing')),
    description: z.string().min(1, $t('project.create.description_missing')),
    type: z.nativeEnum(ProjectType).default(ProjectType.FlEx),
    retentionPolicy: z.nativeEnum(RetentionPolicy).default(RetentionPolicy.Training),
    languageCode: z
      .string()
      .min(3, $t('project.create.language_code_too_short'))
      .regex(/^[a-z-\d]+$/, $t('project.create.language_code_invalid')),
    code: codeValidation,
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
      projectManagerId: requestingUser?.id,
    });
    if (result.error) {
      if (result.error.byCode(DbErrorCode.Duplicate)) {
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
    [RetentionPolicy.Verified]: '',
  };

  function buildProjectCode(languageCode: string, type?: ProjectType, retentionPolicy?: RetentionPolicy): string {
    let typeCode = typeCodeMap[type ?? $form.type] ?? 'misc';
    let policyCode = policyCodeMap[retentionPolicy ?? $form.retentionPolicy];
    if (policyCode) policyCode = `-${policyCode}`;

    return `${languageCode ?? $form.languageCode}${policyCode}-${typeCode}`;
  }

  onMount(() => { // we want to do this once after the user has been set
    requestingUser = data.requestingUser;
    const urlValues = getSearchParamValues<CreateProjectInput>();
    form.update((form) => {
      if (urlValues.name) form.name = urlValues.name;
      if (urlValues.description) form.description = urlValues.description;
      if (urlValues.type) form.type = urlValues.type;
      if (urlValues.retentionPolicy && (urlValues.retentionPolicy !== RetentionPolicy.Dev || isAdmin(user))) form.retentionPolicy = urlValues.retentionPolicy;
      if (urlValues.code) {
        const standardCodeSuffix = buildProjectCode('', urlValues.type, urlValues.retentionPolicy);
        const isCustomCode = !urlValues.code.endsWith(standardCodeSuffix);
        if (isCustomCode && isAdmin(user)) {
          form.customCode = true;
          form.code = form.languageCode = urlValues.code;
        } else {
          form.languageCode = urlValues.code.replace(new RegExp(`${standardCodeSuffix}$`), '');
        }
      }
      return form;
    }, { taint: false });
  });

  $: if (!$form.customCode) {
    const type = $form.type;
    const retentionPolicy = $form.retentionPolicy;
    const languageCode = $form.languageCode;
    form.update(
      (form) => {
        form.code = buildProjectCode(languageCode, type, retentionPolicy);
        return form;
      },
      { taint: false }
    );
  }
</script>

<TitlePage title={$t('project.create.title')}>
  <Form {enhance}>
    <Input
      label={$t('project.create.name')}
      description={$t('project.create.name_description')}
      bind:value={$form.name}
      error={$errors.name}
      autofocus
    />

    <ProjectTypeSelect bind:value={$form.type} error={$errors.type} />

    <AdminContent>
      <div class="form-control">
        <div class="label">
          <span class="label-text">{$t('project_page.members.title')}</span>
        </div>
        {#if requestingUser}
          <MemberBadge canManage member={{...requestingUser, role: ProjectRole.Manager}} type="new" on:action={() => requestingUser = undefined} />
        {:else}
          <span class="text-secondary mx-2 my-1">{$t('common.none')}</span>
        {/if}
      </div>
    </AdminContent>

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

    <TextArea
      id="description"
      label={$t('project.create.description')}
      bind:value={$form.description}
      error={$errors.description}
    />

    <FormError error={$message} />
    <SubmitButton loading={$submitting}>
        {#if data.user.canCreateProjects}
            {$t('project.create.submit')}
        {:else}
            {$t('project.create.request')}
        {/if}
    </SubmitButton>
  </Form>
</TitlePage>
