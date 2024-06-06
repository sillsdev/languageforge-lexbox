import { getClient, graphql } from '$lib/gql';

import type {PageLoadEvent} from './$types';
import { error } from '@sveltejs/kit';
import { tryMakeNonNullable } from '$lib/util/store';

export const ssr = false; // 💖
export async function load(event: PageLoadEvent) {
  const client = getClient();
  const projectCode = event.params.project_code;
  const projectResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
				query viewerProject($projectCode: String!) {
					projectByCode(code: $projectCode) {
						id
						name
            code
          }
				}
			`),
      { projectCode }
    );

  const nonNullableProject = tryMakeNonNullable(projectResult.projectByCode);
  if (!nonNullableProject) {
    error(404);
  }

  return {
    project: nonNullableProject,
  };
}
