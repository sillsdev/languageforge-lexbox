import type {PageLoadEvent} from "./$types";
import {getClient} from "$lib/graphQLClient";
import { graphql } from "$lib/gql";

export async function load(event: PageLoadEvent) {
    const client = getClient();

    //language=GraphQL
    const results = await client.query(graphql(`
        query loadAdminDashboard {
            projects(orderBy: [
                {lastCommit: ASC_NULLS_FIRST},
                {name: ASC}
            ]) {
                code
                id
                name
                lastCommit
                type
                projectUsersAggregate {
                    aggregate {
                        count
                    }
                }
            }
            users(orderBy: {name: ASC}) {
                id
                name
                email
                isAdmin
                createdDate
            }
        }
    `), {}, {fetch: event.fetch}).toPromise();
    if (results.error) throw new Error(results.error.message);
    return {
        projects: results.data?.projects ?? [],
        users: results.data?.users ?? []
    }
}
