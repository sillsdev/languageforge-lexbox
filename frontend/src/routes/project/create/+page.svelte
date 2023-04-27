<script lang="ts">
	import Form from '$lib/forms/Form.svelte';
	import Input from '$lib/forms/Input.svelte';
	import { Page } from '$lib/layout';
	import t from '$lib/i18n';
	import { z } from 'zod';
	import { lexSuperForm, lexSuperValidate } from '$lib/forms';
	import Select from '$lib/forms/Select.svelte';
	import Checkbox from '$lib/forms/Checkbox.svelte';
	const formSchema = z.object({
		name: z.string().min(1, $t('project.create.name_missing')),
		description: z.string().default(''),
		type: z.number().min(0).max(4).default(1),
		retentionPolicy: z.number().min(1).max(4).default(4),
		languageCode: z.string().toLowerCase().min(3, $t('project.create.language_code_too_short')),
		code: z.string().toLowerCase().min(4, $t('project.create.code_too_short')),
		customCode: z.boolean(),
	});
	let { form, errors, valid, update, reset, message, enhance } = lexSuperForm(formSchema);
	let loading = false;
	async function submit() {
		await lexSuperValidate($form, formSchema, update);
		if (!$valid) return;
		//todo submit
	}
	const typeCodeMap: Record<number, string | undefined> = {
		1: 'flex',
		2: 'wesay',
		3: 'onestory',
		4: 'ourword',
	};

	const policyCodeMap: Record<number, string | undefined> = {
		2: 'test',
		3: 'dev',
		4: 'train',
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
		<Select id="type" label={$t('project.create.type')} bind:value={$form.type}>
			<option value="1">{$t('project_type.flex')}</option>
			<option value="2">{$t('project_type.weSay')}</option>
			<option value="3">{$t('project_type.oneStoryEditor')}</option>
			<option value="4">{$t('project_type.ourWord')}</option>
			<option value="0">{$t('project_type.other')}</option>
		</Select>

		<Select
			id="policy"
			label={$t('project.create.retention-policy')}
			bind:value={$form.retentionPolicy}
		>
			<option value="1">{$t('retention_policy.language-project')}</option>
			<option value="4">{$t('retention_policy.training')}</option>
			<option value="2">{$t('retention_policy.test')}</option>
			<option value="3">{$t('retention_policy.dev')}</option>
		</Select>

		<Input
			label={$t('project.create.language-code')}
			bind:value={$form.languageCode}
			error={$errors.languageCode}
		/>
		<Checkbox label={$t('project.create.custom-code')} bind:value={$form.customCode} />
		<Input label={$t('project.create.code')} bind:value={$form.code} error={$errors.code} readonly={!$form.customCode} />

		<button on:click={submit} class="btn btn-primary" class:loading>
			{$t('project.create.submit')}
		</button>
	</Form>
</Page>
