/* See: builder.Services.AddProblemDetails. More specifically the ProblemDetails type. */
export type AspProblemDetails = {
  type?: string,
  title?: string,
  status?: number,
}

export async function getAspResponseErrorMessage(response: Response): Promise<string> {
  if (response.statusText) return response.statusText;
  const { responseJson, responseText } = await tryGetJson<AspProblemDetails>(response);
  return responseJson?.title ?? responseJson?.status?.toString() ?? responseText;
}

async function tryGetJson<T extends object = object>(response: Response): Promise<{ responseJson: T | undefined, responseText: string }> {
  const responseText = await response.text();
  let responseJson: T | undefined = undefined;
  try {
    responseJson = JSON.parse(responseText) as T;
  } catch { /* empty */ }

  return { responseJson, responseText };
}
