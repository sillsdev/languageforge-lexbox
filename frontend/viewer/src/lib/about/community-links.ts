const communityBaseUrl = 'https://community.software.sil.org';

export const featureRequestsUrl = `${communityBaseUrl}/c/fwlite/fwlite-feature-requests/50`;
export const supportForumUrl = `${communityBaseUrl}/c/fwlite/48`;

// Opens the Discourse composer already scoped to the feature-requests category and tagged so
// dashboard ideas group together. Prefill stays English on purpose — it posts to an English forum.
export const dashboardSuggestionUrl = `${communityBaseUrl}/new-topic?${new URLSearchParams({
  category: 'fwlite-feature-requests',
  tags: 'dashboard',
  title: 'What would help me on the dashboard',
  body: 'How I use FieldWorks Lite:\n\nWhat would help me most on the dashboard:\n\n',
}).toString()}`;
