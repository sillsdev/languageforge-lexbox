import { graphql } from "$lib/gql";
import {getClient} from "$lib/graphQLClient";
import type {CreateProjectInput} from "$lib/gql/graphql";


export async function _createProject(input: CreateProjectInput) {
    const result = await getClient().mutation(
        graphql(`
        mutation createProject($input: CreateProjectInput!) {
            createProject(input: $input) {
                project {
                    id
                }
                errors {
                    ... on DbError {
                        code
                    }
                }
            }

        }
        `),
        {input}
    ).toPromise();
    return result;
}