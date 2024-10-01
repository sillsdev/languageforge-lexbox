import {getContext, setContext} from 'svelte';

import type { IEntry } from './mini-lcm';

export type NewEntryDialogOptions = {
  dontNavigate?: true;
};
export type ProjectCommands = {
  createNewEntry: (headword: string, options?: NewEntryDialogOptions) => Promise<IEntry | undefined>,
};

const commandsContextName = 'projectCommands';
export function initProjectCommands(commands: ProjectCommands): ProjectCommands {
  setContext(commandsContextName, commands);
  return commands;
}

export function useProjectCommands(): ProjectCommands {
  return getContext<ProjectCommands>(commandsContextName);
}
