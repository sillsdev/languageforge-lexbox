/* eslint-disable */
//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

export interface IAppLauncher
{
	canOpen(uri: string) : Promise<boolean>;
	open(uri: string) : Promise<void>;
	tryOpen(uri: string) : Promise<boolean>;
	openInFieldWorks(entryId: string, projectName: string) : Promise<boolean>;
}
/* eslint-enable */
