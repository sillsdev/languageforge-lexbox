export type Project = {
  name: string;
  crdt: boolean;
  fwdata: boolean;
  lexbox: boolean,
  server: string | null,
  id: string | null
};
export type ServerStatus = { displayName: string; loggedIn: boolean; loggedInAs: string | null };
export function useProjectsService() {
  return projectService;
}
export class ProjectService {
  async createProject(newProjectName: string): Promise<{error: string|undefined}> {

    if (!newProjectName) {
      return {error: 'Project name is required'};
    }
    const response = await fetch(`/api/project?name=${newProjectName}`, {
      method: 'POST',
    });

    if (!response.ok) {
      return {error: await response.json()};
    }
    return {error: undefined};
  }

  async importFwDataProject(name: string) {
    await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST',
    });
  }

  async downloadCrdtProject(project: Project) {
    await fetch(`/api/download/crdt/${project.server}/${project.name}`, {method: 'POST'});
  }

  async uploadCrdtProject(server: string, projectName: string) {
    await fetch(`/api/upload/crdt/${server}/${projectName}`, {method: 'POST'});
  }
  async getProjectServer(projectName: string): Promise<string|null> {
    const projects = await this.fetchProjects();
    //todo project server is always null from local projects`
    return projects.find(p => p.name === projectName)?.server ?? null;
  }

  async fetchProjects() {
    let r = await fetch('/api/localProjects');
    return (await r.json()) as Project[];
  }

  async fetchRemoteProjects() {

    let r = await fetch('/api/remoteProjects');
    return (await r.json()) as { [server: string]: Project[] };
  }

  async fetchServers() {
    let r = await fetch('/api/auth/servers');
    return (await r.json()) as ServerStatus[];
  }
}
const projectService = new ProjectService();
