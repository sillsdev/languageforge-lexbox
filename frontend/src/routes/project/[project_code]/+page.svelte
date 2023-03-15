<script lang="ts">
	import FormatDate from '$lib/components/FormatDate.svelte'
	import FormatProjectType from '$lib/components/FormatProjectType.svelte'
	import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte'
	import FormatUserProjectRole from '$lib/components/FormatUserProjectRole.svelte'
	import HgWeb from '$lib/components/HgWeb.svelte'
	import t from '$lib/i18n'
	import type { PageData } from './$types'

	export let data: PageData

	$: project = data.project
</script>

{#if project}
	<div>
		<p>
			<span class="text-2xl">{project.name}</span>
		</p>

		<div>
			<span>{project.code}</span>

			<div class="badge badge-lg"><FormatProjectType type={project.type} /></div>
			<div class="badge badge-lg"><FormatRetentionPolicy policy={project.retentionPolicy} /></div>

			<span>{ t('project_page.last_commit') } <FormatDate date={project.lastCommit} /></span>
		</div>

		<p>{project.description || t('project_page.missing_description') }</p>
	</div>

	<div class="divider" />

	<div>
		<p class="text-xl">
			<span class="i-mdi-account-group"/> { t('project_page.members') }
		</p>

		<div class="flex flex-wrap gap-3">
			{#each project.ProjectUsers as member}
				<div class="badge badge-xl badge-primary pr-0">
					{member.User.name}

					<!-- -1px is to cover the border on the right side -->
					<div class="badge badge-xl ml-2" style="margin-right: -1px;">
						<FormatUserProjectRole projectRole={member.role} />
					</div>
				</div>
			{/each}
		</div>
	</div>

	<div class="divider" />

	<div class="grid grid-cols-1">
		<p class="text-xl">{ t('project_page.history') }</p>

		<HgWeb code={project.code} />
	</div>
{:else}
	{ t('project_page.not_found', data.code) }
{/if}
