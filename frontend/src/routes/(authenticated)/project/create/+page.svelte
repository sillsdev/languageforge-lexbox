<script lang="ts">
  import { goto } from '$app/navigation';
  import { Checkbox, Form, FormError, Input, ProjectTypeSelect, Select, SubmitButton, TextArea, lexSuperForm } from '$lib/forms';
  import { CreateProjectResult, DbErrorCode, ProjectRole, ProjectType, RetentionPolicy, type CreateProjectInput } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { TitlePage } from '$lib/layout';
  import { z } from 'zod';
  import { _createProject, _projectCodeAvailable } from './+page';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { useNotifications } from '$lib/notify';
  import { Duration, deriveAsync, deriveAsyncIfDefined } from '$lib/util/time';
  import { getSearchParamValues } from '$lib/util/query-params';
  import { onMount } from 'svelte';
  import MemberBadge from '$lib/components/Badges/MemberBadge.svelte';
  import { derived, writable } from 'svelte/store';
  import { concatAll } from '$lib/util/array';
  import { browser } from '$app/environment';
  import { ProjectConfidentialityCombobox } from '$lib/components/Projects';
  import DevContent from '$lib/layout/DevContent.svelte';
  import { isDev } from '$lib/layout/DevContent.svelte';
  import { _getProjectsByLangCodeAndOrg } from './+page';
  import Markdown from 'svelte-exmarkdown';
  import { NewTabLinkRenderer } from '$lib/components/Markdown';
  import Button from '$lib/forms/Button.svelte';

  export let data;
  $: user = data.user;
  let requestingUser : typeof data.requestingUser;
  $: myOrgs = data.myOrgs ?? [];

  const { notifyWarning } = useNotifications();

  const formSchema = z.object({
    name: z.string().trim().min(1, $t('project.create.name_missing')),
    description: z.string().trim().min(1, $t('project.create.description_missing')),
    type: z.nativeEnum(ProjectType).default(ProjectType.FlEx),
    retentionPolicy: z.nativeEnum(RetentionPolicy).default(RetentionPolicy.Training),
    languageCode: z
      .string()
      .min(2, $t('project.create.language_code_too_short'))
      .regex(/^[a-z\d][a-z-\d]*$/, $t('project.create.language_code_invalid')),
    code: z
      .string()
      .toLowerCase()
      .min(4, $t('project.create.code_too_short'))
      .regex(/^[a-z\d][a-z-\d]*$/, $t('project.create.code_invalid')),
    customCode: z.boolean().default(false),
    isConfidential: z.boolean().default(false),
    orgId: z.string().trim()
  });

  //random guid
  let projectId:string = crypto.randomUUID();
  let { form, errors, message, enhance, submitting } = lexSuperForm(formSchema, async () => {
    const result = await _createProject({
      id: projectId,
      name: $form.name,
      code: $form.code,
      description: $form.description,
      type: $form.type,
      retentionPolicy: $form.retentionPolicy,
      isConfidential: $form.isConfidential,
      projectManagerId: requestingUser?.id,
      orgId: $form.orgId === '' ? null : $form.orgId,
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
      notifyWarning($t('project.create.requested', { name: $form.name }), Duration.Persistent);
      await goto('/');
    }
  });

  const asyncCodeError = writable<string | undefined>();
  const codeStore = derived(form, ($form) => $form.code);
  const codeIsAvailable = deriveAsync(codeStore, async (code) => {
    if (!browser || !code || !user.canCreateProjects) return true;
    return _projectCodeAvailable(code);
  }, true, true);
  $: $asyncCodeError = $codeIsAvailable ? undefined : $t('project.create.code_exists');
  const codeErrors = derived([errors, asyncCodeError], () => [...new Set(concatAll($errors.code, $asyncCodeError))]);

  const langCodeStore = derived(form, ($form) => $form.languageCode);
  const orgIdStore = derived(form, ($form) => $form.orgId);
  const langCodeAndOrgIdStore = derived([langCodeStore, orgIdStore], ([lang, orgId], set) => {
    if (lang && orgId && (lang.length == 2 || lang.length == 3)) {
      set({ langCode: lang, orgId: orgId });
    }
  });

  const relatedProjectsStoreStore = deriveAsyncIfDefined(langCodeAndOrgIdStore, _getProjectsByLangCodeAndOrg);
  const relatedProjects = derived(relatedProjectsStoreStore, (nestedStore, set) => {
    if (nestedStore) return nestedStore.subscribe(set); // Return the unsubscribe fn so we don't leak memory
  }, []);

  const typeCodeMap: Partial<Record<ProjectType, string | undefined>> = {
    [ProjectType.FlEx]: 'flex',
    [ProjectType.WeSay]: 'dictionary',
    [ProjectType.OneStoryEditor]: 'onestory',
    [ProjectType.OurWord]: 'ourword',
    [ProjectType.AdaptIt]: 'aikb',
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
      if (urlValues.id) projectId = urlValues.id;
      if (urlValues.name) form.name = urlValues.name;
      if (urlValues.description) form.description = urlValues.description;
      if (urlValues.type) form.type = urlValues.type;
      if (urlValues.orgId) form.orgId = urlValues.orgId;
      if ($isDev === true) {
        if (!form.orgId && !user.isAdmin && myOrgs?.[0]) {
          form.orgId = myOrgs[0].id;
        }
      }
      if (urlValues.retentionPolicy && (urlValues.retentionPolicy !== RetentionPolicy.Dev || user.isAdmin)) form.retentionPolicy = urlValues.retentionPolicy;
      if (urlValues.isConfidential === 'true') form.isConfidential = true;
      if (urlValues.code) {
        const standardCodeSuffix = buildProjectCode('', urlValues.type, urlValues.retentionPolicy);
        const isCustomCode = !urlValues.code.endsWith(standardCodeSuffix);
        if (isCustomCode && user.isAdmin) {
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

  let selectedProjectCode: string;
  let showRelatedProjects = true;

  function askToJoinProject(projectCode: string): void {
    // TODO: Implement
    console.log('Will ask to join', projectCode);
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

    <DevContent>
      <Select
        id="org"
        label={$t('project.create.org')}
        bind:value={$form.orgId}
        error={$errors.orgId}
        on:change
      >
        <option value={''} >{$t('project_page.organization.placeholder')}</option>
        {#each myOrgs as org}
          <option value={org.id}>{org.name}</option>
        {/each}
      </Select>
    </DevContent>

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
      error={$codeErrors}
      readonly={!$form.customCode}
    />

    {#if $relatedProjects?.length}
      {#if showRelatedProjects}
        <!-- Note, not using RadioButtonGroup here so we can better customize the display to the needs of this form -->
        <div
          role="radiogroup"
          aria-labelledby="label-extra-projects"
          id="group-extra-projects"
          >
          <div class="legend" id="label-extra-projects">
            {$t('project.create.maybe_related')}
          </div>
          {#each $relatedProjects as proj}
          <div class="form-control w-full">
            <label class="label cursor-pointer justify-normal pb-0">
              <input id={`extra-projects-${proj.code}`} type="radio" bind:group={selectedProjectCode} value={proj.code} class="radio mr-2" />
              <span class="label-text inline-flex items-center gap-2">
                {proj.name} ({proj.code})
              </span>
            </label>
          </div>
          {/each}
          <label for="group-extra-projects" class="label pb-0">
            <span class="label-text-alt">
              <Markdown md={$t('project.create.maybe_related_description')} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
            </span>
          </label>
        </div>

        <div class="inline-flex mt-2">
          <Button
            class="mr-2"
            variant="btn-primary"
            disabled={!selectedProjectCode}
            on:click={() => askToJoinProject(selectedProjectCode)}
          >
            Ask to joim
          </Button>
          <Button
            class="mr-2"
            variant="btn-warning"
            on:click={() => showRelatedProjects = false}
          >
            No thanks, create a new project
          </Button>
        </div>
      {:else}
        <span on:click={() => showRelatedProjects = true} class="mb-4">
          Found {$relatedProjects.length} related projects, click to see them
        </span>
      {/if}
    {/if}

    {#if !$relatedProjects?.length || !showRelatedProjects}
      <TextArea
        id="description"
        label={$t('project.create.description')}
        bind:value={$form.description}
        error={$errors.description}
      />

      <div class="mt-4 mb-2">
        <!-- It feels appropriate to give this option a bit more real estate -->
        <ProjectConfidentialityCombobox bind:value={$form.isConfidential} />
      </div>

      <FormError error={$message} />
      <SubmitButton loading={$submitting}>
          {#if data.user.canCreateProjects}
              {$t('project.create.submit')}
          {:else}
              {$t('project.create.request')}
          {/if}
      </SubmitButton>
    {/if}
  </Form>
</TitlePage>
