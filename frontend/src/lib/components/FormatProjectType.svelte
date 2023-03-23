<script lang="ts">
	import t from '$lib/i18n';


	export let type: number

	const types = {
		1: $t('project_type.flex'),
	}
</script>

{ types[type] || $t('unknown') }
