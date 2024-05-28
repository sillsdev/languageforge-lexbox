<script lang="ts">
  import ProjectTypeIcon from '$lib/components/ProjectType/ProjectTypeIcon.svelte';
  import { ProjectType } from '$lib/gql/types';
  import { AuthenticatedUserIcon } from '$lib/icons';

  import Icon from '$lib/icons/Icon.svelte';
  import type { PageData } from './$types';

  export let data: PageData;
</script>

<div class="flex flex-col items-center grow">
  <div class="flex flex-col justify-center grow max-w-lg">
    <div class="flex-grow"></div>
    <form method="post" action="/api/oauth/open-id-auth" class="flex flex-col items-center">
      <div class="mb-6 grid gap-x-3 gap-y-1 items-center" style="grid-template-columns: auto 1fr">
        <div class="row-span-2">
          <Icon icon="i-mdi-approval" color="text-success" size="text-5xl" />
        </div>
        <h2 class="text-3xl">
          Authorize "{data.appName}"
        </h2>
        <div>
          {data.appName} wants to access your account.
        </div>
      </div>
      <div class="bg-base-200/50 py-5 px-8">
        <div class="grid gap-x-3 gap-y-5" style="grid-template-columns: auto 1fr">
          {#each data.scope?.split(' ') ?? [] as scope}
            {#if scope === 'profile'}
              <div class="grid grid-cols-subgrid col-span-full items-center">
                <AuthenticatedUserIcon size="text-4xl" />
                <div>
                  <div class="font-bold">Personal user data</div>
                  <div>Name, email address and/or username (read-only)</div>
                </div>
              </div>
            {/if}
            {#if scope === 'profile'}
              <div class="grid grid-cols-subgrid col-span-full items-center">
                <ProjectTypeIcon type={ProjectType.FlEx} size="h-8" />
                <div>
                  <div class="font-bold">Projects</div>
                  <div>Project membership and roles (read-only)</div>
                </div>
              </div>
            {/if}
          {/each}
        </div>
      </div>
      <div>
        {#each Object.entries(data.postback) as [key, value]}
          <!--parameters required to resume the auth process-->
          <input type="hidden" name={key} {value} />
        {/each}
      </div>
      <div class="mt-4 flex items-center gap-2">
        {data.appName} is trusted by Language Depot
        <Icon icon="i-mdi-shield-check" color="text-info" size="text-xl" />
      </div>
      <div class="w-full flex max-sm:flex-col justify-center gap-4 mt-6">
        <input type="submit" class="btn whitespace-nowrap btn-secondary" name="submit.deny" value="Deny" />
        <input
          type="submit"
          class="btn whitespace-nowrap btn-success"
          name="submit.accept"
          value="Authorize {data.appName}"
        />
      </div>
    </form>
    <div class="flex-grow-[2]"></div>
  </div>
</div>
