import type {$OpResult, AskToJoinProjectMutation, CreateProjectInput, CreateProjectMutation, LoadRequestingUserQuery, ProjectStatus, ProjectsByLangCodeAndOrgQuery, ProjectsByNameAndOrgQuery} from '$lib/gql/types';
import {getClient, graphql} from '$lib/gql';

import type {PageLoadEvent} from './$types';
import {getSearchParam} from '$lib/util/query-params';
import {isGuid} from '$lib/util/guid';

export async function load(event: PageLoadEvent) {
  const userIsAdmin = (await event.parent()).user.isAdmin;
  const requestingUserId = getSearchParam<CreateProjectInput>('projectManagerId', event.url.searchParams);
  let requestingUser: NonNullable<NonNullable<NonNullable<LoadRequestingUserQuery>['users']>['items']>[number] | undefined;
  const client = getClient();
  if (userIsAdmin && isGuid(requestingUserId)) {
    const userResultsPromise = await client.query(graphql(`
          query loadRequestingUser($userId: UUID!) {
              users(
                where: {id: {eq: $userId}}, take: 1) {
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

  let orgs: undefined | { id: string, name: string }[];
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

  const projectId = getSearchParam<CreateProjectInput>('id', event.url.searchParams);
  let projectStatus: ProjectStatus | undefined;
  if (projectId) {
    const projectStatusResult = await client.query(graphql(`
        query loadProjectStatus($projectId: UUID!) {
            projectStatus(projectId: $projectId) {
                id
                exists
                deleted
                accessibleCode
            }
        }
    `), {projectId}, {fetch: event.fetch});
    projectStatus = projectStatusResult.data?.projectStatus;
  }

  return { requestingUser, myOrgs: orgs, projectStatus };
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

export async function _getProjectsByLangCodeAndOrg(input: { orgId: string, langCode: string }): Promise<ProjectsByLangCodeAndOrgQuery['projectsByLangCodeAndOrg']> {
  if (input.langCode.length !== 3 && input.langCode.length !== 2) return [];
  const client = getClient();
  //language=GraphQL
  const results = await client.query(
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
  return results.data?.projectsByLangCodeAndOrg ?? [];
}

export async function _getProjectsByNameAndOrg({ orgId, projectName }: { orgId: string, projectName: string }): Promise<ProjectsByNameAndOrgQuery['projectsInMyOrg']> {
  if (projectName.length < 3) return [];
  const client = getClient();
  //language=GraphQL
  const results = await client.query(
    graphql(`
      query ProjectsByNameAndOrg($input: ProjectsInMyOrgInput!, $filter: ProjectFilterInput) {
        projectsInMyOrg(input: $input, where: $filter) {
          id
          code
          name
          description
        }
      }
    `), { input: { orgId }, filter: {name: {icontains: projectName} } }
  );
  return results.data?.projectsInMyOrg ?? [];
}

export async function _askToJoinProject(projectId: string): $OpResult<AskToJoinProjectMutation> {
  const result = await getClient().mutation(
    //language=GraphQL
    graphql(`
      mutation askToJoinProject($input: AskToJoinProjectInput!) {
        askToJoinProject(input: $input) {
          project {
            id
          }
          errors {
            __typename
            ... on DbError {
              code
            }
          }
        }
      }
    `),
    { input: { projectId } });
  return result;
}
