import { getClient } from "$lib/graphQLClient";
import type { PageLoadEvent } from "./$types";
import { graphql } from "$lib/gql";
export async function load(event: PageLoadEvent) {
    const client = getClient(event);
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
`), {projectCode: event.params.project_code}).toPromise();
    if (result.error) throw new Error(result.error.message);
    return {
        project: result.data?.projects[0],
        code: event.params.project_code
    };
}