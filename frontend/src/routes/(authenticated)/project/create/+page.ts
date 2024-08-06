import type { $OpResult, CreateProjectInput, CreateProjectMutation, ProjectsByLangCodeAndOrgQuery } from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { getSearchParam } from '$lib/util/query-params';
import { isGuid } from '$lib/util/guid';
import type { Readable } from 'svelte/store';

export async function load(event: PageLoadEvent) {
  const userIsAdmin = (await event.parent()).user.isAdmin;
  const requestingUserId = getSearchParam<CreateProjectInput>('projectManagerId', event.url.searchParams);
  let requestingUser = null;
  const client = getClient();
  if (userIsAdmin && isGuid(requestingUserId)) {
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

    requestingUser = userResultsPromise.data?.users?.items?.[0];
  }

  let orgs;
  if (userIsAdmin) {
    const orgsPromise = await client.query(graphql(`
          query loadOrgs {
              orgs {
                  id
                  name
              }
          }
      `), {}, { fetch: event.fetch });
    orgs = orgsPromise.data?.orgs;
  } else {
    const myOrgsPromise = await client.query(graphql(`
          query loadMyOrgs {
              myOrgs {
                  id
                  name
              }
          }
      `), {}, { fetch: event.fetch });
    orgs = myOrgsPromise.data?.myOrgs;
  }
  return { requestingUser, myOrgs: orgs };
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

export async function _projectCodeAvailable(code: string): Promise<boolean> {
  const result = await fetch(`/api/project/projectCodeAvailable/${encodeURIComponent(code)}`);
  if (!result.ok) throw new Error('Failed to check project code availability');
  return await result.json() as boolean;
}

export async function _getProjectsByLangCodeAndOrg(input: { orgId: string, langCode: string }): Promise<Readable<ProjectsByLangCodeAndOrgQuery['projectsByLangCodeAndOrg']>> {
  const client = getClient();
  //language=GraphQL
  const results = await client.awaitedQueryStore(fetch,
    graphql(`
      query ProjectsByLangCodeAndOrg($input: ProjectsByLangCodeAndOrgInput!) {
        projectsByLangCodeAndOrg(input: $input) {
          id
          code
          name
          description
        }
      }
    `), { input }
  );
  return results.projectsByLangCodeAndOrg;
}
