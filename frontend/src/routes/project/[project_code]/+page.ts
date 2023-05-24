import type { AddProjectMemberInput, ChangeProjectDescriptionInput, ChangeProjectNameInput, ProjectPageQuery } from "$lib/gql/graphql";

import type { PageLoadEvent } from "./$types";
import { getClient } from "$lib/graphQLClient";
import { graphql } from "$lib/gql";
import { invalidate } from "$app/navigation";

import logsample from './logsample.json';

export type ProjectUser = ProjectPageQuery["projects"][0]["ProjectUsers"][number];

export async function load(event: PageLoadEvent) {
    const client = getClient(event);
    const projectCode = event.params.project_code;
    const result = await client.query(graphql(`
        query projectPage($projectCode: String!) {
            projects(where: {code: {_eq: $projectCode}}) {
                id
                name
                code
                description
                type
                lastCommit
                createdDate
                retentionPolicy
                ProjectUsers {
                    id
                    role
                    User {
                        id
                        name
                    }
                }
                changesets {
                    node
                    parents
                    date
                    user
                    desc
                }
            }
        }
`), { projectCode }).toPromise();
    if (result.error) throw new Error(result.error.message);
    event.depends(`project:${result.data?.projects[0]?.id}`);
    return {
        project: result.data?.projects[0],
        log: logsample,
        code: projectCode
    };
}

export async function _addProjectUser(input: AddProjectMemberInput) {
    //language=GraphQL
    const result = await getClient().mutation(
        graphql(`
            mutation AddProjectUser($input: AddProjectMemberInput!) {
                addProjectMember(input: $input) {
                    project {
                        id
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
        `),
        {input: input}, 
        //invalidates the graphql project cache
        {additionalTypenames: ['Projects']}
    ).toPromise();
    if (!result.error)
        invalidate(`project:${input.projectId}`);
    return result;
}

export async function _changeProjectName(input: ChangeProjectNameInput) {
    //language=GraphQL
    const result = await getClient().mutation(
        graphql(`
            mutation ChangeProjectName($input: ChangeProjectNameInput!) {
                changeProjectName(input: $input) {
                    project {
                        id
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
        `),
        {input: input}, 
        //invalidates the graphql project cache
        {additionalTypenames: ['Projects']}
    ).toPromise();
    if (!result.error)
        invalidate(`project:${input.projectId}`);
    return result;
}

export async function _changeProjectDescription(input: ChangeProjectDescriptionInput) {
    //language=GraphQL
    const result = await getClient().mutation(
        graphql(`
            mutation ChangeProjectDescription($input: ChangeProjectDescriptionInput!) {
                changeProjectDescription(input: $input) {
                    project {
                        id
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
        `),
        {input: input}, 
        //invalidates the graphql project cache
        {additionalTypenames: ['Projects']}
    ).toPromise();
    if (!result.error)
        invalidate(`project:${input.projectId}`);
    return result;
}

export async function _deleteProjectUser(projectId: string, userId: string) {
    const result = await getClient().mutation(
        graphql(`
    mutation deleteProjectUser($input: RemoveProjectMemberInput!) {
        removeProjectMember(input: $input) {
            code
        }
    }
    `),
        { input: { projectId: projectId, userId: userId } },
        // invalidates the cached project so invalidate below will actually reload the project
        {additionalTypenames: ['Projects']}
        ).toPromise();
    if (!result.error) {
        invalidate(`project:${projectId}`);
    }
}
