import {getClient, graphql} from '$lib/gql';

import type { PageLoadEvent } from './$types';
import type {Readable} from 'svelte/store';
import type {SandboxQueryMyselfQuery} from '$lib/gql/generated/graphql';
import { browser } from '$app/environment';

export async function load(event: PageLoadEvent) {
  if (!browser && event.url.searchParams.has('ssr-gql-500')) {
    await _gqlThrows500(event.fetch);
  }
}

export async function _gqlThrows500(argFetch?: Fetch): Promise<Readable<SandboxQueryMyselfQuery['testingThrowsError']>> {
  const client = getClient();
  //language=GraphQL
  const results = await client.awaitedQueryStore(argFetch ?? fetch, graphql(`
    query sandboxQueryMyself {
      testingThrowsError {
        id
      }
    }
  `), {}, {requestPolicy: 'network-only'});
  return results.testingThrowsError;
}
