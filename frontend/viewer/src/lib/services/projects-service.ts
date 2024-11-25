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

  async importFwDataProject(name: string): Promise<boolean> {
    const r = await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST',
    });
    if (!r.ok) {
      AppNotification.display(`Failed to import FieldWorks project ${name}: ${r.statusText} (${r.status})`, 'error', 'long');
      console.error(`Failed to import FieldWorks project ${name}: ${r.statusText} (${r.status})`, r, await r.text())
    }
    return r.ok;
  }

  async downloadCrdtProject(project: Project) {
    const r = await fetch(`/api/download/crdt/${project.serverAuthority}/${project.id}?projectName=${project.name}`, {method: 'POST'});
    if (!r.ok) {
      AppNotification.display(`Failed to download project ${project.name}: ${r.statusText} (${r.status})`, 'error', 'long');
      console.error(`Failed to download project ${project.name}: ${r.statusText} (${r.status})`, r, await r.text())
    }
    return r.ok;
  }

  async uploadCrdtProject(server: string, projectName: string, lexboxProjectId: string): Promise<boolean> {
    const r = await fetch(`/api/upload/crdt/${server}/${projectName}?lexboxProjectId=${lexboxProjectId}`, {method: 'POST'});
    if (!r.ok) {
      AppNotification.display(`Failed to upload project ${projectName}: ${r.statusText} (${r.status})`, 'error', 'long');
      console.error(`Failed to upload project ${projectName}: ${r.statusText} (${r.status})`, r, await r.text())
    }
    return r.ok;
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
