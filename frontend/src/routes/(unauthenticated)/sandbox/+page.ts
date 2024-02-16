import {getClient, graphql} from '$lib/gql';
import type {SandboxQueryMyselfQuery} from '$lib/gql/generated/graphql';
import type {Readable} from 'svelte/store';

export async function _gqlThrows500(): Promise<Readable<SandboxQueryMyselfQuery['testingThrowsError']>> {
  const client = getClient();
  //language=GraphQL
  const results = await client.awaitedQueryStore(fetch, graphql(`
    query sandboxQueryMyself {
      testingThrowsError {
        id
      }
    }
  `), {}, {requestPolicy: 'network-only'});
  return results.testingThrowsError;
}
