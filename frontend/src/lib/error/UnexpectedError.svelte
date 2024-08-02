<script lang="ts">
  /* eslint-disable svelte/no-dom-manipulating */
  import { env } from '$env/dynamic/public';
  import { useError } from '.';
  import t from '$lib/i18n';
  import { derived } from 'svelte/store';
  import { onDestroy } from 'svelte';
  import { browser } from '$app/environment';

  let alertMessageElem: HTMLElement;
  let traceIdElem: HTMLElement;

  const error = derived(useError(), (error) => {
    if (error) {
      const updatedDetected = error.updateDetected ? '\r\nUpdate detected' : '';
      return {
        ...error,
        message: `${error?.message}\r\n(${error?.handler})${updatedDetected}`,
      };
    }
  });

  const mailToParams = derived(error, (error) => {
    if (!error) {
      return '';
    }

    const body = `${error.message}\r\nError code: ${error.traceId}`.replaceAll('\r\n', '%0D%0A%0D%0A');
    const subject = `Lexbox - Unexpected error`;
    return `?subject=${subject}&body=${body}`;
  });

  onDestroy(
    // subscribe() is more durable than reactive syntax
    error.subscribe((e) => {
      if (e) {
        alertMessageElem = alertMessageElem ?? (browser ? document.querySelector('.error-message') : undefined) ?? undefined;
        if (alertMessageElem) alertMessageElem.textContent = e.message;
        traceIdElem = traceIdElem ?? (browser ? document.querySelector('.trace-id') : undefined) ?? undefined;
        if (traceIdElem) traceIdElem.textContent = e.traceId;
      }
    })
  );

  function onTraceIdClick(event: MouseEvent): void {
    if (event.ctrlKey) {
      const traceId = (event.target as HTMLElement).textContent as string;
      const honeyCombEnv = getHoneyCombEnv();
      const url = `https://ui.honeycomb.io/sil-language-forge/environments/${honeyCombEnv}/trace?trace_id=${traceId}`;
      window.open(url, '_blank')?.focus();
    }
  }

  function getHoneyCombEnv(): string {
    const _env = env.PUBLIC_ENV_NAME.toLowerCase();
    if (_env.includes('prod')) {
      return 'prod';
    } else if (_env.includes('stag')) {
      return 'staging';
    } else if (_env.includes('develop')) {
      return 'develop';
    } else {
      return 'test';
    }
  }
</script>

<div class="flex flex-col gap-4 items-start">
  <div class="flex gap-4">
    <span class="i-mdi-alert-circle-outline text-3xl" />
    <span class="text-2xl">{$t('errors.apology')}</span>
  </div>

  <div class="max-w-full whitespace-pre-wrap error-message" style="overflow-wrap: break-word" bind:this={alertMessageElem}>
    {$error?.message}
  </div>

  <div>
    {$t('errors.mail_us_at')}
    <a class="link" href="mailto:issues@languageforge.org{$mailToParams}">issues@languageforge.org</a>.
    {$t('errors.please_include')}
  </div>

  <div>
    <span>{$t('errors.error_code')}:</span>
    <!-- svelte-ignore a11y-no-static-element-interactions a11y-click-events-have-key-events -->
    <span on:click={onTraceIdClick} class="trace-id" bind:this={traceIdElem}>{$error?.traceId}</span>
  </div>
</div>
