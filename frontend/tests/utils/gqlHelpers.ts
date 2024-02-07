import { expect, type APIRequestContext } from '@playwright/test';
import { serverBaseUrl } from '../envVars';

export function validateGqlErrors(json: {errors: unknown, data: unknown}, expectError = false): void {
  if (!expectError) {
    expect(json.errors).toBeFalsy();
    expect(json.data).toBeDefined();
    Object.values(json.data as {errors: unknown}[]).forEach(value => expect(value.errors).toBeFalsy());
  }
}

export async function executeGql(api: APIRequestContext, gql: string, expectError = false): Promise<unknown> {
  const response = await api.post(`${serverBaseUrl}/api/graphql`, {data: {query: gql}});
  await expect(response, `code was ${response.status()} (${response.statusText()})`).toBeOK();
  const json: unknown = await response.json();
  expect(json, `for query ${gql}`).not.toBeNull();
  validateGqlErrors(json as {errors: unknown, data: unknown}, expectError);
  return json;
}
