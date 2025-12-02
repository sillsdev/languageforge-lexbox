<script lang="ts">
  import {page} from '$app/state';
  import UnexpectedError from '$lib/error/UnexpectedError.svelte';
  import t from '$lib/i18n';
  import {Layout, Page, HomeBreadcrumb, PageBreadcrumb} from '$lib/layout';
  import {resolve} from '$app/paths';

  const status = page.status;
</script>

{#if page.data.user}
  <HomeBreadcrumb />
  {#each page.url.pathname.split('/').slice(1) as path, i (i)}
    <PageBreadcrumb>{path}</PageBreadcrumb>
  {/each}
{/if}

<Layout>
  <Page>
    <div class="py-6">
      {#if status === 404}
        <div class="flex flex-col gap-4 items-center">
          <div class="flex gap-2">
            <span class="i-mdi-emoticon-confused-outline text-3xl"></span>
            <span class="text-2xl">{$t('errors.not_found')}</span>
          </div>
        </div>
      {:else}
        <UnexpectedError />
      {/if}
      <div class="mt-8 text-center">
        <a class="btn btn-success" href={resolve('/')} rel="external">{$t('errors.go_home')} <span class="i-mdi-home-outline text-xl"></span></a>
      </div>
    </div>
  </Page>
</Layout>
