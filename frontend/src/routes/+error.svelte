<script lang="ts">
  import { page } from '$app/stores';
  import { LinkRenderer } from '$lib/components/Markdown';
  import UnexpectedError from '$lib/error/UnexpectedError.svelte';
  import t from '$lib/i18n';
  import { AppBar, Content, Page } from '$lib/layout';
  import { error } from '@sveltejs/kit';
  import SvelteMarkdown from 'svelte-markdown';
</script>

<AppBar user={undefined} />

<Content>
  <Page>
    {#if $page.status === 404}
      <div class="flex flex-col gap-4 items-center">
        <div class="flex gap-2">
          <span class="i-mdi-emoticon-confused-outline text-3xl" />
          <span class="text-2xl">{$t('errors.not_found')}</span>
        </div>
        <SvelteMarkdown source={$t('errors.go_home')} renderers={{ link: LinkRenderer }} />
      </div>
    {:else}
      <UnexpectedError />
    {/if}
  </Page>
</Content>
