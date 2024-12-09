/* eslint-disable */
//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

import type {DotnetService} from './DotnetService';

export interface IFwLiteProvider
{
	dispose() : void;
	getServices() : { [key in DotnetService]: any };
	getService(service: DotnetService) : any;
	setService(service: DotnetService, serviceInstance: any) : Promise<void>;
	injectCrdtProject(projectName: string) : Promise<any>;
	injectFwDataProject(projectName: string) : Promise<any>;
}
/* eslint-enable */