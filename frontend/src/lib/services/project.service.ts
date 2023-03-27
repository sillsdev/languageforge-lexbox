import type {AddProjectMemberInput} from "$lib/gql/graphql";
import {getClient} from "$lib/graphQLClient";
import {graphql} from "$lib/gql";

export async function addProjectUser(input: AddProjectMemberInput) {
    //language=GraphQL
    const result = await getClient().mutation(
        graphql(`
            mutation AddProjectUser($input: AddProjectMemberInput!) {
                addProjectMember(input: $input) {
                    project {
                        id
                        users {
                            id
                            role
                            user {
                                id
                                name 
                            }
                        }
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
        `),
        {input: input}
    ).toPromise();
    return result;
}