<script lang="ts">
	import t from '$lib/i18n'

	export let policy: number

	const policies = {
		1: $t('retention_policy.language-project'),
		2: $t('retention_policy.test'),
		3: $t('retention_policy.dev'),
		4: $t('retention_policy.training'),
	}
</script>

{ policies[policy] || $t('unknown') }
