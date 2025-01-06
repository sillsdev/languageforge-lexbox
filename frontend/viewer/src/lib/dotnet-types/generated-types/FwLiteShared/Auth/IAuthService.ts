/* eslint-disable */
//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

import type {IServerStatus} from './IServerStatus';
import type {ILexboxServer} from './ILexboxServer';

export interface IAuthService
{
	servers() : Promise<IServerStatus[]>;
	signInWebView(server: ILexboxServer) : Promise<void>;
	useSystemWebView() : Promise<boolean>;
	signInWebApp(server: ILexboxServer, returnUrl: string) : Promise<string>;
	logout(server: ILexboxServer) : Promise<void>;
	getLoggedInName(server: ILexboxServer) : Promise<string>;
}
/* eslint-enable */
