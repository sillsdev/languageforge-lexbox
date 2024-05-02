export { default as SupHelp } from './SupHelp.svelte';

export const helpLinks = {
  helpList: 'https://scribehow.com/page/Language_Depot_How-tos__Jy5qu62XRQ-pVGGw6-Cqbw',
  createProject: 'https://scribehow.com/shared/Create_a_Project__3LFa5XTHSmOLbSSOm8hZKQ',
  addProjectMember: 'https://scribehow.com/shared/Add_Project_Member__bUJVVK2QT9KhWMqtiPYckA',
  confidentiality: 'https://scribehow.com/shared/Project_Confidentiality__s6TX8_wFQ1ejVpH1s5Bsmw',
  bulkAddCreate: 'https://scribehow.com/shared/Bulk_AddCreate_Project_Members__3wwDKk3TTGaAwMEmT4rrXQ',
  projectRequest: 'https://scribehow.com/shared/Project_requests__zOdcHT8KRGygGmPgr5z2_A',
};

export type HelpLink = typeof helpLinks[keyof typeof helpLinks];
