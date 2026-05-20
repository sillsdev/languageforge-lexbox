import type {ICombinedProjectsService, ILexboxServer, IProjectModel, IServerProjects, IServerStatus} from '$lib/dotnet-types';
import type {DownloadProjectByCodeResult} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/DownloadProjectByCodeResult';
import type {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';

import {AppNotification} from '../notifications/notifications';

export type Project = IProjectModel;
export type ServerStatus = IServerStatus;

export class ProjectService implements ICombinedProjectsService {
  serverProjects(_serverId: string, _forceRefresh: boolean): Promise<IServerProjects> {
      throw new Error('Method not implemented.');
  }
  supportsFwData(): Promise<boolean> {
      throw new Error('Method not implemented.');
  }
  remoteProjects(): Promise<IServerProjects[]> {
      throw new Error('Method not implemented.');
  }
  downloadProject(_project: IProjectModel): Promise<void> {
      throw new Error('Method not implemented.');
  }
  downloadProjectByCode(_code: string, _server: ILexboxServer, _userRole: UserProjectRole): Promise<DownloadProjectByCodeResult> {
      throw new Error('Method not implemented.');
  }
  deleteProject(_code: string): Promise<void> {
      throw new Error('Method not implemented.');
  }
  // Applies the shipped SQL template — canonical morph types only, no demo data.
  async createProject(name: string, code: string, vernacularWs: string): Promise<void> {
    if (!name) throw new Error('Project name is required');
    if (!code) throw new Error('Project code is required');
    if (!vernacularWs) throw new Error('Vernacular writing system is required');
    await this.postOk(`/api/project?name=${encodeURIComponent(name)}&code=${encodeURIComponent(code)}&vernacularWs=${encodeURIComponent(vernacularWs)}`);
  }

  // Example/demo project — seeds canonical PreDefinedData and demo entries. Dev use.
  async createDemoProject(name: string): Promise<void> {
    if (!name) throw new Error('Project name is required');
    await this.postOk(`/api/project/demo?name=${encodeURIComponent(name)}`);
  }

  private async postOk(url: string): Promise<void> {
    const response = await fetch(url, {method: 'POST'});
    if (!response.ok) throw new Error(await response.text());
  }

  async importFwDataProject(name: string): Promise<boolean> {
    const r = await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST',
    });
    if (!r.ok) {
      AppNotification.error(`Failed to import FieldWorks project ${name}`, `${r.statusText} (${r.status})`);
      console.error(`Failed to import FieldWorks project ${name}: ${r.statusText} (${r.status})`, r, await r.text())
    }
    return r.ok;
  }

  async downloadCrdtProject(project: Project) {
    const r = await fetch(`/api/download/crdt/${project.server!.authority}/${project.code}`, {method: 'POST'});
    if (!r.ok) {
      AppNotification.error(`Failed to download project ${project.code}`, `${r.statusText} (${r.status})`);
      console.error(`Failed to download project ${project.code}: ${r.statusText} (${r.status})`, r, await r.text())
    }
    return r.ok;
  }

  async uploadCrdtProject(server: string, projectName: string, lexboxProjectId: string): Promise<boolean> {
    const r = await fetch(`/api/upload/crdt/${server}/${projectName}?lexboxProjectId=${lexboxProjectId}`, {method: 'POST'});
    if (!r.ok) {
      AppNotification.error(`Failed to upload project ${projectName}`, `${r.statusText} (${r.status})`);
      console.error(`Failed to upload project ${projectName}: ${r.statusText} (${r.status})`, r, await r.text())
    }
    return r.ok;
  }

  async getProjectServer(projectName: string): Promise<string|null> {
    const projects = await this.localProjects();
    //todo project server is always null from local projects`
    return projects.find(p => p.name === projectName)?.server?.authority ?? null;
  }

  async localProjects(): Promise<Project[]> {
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
