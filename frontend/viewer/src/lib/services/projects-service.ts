﻿import {AppNotification} from '../notifications/notifications';
import type {IServerStatus, ICombinedProjectsService, IProjectModel, ILexboxServer, IServerProjects} from '$lib/dotnet-types';

export type Project = IProjectModel;
export type ServerStatus = IServerStatus;

export class ProjectService implements ICombinedProjectsService {
  supportsFwData(): Promise<boolean> {
      throw new Error('Method not implemented.');
  }
  remoteProjects(): Promise<IServerProjects[]> {
      throw new Error('Method not implemented.');
  }
  downloadProject(lexboxProjectId: string, projectName: string, server: ILexboxServer): Promise<void> {
      throw new Error('Method not implemented.');
  }
  async createProject(newProjectName: string): Promise<void> {

    if (!newProjectName) {
      throw new Error('Project name is required');
    }
    const response = await fetch(`/api/project?name=${newProjectName}`, {
      method: 'POST',
    });

    if (!response.ok) {
      throw new Error(await response.text());
    }
    return;
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
    const projects = await this.localProjects();
    //todo project server is always null from local projects`
    return projects.find(p => p.name === projectName)?.serverAuthority ?? null;
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
