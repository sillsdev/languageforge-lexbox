<script lang="ts">
  import {goto} from '$app/navigation';
  import {
    Checkbox,
    Form,
    FormError,
    Input,
    ProjectTypeSelect,
    Select,
    SubmitButton,
    TextArea,
    lexSuperForm,
  } from '$lib/forms';
  import {
    CreateProjectResult,
    DbErrorCode,
    ProjectRole,
    ProjectType,
    RetentionPolicy,
    type CreateProjectInput,
  } from '$lib/gql/types';
  import t from '$lib/i18n';
  import {TitlePage} from '$lib/layout';
  import {z} from 'zod';
  import {_askToJoinProject, _createProject, _projectCodeAvailable} from './+page';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import {useNotifications} from '$lib/notify';
  import {Duration, DEFAULT_DEBOUNCE_TIME} from '$lib/util/time';
  import {getSearchParamValues} from '$lib/util/query-params';
  import {onMount} from 'svelte';
  import MemberBadge from '$lib/components/Badges/MemberBadge.svelte';
  import {concatAll} from '$lib/util/array';
  import {browser} from '$app/environment';
  import {ProjectConfidentialityCombobox} from '$lib/components/Projects';
  import {_getProjectsByLangCodeAndOrg, _getProjectsByNameAndOrg} from './+page';
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import Button from '$lib/forms/Button.svelte';
  import {projectUrl} from '$lib/util/project';
  import DevContent from '$lib/layout/DevContent.svelte';
  import {resource, watch} from 'runed';

  const { data } = $props();
  let user = $derived(data.user);
  let requestingUser: typeof data.requestingUser = $state();
  let myOrgs = $derived(data.myOrgs ?? []);
  let projectStatus = $derived(data.projectStatus);

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
    orgId: z.string().trim(),
  });
  let forceDraft = $state(false);

  //random guid
  let projectId: string = crypto.randomUUID();
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
      forceDraft,
    });
    if (result.error) {
      if (result.error.byCode(DbErrorCode.DuplicateProjectCode)) {
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
  }, {
    resetForm: false,
    taintedMessage: true,
  });

  const defaultCode = '-train-flex';
  //can't use $form in resource directly otherwise any field change would retrigger it
  let formCode = $derived($form.code);
  const codeIsAvailable = resource(() => formCode,
    code => {
      //don't query for the default code as it's probably not what they want
      if (!browser || !code || !user.canCreateProjects || code === defaultCode) return Promise.resolve(true);
      return _projectCodeAvailable(code);
    }, {initialValue: true, debounce: DEFAULT_DEBOUNCE_TIME});
  const codeErrors = $derived([
    ...new Set(concatAll($errors.code, codeIsAvailable.current ? undefined : $t('project.create.code_exists'))),
  ]);

  let relatedProjectsInput = $derived({projectName: $form.name, langCode: $form.languageCode, orgId: $form.orgId})
  const relatedProjects = resource(() => relatedProjectsInput, async input => {
    if (!input.orgId) return [];
    const byLangCodePromise = _getProjectsByLangCodeAndOrg({langCode: input.langCode, orgId: input.orgId});
    const byNamePromise = _getProjectsByNameAndOrg(input);
    const byLangCode = await byLangCodePromise;
    const byName = await byNamePromise;
    // Put projects related by language code first as they're more likely to be real matches
    const uniqueByName = byName.filter((n) => byLangCode.findIndex((c) => c.id == n.id) == -1);
    return [...byLangCode, ...uniqueByName];
  }, {initialValue: [], debounce: DEFAULT_DEBOUNCE_TIME});

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

  onMount(() => {
    // we want to do this once after the user has been set
    requestingUser = data.requestingUser;
    const urlValues = getSearchParamValues<CreateProjectInput>();
    form.update(
      (form) => {
        if (urlValues.id) projectId = urlValues.id;
        if (urlValues.name) form.name = urlValues.name;
        if (urlValues.description) form.description = urlValues.description;
        if (urlValues.type) form.type = urlValues.type;
        if (urlValues.orgId) form.orgId = urlValues.orgId;
        if (!form.orgId && !user.isAdmin && myOrgs?.[0]) {
          form.orgId = myOrgs[0].id;
        }
        if (urlValues.retentionPolicy && (urlValues.retentionPolicy !== RetentionPolicy.Dev || user.isAdmin))
          form.retentionPolicy = urlValues.retentionPolicy;
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
      },
      { taint: false },
    );
  });

  const calculatedCode = $derived(buildProjectCode($form.languageCode, $form.type, $form.retentionPolicy));
  const code = $derived($form.customCode ? $form.code : calculatedCode);

  watch(() => code, () => {
    form.update((form) => {
      form.code = code;
      return form;
    },
    { taint: false },
    );
  });

  let selectedProject: { name: string; id: string } | undefined = $state(undefined);
  let showRelatedProjects = $state(true);

  // When the related-projects list changes, keep selectedProject up-to-date
  watch(() => relatedProjects.current, projects => {
    if (selectedProject) selectedProject = projects.find((p) => selectedProject?.id === p.id);
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
  {#if projectStatus?.exists}
    <div class="text-center">
      <div>
        <p>{$t('project.create.already_approved')}</p>
        {#if projectStatus.deleted}
          <p>{$t('project.create.already_approved_deleted')}</p>
        {/if}
      </div>
      {#if projectStatus.accessibleCode}
        <a class="btn btn-primary mt-4" href={projectUrl({ code: projectStatus.accessibleCode })}
          >{$t('project.create.go_to_project')}</a
        >
      {/if}
    </div>
  {:else}
    <Form {enhance}>
      <Input
        label={$t('project.create.name')}
        description={$t('project.create.name_description')}
        bind:value={$form.name}
        error={$errors.name}
        autofocus
      />

      <ProjectTypeSelect bind:value={$form.type} error={$errors.type} />

      <Select id="org" label={$t('project.create.org')} bind:value={$form.orgId} error={$errors.orgId}>
        <option value="">{$t('project_page.organization.placeholder')}</option>
        {#each myOrgs as org (org.id)}
          <option value={org.id}>{org.name}</option>
        {/each}
      </Select>

      <AdminContent>
        <div class="form-control">
          <div class="label">
            <span class="label-text">{$t('project_page.members.title')}</span>
          </div>
          {#if requestingUser}
            <MemberBadge
              canManage
              member={{ ...requestingUser, role: ProjectRole.Manager }}
              type="new"
              onAction={() => (requestingUser = undefined)}
            />
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
        error={codeErrors}
        readonly={!$form.customCode}
      />

      {#if relatedProjects.current.length}
        {#if showRelatedProjects}
          <!-- Note, not using RadioButtonGroup here so we can better customize the display to the needs of this form -->
          <div role="radiogroup" aria-labelledby="label-extra-projects" id="group-extra-projects">
            <div class="legend" id="label-extra-projects">
              {$t('project.create.maybe_related')}
            </div>
            {#each relatedProjects.current as proj (proj.id)}
              <div class="form-control w-full">
                <label class="label cursor-pointer justify-normal pb-0">
                  <input
                    id={`extra-projects-${proj.code}`}
                    type="radio"
                    bind:group={selectedProject}
                    value={proj}
                    class="radio mr-2"
                  />
                  <span class="label-text inline-flex items-center gap-2">
                    {proj.name} ({proj.code}) <br />
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
              onclick={() => {
                if (selectedProject) void askToJoinProject(selectedProject.id, selectedProject.name);
              }}
            >
              {$t('project.create.ask_to_join')}
            </Button>
            <Button class="mr-2" variant="btn-warning" onclick={() => (showRelatedProjects = false)}>
              {$t('project.create.no_thanks')}
            </Button>
          </div>
        {:else}
          <button class="btn btn-ghost btn-sm mb-4" tabindex="0" onclick={() => (showRelatedProjects = true)}>
            {$t('project.create.click_to_view_related_projects', { count: relatedProjects.current.length })}
          </button>
        {/if}
      {/if}

      {#if !relatedProjects.current?.length || !showRelatedProjects}
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
          <Checkbox label="Force draft project creation" bind:value={forceDraft} />
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
  {/if}
</TitlePage>
