/* eslint-disable */
//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

import type {IMiniLcmFeatures} from './IMiniLcmFeatures';
import type {IWritingSystems} from '../../MiniLcm/Models/IWritingSystems';
import type {IPartOfSpeech} from '../../MiniLcm/Models/IPartOfSpeech';
import type {IPublication} from '../../MiniLcm/Models/IPublication';
import type {ISemanticDomain} from '../../MiniLcm/Models/ISemanticDomain';
import type {IComplexFormType} from '../../MiniLcm/Models/IComplexFormType';
import type {IEntry} from '../../MiniLcm/Models/IEntry';
import type {IQueryOptions} from '../../MiniLcm/IQueryOptions';
import type {ISense} from '../../MiniLcm/Models/ISense';
import type {IExampleSentence} from '../../MiniLcm/Models/IExampleSentence';
import type {IWritingSystem} from '../../MiniLcm/Models/IWritingSystem';
import type {WritingSystemType} from '../../MiniLcm/Models/WritingSystemType';
import type {IComplexFormComponent} from '../../MiniLcm/Models/IComplexFormComponent';

export interface IMiniLcmJsInvokable
{
	supportedFeatures() : Promise<IMiniLcmFeatures>;
	getWritingSystems() : Promise<IWritingSystems>;
	getPartsOfSpeech() : Promise<IPartOfSpeech[]>;
	getPublications() : Promise<IPublication[]>;
	getSemanticDomains() : Promise<ISemanticDomain[]>;
	getComplexFormTypes() : Promise<IComplexFormType[]>;
	getComplexFormType(id: string) : Promise<IComplexFormType>;
	getEntries(options?: IQueryOptions) : Promise<IEntry[]>;
	searchEntries(query: string, options?: IQueryOptions) : Promise<IEntry[]>;
	getEntry(id: string) : Promise<IEntry>;
	getSense(entryId: string, id: string) : Promise<ISense>;
	getPartOfSpeech(id: string) : Promise<IPartOfSpeech>;
	getSemanticDomain(id: string) : Promise<ISemanticDomain>;
	getExampleSentence(entryId: string, senseId: string, id: string) : Promise<IExampleSentence>;
	createWritingSystem(type: WritingSystemType, writingSystem: IWritingSystem) : Promise<IWritingSystem>;
	updateWritingSystem(before: IWritingSystem, after: IWritingSystem) : Promise<IWritingSystem>;
	createPartOfSpeech(partOfSpeech: IPartOfSpeech) : Promise<IPartOfSpeech>;
	updatePartOfSpeech(before: IPartOfSpeech, after: IPartOfSpeech) : Promise<IPartOfSpeech>;
	deletePartOfSpeech(id: string) : Promise<void>;
	createSemanticDomain(semanticDomain: ISemanticDomain) : Promise<ISemanticDomain>;
	updateSemanticDomain(before: ISemanticDomain, after: ISemanticDomain) : Promise<ISemanticDomain>;
	deleteSemanticDomain(id: string) : Promise<void>;
	createComplexFormType(complexFormType: IComplexFormType) : Promise<IComplexFormType>;
	updateComplexFormType(before: IComplexFormType, after: IComplexFormType) : Promise<IComplexFormType>;
	deleteComplexFormType(id: string) : Promise<void>;
	createEntry(entry: IEntry) : Promise<IEntry>;
	updateEntry(before: IEntry, after: IEntry) : Promise<IEntry>;
	deleteEntry(id: string) : Promise<void>;
	createComplexFormComponent(complexFormComponent: IComplexFormComponent) : Promise<IComplexFormComponent>;
	deleteComplexFormComponent(complexFormComponent: IComplexFormComponent) : Promise<void>;
	addComplexFormType(entryId: string, complexFormTypeId: string) : Promise<void>;
	removeComplexFormType(entryId: string, complexFormTypeId: string) : Promise<void>;
	createSense(entryId: string, sense: ISense) : Promise<ISense>;
	updateSense(entryId: string, before: ISense, after: ISense) : Promise<ISense>;
	deleteSense(entryId: string, senseId: string) : Promise<void>;
	addSemanticDomainToSense(senseId: string, semanticDomain: ISemanticDomain) : Promise<void>;
	removeSemanticDomainFromSense(senseId: string, semanticDomainId: string) : Promise<void>;
	createExampleSentence(entryId: string, senseId: string, exampleSentence: IExampleSentence) : Promise<IExampleSentence>;
	updateExampleSentence(entryId: string, senseId: string, before: IExampleSentence, after: IExampleSentence) : Promise<IExampleSentence>;
	deleteExampleSentence(entryId: string, senseId: string, exampleSentenceId: string) : Promise<void>;
}
/* eslint-enable */
