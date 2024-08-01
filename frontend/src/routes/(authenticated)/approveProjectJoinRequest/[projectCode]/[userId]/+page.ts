import type { PageLoadEvent } from './$types';

export async function load(event: PageLoadEvent) {
  const projectCode = event.params.projectCode;
  const userId = event.params.userId;

  const result = await event.fetch(`/api/project/approveProjectJoinRequest/${projectCode}/${userId}`, {method: 'POST'});
  if (result.ok) {
    const resultData = await result.json() as { userName: string };
    return {
      redirect: `/project/${projectCode}`,
      projectCode,
      userId,
      userName: resultData.userName
    };
  }
}
