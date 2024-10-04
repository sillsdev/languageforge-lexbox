import { expect, type APIRequestContext, type Page } from '@playwright/test';
import { serverBaseUrl } from '../envVars';

export function validateGqlErrors(json: {errors: unknown, data: unknown}, expectError = false): void {
  if (!expectError) {
    expect(json.errors).toBeFalsy();
    expect(json.data).toBeDefined();
    Object.values(json.data as {errors: unknown}[]).forEach(value => expect(value.errors).toBeFalsy());
  }
}

export async function executeGql<T>(api: APIRequestContext, gql: string, expectError = false): Promise<T> {
  const response = await api.post(`${serverBaseUrl}/api/graphql`, {data: {query: gql}});
  await expect(response, `code was ${response.status()} (${response.statusText()})`).toBeOK();
  const json: unknown = await response.json();
  expect(json, `for query ${gql}`).not.toBeNull();
  validateGqlErrors(json as {errors: unknown, data: unknown}, expectError);
  return json as T;
}

export async function waitForGqlResponse<T>(page: Page, action: () => Promise<void>): Promise<T> {
  const gqlPromise = page.waitForResponse('/api/graphql');
  await action();
  const response = await gqlPromise;
  expect(response.ok(), `code was ${response.status()} (${response.statusText()})`).toBeTruthy();
  const json: unknown = await response.json();
  expect(json, `for query ${response.request().postData()}`).not.toBeNull();
  validateGqlErrors(json as { errors: unknown, data: unknown });
  return json as T;
}
