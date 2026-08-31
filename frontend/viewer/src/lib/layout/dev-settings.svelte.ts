// Dev-only settings toggled from DevToolsDialog. These are NOT persisted and are
// unrelated to whether a project is actually read only — see the `dev` release channel.
class DevSettings {
  // Forces the write feature off so the whole UI goes readonly.
  readonly = $state(false);
}

export const devSettings = new DevSettings();
