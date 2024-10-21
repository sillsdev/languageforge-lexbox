import {AppNotification} from '../notifications/notifications';

export type Project = {
  name: string;
  crdt: boolean;
  fwdata: boolean;
  lexbox: boolean,
  serverAuthority: string | null,
  id: string | null
};
export type ServerStatus = { displayName: string; loggedIn: boolean; loggedInAs: string | null, authority: string };
export function useProjectsService(): ProjectService {
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
      return {error: await response.text()};
    }
    return {error: undefined};
  }

  async importFwDataProject(name: string) {
    await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST',
    });
  }

  async downloadCrdtProject(project: Project) {
    const r = await fetch(`/api/download/crdt/${project.serverAuthority}/${project.name}`, {method: 'POST'});
    if (r.status !== 200) {
      AppNotification.display(`Failed to download project, status code ${r.status}`, 'error');
      console.error(`Failed to download project ${project.name}`, r)
    }
  }

  async uploadCrdtProject(server: string, projectName: string) {
    await fetch(`/api/upload/crdt/${server}/${projectName}`, {method: 'POST'});
  }
  async getProjectServer(projectName: string): Promise<string|null> {
    const projects = await this.fetchProjects();
    //todo project server is always null from local projects`
    return projects.find(p => p.name === projectName)?.serverAuthority ?? null;
  }

  async fetchProjects(): Promise<Project[]> {
    const r = await fetch('/api/localProjects');
    return (await r.json()) as Project[];
  }

  async fetchRemoteProjects(): Promise<{ [server: string]: Project[] }> {
    const r = await fetch('/api/remoteProjects');
    return (await r.json()) as { [server: string]: Project[] };
  }

  async fetchServers(): Promise<ServerStatus[]> {
    const r = await fetch('/api/auth/servers');
    return (await r.json()) as ServerStatus[];
  }
}
const projectService = new ProjectService();
