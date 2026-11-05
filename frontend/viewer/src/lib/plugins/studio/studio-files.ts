/** A file name from the plugin's name, e.g. "Dictionary stats" → "dictionary-stats". */
export function fileSlug(name: string): string {
  return name.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || 'plugin';
}

export function downloadText(text: string, fileName: string, type: string): void {
  const url = URL.createObjectURL(new Blob([text], {type}));
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}

// A download rather than the clipboard: some AI chats cut a long paste short (the prompt is
// ~20 KB, more with the plugin's code), while an attached file gets through whole.
export function downloadPrompt(prompt: string, pluginName: string): void {
  downloadText(prompt, `${fileSlug(pluginName)}-prompt.md`, 'text/markdown');
}

export function downloadPluginHtml(html: string, pluginName: string): void {
  downloadText(html, `${fileSlug(pluginName)}.html`, 'text/html');
}
