<script lang="ts">
  import {resolve} from '$app/paths';
  import {Button} from '$lib/forms';
  import t from '$lib/i18n';
  import {getErrorMessage} from './utils';

  interface Props {
    error: unknown;
    onRetry: () => void;
  }

  const {error, onRetry}: Props = $props();
</script>

<div class="flex flex-col gap-4 items-start">
  <div class="flex gap-4">
    <span class="i-mdi-alert-circle-outline text-3xl"></span>
    <span class="text-2xl">{$t('errors.apology')}</span>
  </div>
  <p class="max-w-full whitespace-pre-wrap">{getErrorMessage(error)}</p>
  <div class="flex flex-wrap gap-4 mt-4">
    <Button variant="btn-success" outline onclick={onRetry}>
      {$t('errors.retry')} <span class="i-mdi-refresh text-xl"></span>
    </Button>
    <a class="btn btn-success" href={resolve('/')} rel="external">
      {$t('errors.go_home')} <span class="i-mdi-home-outline text-xl"></span>
    </a>
  </div>
</div>
