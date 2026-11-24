import {fileNameSlug} from '$project/data/plugin-service.svelte';

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
  downloadText(prompt, `${fileNameSlug(pluginName)}-prompt.md`, 'text/markdown');
}

export function downloadPluginHtml(html: string, pluginName: string): void {
  downloadText(html, `${fileNameSlug(pluginName)}.html`, 'text/html');
}
