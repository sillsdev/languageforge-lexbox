import type { $OpResult, CreateProjectInput, CreateProjectMutation } from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { getSearchParam } from '$lib/util/query-params';
import { isGuid } from '$lib/util/guid';

export async function load(event: PageLoadEvent) {
  const userIsAdmin = (await event.parent()).user.isAdmin;
  const requestingUserId = getSearchParam<CreateProjectInput>('projectManagerId', event.url.searchParams);
  if (userIsAdmin && isGuid(requestingUserId)) {
    const client = getClient();
    const userResultsPromise = await client.query(graphql(`
          query loadRequestingUser($userId: UUID!) {
              users(
                where: {id: {eq: $userId}}) {
                items {
                  id
                  name
                  email
                  username
                  isAdmin
                  emailVerified
                  createdDate
                  locked
                  localizationCode
                  updatedDate
                  lastActive
                  canCreateProjects
                }
              }
          }
      `), { userId: requestingUserId }, { fetch: event.fetch});
    const requestingUser = userResultsPromise.data?.users?.items?.[0];
    return { requestingUser };
  }
}

export async function _createProject(input: CreateProjectInput): $OpResult<CreateProjectMutation> {
  const result = await getClient().mutation(
    graphql(`
        mutation createProject($input: CreateProjectInput!) {
            createProject(input: $input) {
                createProjectResponse {
                  id
                  result
                }
                errors {
                    ... on DbError {
                        code
                    }
                }
            }

        }
        `),
    { input });
  return result;
}
