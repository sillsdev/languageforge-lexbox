/* eslint-disable */
//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

import type {IObjectWithId} from './IObjectWithId';
import type {IRichMultiString} from '$lib/dotnet-types/i-multi-string';

export interface IExampleSentence extends IObjectWithId
{
	id: string;
	sentence: IRichMultiString;
	translation: IRichMultiString;
	reference?: string;
	senseId: string;
	deletedAt?: string;
}
/* eslint-enable */
