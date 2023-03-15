<script lang="ts">
	import t from '$lib/i18n'

	export let projectRole: number

	const roles = {
		2: t('project_role.manager'),
		3: t('project_role.editor'),
	}
</script>

{ roles[projectRole] || t('unknown') }
