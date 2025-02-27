<script lang="ts">
  import { goto } from '$app/navigation';
  import { Checkbox, Form, FormError, Input, ProjectTypeSelect, Select, SubmitButton, TextArea, lexSuperForm } from '$lib/forms';
  import { CreateProjectResult, DbErrorCode, ProjectRole, ProjectType, RetentionPolicy, type CreateProjectInput } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { TitlePage } from '$lib/layout';
  import { z } from 'zod';
  import { _askToJoinProject, _createProject, _projectCodeAvailable } from './+page';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { useNotifications } from '$lib/notify';
  import { Duration, deriveAsync, deriveAsyncIfDefined } from '$lib/util/time';
  import { getSearchParamValues } from '$lib/util/query-params';
  import { onMount } from 'svelte';
  import MemberBadge from '$lib/components/Badges/MemberBadge.svelte';
  import { derived, writable, type Readable } from 'svelte/store';
  import { concatAll } from '$lib/util/array';
  import { browser } from '$app/environment';
  import { ProjectConfidentialityCombobox } from '$lib/components/Projects';
  import { _getProjectsByLangCodeAndOrg, _getProjectsByNameAndOrg } from './+page';
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import Button from '$lib/forms/Button.svelte';
  import {projectUrl} from '$lib/util/project';
  import DevContent from '$lib/layout/DevContent.svelte';

  export let data;
  $: user = data.user;
  let requestingUser : typeof data.requestingUser;
  $: myOrgs = data.myOrgs ?? [];

  const { notifySuccess, notifyWarning } = useNotifications();

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
  let forceDraft = false;

  //random guid
  let projectId:string = crypto.randomUUID();
  let { form, errors, message, enhance, submitting, tainted } = lexSuperForm(formSchema, async () => {
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
      forceDraft
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
      await goto(projectUrl($form));
    } else {
      notifyWarning($t('project.create.requested', { name: $form.name }), Duration.Persistent);
      await goto('/');
    }
  });

  const asyncCodeError = writable<string | undefined>();
  const codeStore = derived(form, f => f.code);
  const codeIsAvailable = deriveAsync(codeStore, async (code) => {
    if (!browser || !code || !user.canCreateProjects) return true;
    return _projectCodeAvailable(code);
  }, true, true);
  $: $asyncCodeError = $codeIsAvailable ? undefined : $t('project.create.code_exists');
  const codeErrors = derived([errors, asyncCodeError], () => [...new Set(concatAll($errors.code, $asyncCodeError))]);

  const projectNameStore = derived(form, f => f.name);
  const langCodeStore = derived(form, f => f.languageCode);
  const orgIdStore = derived(form, f => f.orgId);
  const langCodeAndOrgIdStore: Readable<{langCode: string, orgId: string}> = derived([langCodeStore, orgIdStore], ([langCode, orgId], set) => {
    if (langCode && orgId && (langCode.length == 2 || langCode.length == 3)) {
      set({ langCode, orgId });
    }
  });

  const projectNameAndOrgIdStore: Readable<{projectName: string, orgId: string}> = derived([projectNameStore, orgIdStore], ([projectName, orgId], set) => {
    if (projectName && orgId && projectName.length >= 3) {
      set({ projectName, orgId });
    }
  });

  const relatedProjectsByLangCode = deriveAsyncIfDefined(langCodeAndOrgIdStore, _getProjectsByLangCodeAndOrg, []);
  const relatedProjectsByName = deriveAsyncIfDefined(projectNameAndOrgIdStore, _getProjectsByNameAndOrg, []);

  const relatedProjects = derived([relatedProjectsByName, relatedProjectsByLangCode], ([byName, byCode]) => {
    // Put projects related by language code first as they're more likely to be real matches
    var uniqueByName = byName.filter(n => byCode.findIndex(c => c.id == n.id) == -1);
    return [...byCode, ...uniqueByName];
  });

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
      if (!form.orgId && !user.isAdmin && myOrgs?.[0]) {
        form.orgId = myOrgs[0].id;
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

  let selectedProject: { name: string, id: string } | undefined = undefined;
  let showRelatedProjects = true;

  // When the related-projects list changes, keep selectedProject up-to-date
  relatedProjects.subscribe(projects => {
    if (selectedProject) selectedProject = projects.find(p => selectedProject?.id === p.id);
  });

  async function askToJoinProject(projectId: string, projectName: string): Promise<void> {
    const joinResult = await _askToJoinProject(projectId);
    if (!joinResult.error) {
      notifySuccess($t('project.create.join_request_sent', { projectName }), Duration.Persistent);
      $tainted = undefined; // Prevent "are you sure you want to leave?" warning
      await goto('/');
    }
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

    {#if $relatedProjects.length}
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
              <input id={`extra-projects-${proj.code}`} type="radio" bind:group={selectedProject} value={proj} class="radio mr-2" />
              <span class="label-text inline-flex items-center gap-2">
                {proj.name} ({proj.code}) <br/>
                {proj.description ?? $t('project.create.no_description')}
              </span>
            </label>
          </div>
          {/each}
          <label for="group-extra-projects" class="label pb-0">
            <span class="label-text-alt">
              <NewTabLinkMarkdown md={$t('project.create.maybe_related_description')} />
            </span>
          </label>
        </div>

        <div class="inline-flex mt-2">
          <Button
            class="mr-2"
            variant="btn-primary"
            disabled={!selectedProject}
            on:click={() => {if (selectedProject) void askToJoinProject(selectedProject.id, selectedProject.name)}}
          >
            {$t('project.create.ask_to_join')}
          </Button>
          <Button
            class="mr-2"
            variant="btn-warning"
            on:click={() => showRelatedProjects = false}
          >
          {$t('project.create.no_thanks')}
          </Button>
        </div>
      {:else}
        <button class="btn btn-ghost btn-sm mb-4" tabindex="0" on:click={() => showRelatedProjects = true}>
          {$t('project.create.click_to_view_related_projects', {count: $relatedProjects.length})}
        </button>
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

      <DevContent>
        <Checkbox label="Force draft project creation" bind:value={forceDraft}/>
      </DevContent>
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
