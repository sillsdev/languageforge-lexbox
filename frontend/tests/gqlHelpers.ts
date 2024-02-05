import { expect, type APIRequestContext } from '@playwright/test';
import { serverBaseUrl } from './envVars';

export function validateGqlErrors(json: {error: unknown, data: unknown}, expectError = false): void {
  if (!expectError) {
    expect(json.error).toBeFalsy();
    expect(json.data).toBeDefined();
    // TODO: Dive into data object properties and make sure none of them have any "errors" subproperties
  }
}

export async function executeGql(api: APIRequestContext, gql: string, expectError = false): Promise<unknown> {
  const response = await api.post(`${serverBaseUrl}/api/graphql`, {data: {query: gql}});
  await expect(response, `code was ${response.status()} (${response.statusText()})`).toBeOK();
  const json: unknown = await response.json();
  expect(json, `for query ${gql}`).not.toBeNull();
  validateGqlErrors(json as {error: unknown, data: unknown}, expectError);
  return json;
}
