import type {AddProjectMemberInput} from "$lib/gql/graphql";
import {getClient} from "$lib/graphQLClient";
import {graphql} from "$lib/gql";
import { invalidate } from "$app/navigation";

export async function addProjectUser(input: AddProjectMemberInput) {
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