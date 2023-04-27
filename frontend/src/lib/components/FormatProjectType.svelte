<script lang="ts">
	import t from '$lib/i18n';


	export let type: number

	const types = {
		1: $t('project_type.flex'),
		2: $t('project_type.weSay'),
		3: $t('project_type.oneStoryEditor'),
		4: $t('project_type.ourWord'),
	}
</script>

{ types[type] || $t('unknown') }
