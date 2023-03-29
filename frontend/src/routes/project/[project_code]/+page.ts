import { getClient } from "$lib/graphQLClient";
import type { PageLoadEvent } from "./$types";
import { graphql } from "$lib/gql";
import type { ProjectPageQuery } from "$lib/gql/graphql";
import { invalidate } from "$app/navigation";

export type ProjectUser = ProjectPageQuery["projects"][0]["ProjectUsers"][number];

export async function load(event: PageLoadEvent) {
    const client = getClient(event);
    const project_code = event.params.project_code;
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
            }
        }
`), { projectCode: project_code }).toPromise();
    if (result.error) throw new Error(result.error.message);
    event.depends(`project:${result.data?.projects[0].id}`);
    return {
        project: result.data?.projects[0],
        code: project_code
    };
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