<script lang="ts">
  import { page } from '$app/stores';
  import t from '$lib/i18n';
  import type { ProjectPageData } from '$lib/page-data';
  import { get } from 'svelte/store';

  interface Breadcrumb {
    name: string;
    href: string;
  }
  let crumbs: Breadcrumb[] = [];

  type CrumbMapperFunc = (token: string) => string;
  type CrumbValue = string | CrumbConfig | CrumbMapperFunc | undefined | null;
  type CrumbFallbackMapper = { _get: CrumbMapperFunc };
  type CrumbWithChild = { _name: string | null };
  interface CrumbConfig extends Record<string, CrumbValue>, Partial<CrumbFallbackMapper>, Partial<CrumbWithChild> {}

  const CRUMB_CONFIG = {
    _name: 'user_dashboard.home_title',
    admin: 'admin_dashboard.title',
    resetPassword: 'reset_password.title',
    project: {
      _name: null,
      create: 'project.create.title',
      _get: () => {
        const data = $page.data as ProjectPageData;
        return data.project ? get(data.project)?.name ?? data.code : data.code;
      },
    },
    user: 'account_settings.title',

  } as const satisfies CrumbConfig;

  const ROOT_CRUMB = { name: CRUMB_CONFIG._name, href: '/' };

  $: {
    crumbs = [ROOT_CRUMB];
    let href = '';
    let currConfig: CrumbConfig = CRUMB_CONFIG;

    const path = $page.url.pathname;
    const tokens = path.split('/').filter((t) => t);
    for (const token of tokens) {
      href += `/${token}`;
      const name = lookupCrumbName(token, currConfig);
      if (name) {
        crumbs.push({ name, href });
      }
      currConfig = currConfig[token] as CrumbConfig;
      if (!currConfig) {
        break;
      }
    }
  }

  function lookupCrumbName(token: string, crumbConfig: CrumbConfig): string | null {
    const crumbValue = crumbConfig[token];
    if (!crumbValue) {
      if (crumbConfig._get) {
        return crumbConfig._get(token);
      }
    } else if (typeof crumbValue === 'function') {
      return crumbValue(token);
    } else if (typeof crumbValue === 'string') {
      return crumbValue;
    } else if (crumbValue._name !== undefined) {
      return crumbValue._name;
    }
    console.error(`No breadcrumb found for '${token}'`);
    return token;
  }
</script>

<div class="text-sm breadcrumbs p-0">
  <ul>
    {#each crumbs as crumb, i}
      <li>
        {#if i == crumbs.length - 1}
          {$t(crumb.name)}
        {:else}
          <a href={crumb.href}>{$t(crumb.name)}</a>
        {/if}
      </li>
    {/each}
  </ul>
</div>
