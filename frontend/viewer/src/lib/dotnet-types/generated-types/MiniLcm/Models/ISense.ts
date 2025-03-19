/* eslint-disable */
//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

import type {IObjectWithId} from './IObjectWithId';
import type {IRichMultiString} from '$lib/dotnet-types/i-multi-string';
import type {IMultiString} from '$lib/dotnet-types/i-multi-string';
import type {IPartOfSpeech} from './IPartOfSpeech';
import type {ISemanticDomain} from './ISemanticDomain';
import type {IExampleSentence} from './IExampleSentence';

export interface ISense extends IObjectWithId
{
	id: string;
	deletedAt?: string;
	entryId: string;
	definition: IRichMultiString;
	gloss: IMultiString;
	partOfSpeech?: IPartOfSpeech;
	partOfSpeechId?: string;
	semanticDomains: ISemanticDomain[];
	exampleSentences: IExampleSentence[];
}
/* eslint-enable */
