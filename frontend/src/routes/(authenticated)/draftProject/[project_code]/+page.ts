import type { PageLoadEvent } from './$types';
import { isAdmin, type LexAuthUser } from '$lib/user';
import { error, redirect } from '@sveltejs/kit';
import { getClient, graphql } from '$lib/gql';
import { tryMakeNonNullable } from '$lib/util/store';

import type {
  $OpResult,
  ChangeDraftProjectDescriptionInput,
  ChangeDraftProjectDescriptionMutation,
  ChangeDraftProjectNameInput,
  ChangeDraftProjectNameMutation,
  PromoteDraftProjectInput,
  PromoteDraftProjectMutation,
  DraftProjectPageQuery,
} from '$lib/gql/types';

export type DraftProject = NonNullable<DraftProjectPageQuery['draftProjectByCode']>;

function requireAdmin(user: LexAuthUser | null): void {
  if (!isAdmin(user)) {
    redirect(307, '/');
  }
}

export async function _promoteDraftProject(input: PromoteDraftProjectInput): $OpResult<PromoteDraftProjectMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation PromoteDraftProject($input: PromoteDraftProjectInput!) {
          promoteDraftProject(input: $input) {
            promoteDraftProjectResponse {
              id
              result
            }
            errors {
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input }
    );
  return result;
}

export async function _changeDraftProjectName(input: ChangeDraftProjectNameInput): $OpResult<ChangeDraftProjectNameMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeDraftProjectName($input: ChangeDraftProjectNameInput!) {
          changeDraftProjectName(input: $input) {
            draftProject {
              id
              name
            }
            errors {
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input }
    );
  return result;
}

export async function _changeDraftProjectDescription(input: ChangeDraftProjectDescriptionInput): $OpResult<ChangeDraftProjectDescriptionMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeDraftProjectDescription($input: ChangeDraftProjectDescriptionInput!) {
          changeDraftProjectDescription(input: $input) {
            draftProject {
              id
              description
            }
            errors {
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input }
    );
  return result;
}

export async function load(event: PageLoadEvent) {
  const parentData = await event.parent();
  requireAdmin(parentData.user);
  const client = getClient();
  const projectCode = event.params.project_code;
  const projectResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
        query draftProjectPage($projectCode: String!) {
          draftProjectByCode(code: $projectCode) {
            id
            name
            description
            code
            type
            retentionPolicy
            projectManagerId
          }
        }
      `),
      { projectCode }
    );
  const nonNullableProject = tryMakeNonNullable(projectResult.draftProjectByCode);
  if (!nonNullableProject) {
    error(404);
  }

  event.depends(`project:${projectCode}`);

  return {
    project: nonNullableProject,
    code: projectCode,
  }
}
