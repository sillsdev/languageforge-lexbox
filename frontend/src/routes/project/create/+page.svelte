<script lang="ts">
	import Form from '$lib/forms/Form.svelte';
	import Input from '$lib/forms/Input.svelte';
	import { Page } from '$lib/layout';
	import t from '$lib/i18n';
	import { z } from 'zod';
	import { lexSuperForm } from '$lib/forms';
	import Select from '$lib/forms/Select.svelte';
	import Checkbox from '$lib/forms/Checkbox.svelte';
	import { _createProject } from './+page';
	import { DbErrorCode, ProjectType, RetentionPolicy } from '$lib/gql/graphql';
	import { goto } from '$app/navigation';
	const formSchema = z.object({
		name: z.string().min(1, $t('project.create.name_missing')),
		description: z.string().min(1, $t('project.create.description_missing')),
		type: z.nativeEnum(ProjectType).default(ProjectType.FlEx),
		retentionPolicy: z.nativeEnum(RetentionPolicy).default(RetentionPolicy.Training),
		languageCode: z.string().toLowerCase().min(3, $t('project.create.language_code_too_short')),
		code: z.string().toLowerCase().min(4, $t('project.create.code_too_short')),
		customCode: z.boolean().default(false),
	});
	//random guid
	const projectId = crypto.randomUUID();
	let { form, errors, message, enhance, submitting } = lexSuperForm(formSchema, async ({form}) => {
		const result = await _createProject({
			id: projectId,
			name: $form.name,
			code: $form.code,
			description: $form.description,
			type: $form.type,
			retentionPolicy: $form.retentionPolicy,
		});
		if (result.error) {
			if (result.data?.createProject.errors?.some((e) => e.code === DbErrorCode.Duplicate)) {
				form.errors.code = [$t('project.create.code_exists')];
			} else {
				form.message = result.error.message;
			}

			return;
		}
		await goto(`/project/${$form.code}`);
	});
	const typeCodeMap: Partial<Record<ProjectType, string | undefined>> = {
		[ProjectType.FlEx]: 'flex',
		[ProjectType.WeSay]: 'wesay',
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
		$form.code = `${$form.languageCode}${policyCode}-${typeCode}`;
	}
</script>

<Page>
	<h1 class="text-lg">Create Project</h1>

	<Form {enhance}>
		<Input
			label={$t('project.create.name')}
			bind:value={$form.name}
			error={$errors.name}
			autofocus
			required
		/>
		<div class="form-control">
			<label class="label" for="description">
				<span class="label-text">Description</span>
			</label>
			<textarea
				id="description"
				class="textarea textarea-bordered h-24"
				bind:value={$form.description}
			/>
		</div>
		<Select id="type" label={$t('project.create.type')} bind:value={$form.type} error={$errors.type}>
			<option value={ProjectType.FlEx}>{$t('project_type.flex')}</option>
			<option value={ProjectType.WeSay}>{$t('project_type.weSay')}</option>
			<option value={ProjectType.OneStoryEditor}>{$t('project_type.oneStoryEditor')}</option>
			<option value={ProjectType.OurWord}>{$t('project_type.ourWord')}</option>
		</Select>

		<Select
			id="policy"
			label={$t('project.create.retention-policy')}
			bind:value={$form.retentionPolicy}
			error={$errors.retentionPolicy}>
			<option value={RetentionPolicy.Verified}>{$t('retention_policy.language-project')}</option>
			<option value={RetentionPolicy.Training}>{$t('retention_policy.training')}</option>
			<option value={RetentionPolicy.Test}>{$t('retention_policy.test')}</option>
			<option value={RetentionPolicy.Dev}>{$t('retention_policy.dev')}</option>
		</Select>

		<Input
			label={$t('project.create.language-code')}
			bind:value={$form.languageCode}
			error={$errors.languageCode}
		/>
		<Checkbox label={$t('project.create.custom-code')} bind:value={$form.customCode} />
		<Input
			label={$t('project.create.code')}
			bind:value={$form.code}
			error={$errors.code}
			readonly={!$form.customCode}
		/>
		{#if $message}
			<label class="label">
				<span class="label-text-alt text-lg text-error mb-2">{$message}</span>
			</label>
		{/if}
		<button type="submit" class="btn btn-primary mb-2" class:loading={$submitting}>
			{$t('project.create.submit')}
		</button>
	</Form>
</Page>
