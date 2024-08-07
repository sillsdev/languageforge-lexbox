/* THIS (.ts) FILE IS GENERATED BY TypedSignalR.Client.TypeScript */
/* eslint-disable */
/* tslint:disable */

import type {Entry, ExampleSentence, PartOfSpeech, QueryOptions, SemanticDomain, Sense, WritingSystem, WritingSystems} from '../../mini-lcm';
import type { ILexboxApiHub, ILexboxClient } from './Lexbox.ClientServer.Hubs';

import { HubConnection } from '@microsoft/signalr';
import type { JsonOperation } from '../Lexbox.ClientServer.Hubs';
import type {WritingSystemType} from '../../services/lexbox-api';

// components

export type Disposable = {
    dispose(): void;
}

export type HubProxyFactory<T> = {
    createHubProxy(connection: HubConnection): T;
}

export type ReceiverRegister<T> = {
    register(connection: HubConnection, receiver: T): Disposable;
}

type ReceiverMethod = {
    methodName: string,
    method: (...args: any[]) => void
}

class ReceiverMethodSubscription implements Disposable {

    public constructor(
        private connection: HubConnection,
        private receiverMethod: ReceiverMethod[]) {
    }

    public readonly dispose = () => {
        for (const it of this.receiverMethod) {
            this.connection.off(it.methodName, it.method);
        }
    }
}

// API

export type HubProxyFactoryProvider = {
    (hubType: "ILexboxApiHub"): HubProxyFactory<ILexboxApiHub>;
}

export const getHubProxyFactory = ((hubType: string) => {
    if(hubType === "ILexboxApiHub") {
        return ILexboxApiHub_HubProxyFactory.Instance;
    }
}) as HubProxyFactoryProvider;

export type ReceiverRegisterProvider = {
    (receiverType: "ILexboxClient"): ReceiverRegister<ILexboxClient>;
}

export const getReceiverRegister = ((receiverType: string) => {
    if(receiverType === "ILexboxClient") {
        return ILexboxClient_Binder.Instance;
    }
}) as ReceiverRegisterProvider;

// HubProxy

class ILexboxApiHub_HubProxyFactory implements HubProxyFactory<ILexboxApiHub> {
    public static Instance = new ILexboxApiHub_HubProxyFactory();

    private constructor() {
    }

    public readonly createHubProxy = (connection: HubConnection): ILexboxApiHub => {
        return new ILexboxApiHub_HubProxy(connection);
    }
}

class ILexboxApiHub_HubProxy implements ILexboxApiHub {

    public constructor(private connection: HubConnection) {
    }

    public readonly GetWritingSystems = async (): Promise<WritingSystems> => {
        return await this.connection.invoke("GetWritingSystems");
    }

    public readonly CreateWritingSystem = async (type: WritingSystemType, writingSystem: WritingSystem): Promise<void> => {
        return await this.connection.invoke("CreateWritingSystem", type, writingSystem);
    }

    public readonly UpdateWritingSystem = async (wsId: string, type: WritingSystemType, update: JsonOperation[]): Promise<WritingSystem> => {
        return await this.connection.invoke("UpdateWritingSystem", wsId, type, update);
    }

    public readonly GetPartsOfSpeech = async (): Promise<PartOfSpeech[]> => {
      return new Promise((resolve, reject) => {
        let partsOfSpeech: PartOfSpeech[] = [];
        this.connection.stream<PartOfSpeech>('GetPartsOfSpeech').subscribe({
          next(value: PartOfSpeech) {
            partsOfSpeech.push(value);
          },
          error(err: any) {
            reject(err);
          },
          complete() {
            resolve(partsOfSpeech);
          }
        });
      });
    }

    public readonly GetSemanticDomains = async (): Promise<SemanticDomain[]> => {
      return new Promise((resolve, reject) => {
        let semanticDomains: SemanticDomain[] = [];
        this.connection.stream<SemanticDomain>('GetSemanticDomains').subscribe({
          next(value: SemanticDomain) {
            semanticDomains.push(value);
          },
          error(err: any) {
            reject(err);
          },
          complete() {
            resolve(semanticDomains);
          }
        });
      });
    }

    public readonly GetEntries = async (options: QueryOptions): Promise<Entry[]> => {
        return await this.connection.invoke("GetEntries", options);
    }

  public readonly SearchEntries = (query: string, options: QueryOptions): Promise<Entry[]> => {
    return new Promise((resolve, reject) => {
      let entries: Entry[] = [];
      this.connection.stream<Entry>('SearchEntries', query, options).subscribe({
        next(value: Entry) {
          entries.push(value);
        },
        error(err: any) {
          reject(err);
        },
        complete() {
          resolve(entries);
        }
      });
    });
  };

    public readonly GetEntry = async (id: string): Promise<Entry> => {
        return await this.connection.invoke("GetEntry", id);
    }

    public readonly CreateEntry = async (entry: Entry): Promise<Entry> => {
        return await this.connection.invoke("CreateEntry", entry);
    }

    public readonly UpdateEntry = async (id: string, update: JsonOperation[]): Promise<Entry> => {
        return await this.connection.invoke("UpdateEntry", id, update);
    }

    public readonly DeleteEntry = async (id: string): Promise<void> => {
        return await this.connection.invoke("DeleteEntry", id);
    }

    public readonly CreateSense = async (entryId: string, sense: Sense): Promise<Sense> => {
        return await this.connection.invoke("CreateSense", entryId, sense);
    }

    public readonly UpdateSense = async (entryId: string, senseId: string, update: JsonOperation[]): Promise<Sense> => {
        return await this.connection.invoke("UpdateSense", entryId, senseId, update);
    }

    public readonly DeleteSense = async (entryId: string, senseId: string): Promise<void> => {
        return await this.connection.invoke("DeleteSense", entryId, senseId);
    }

    public readonly CreateExampleSentence = async (entryId: string, senseId: string, exampleSentence: ExampleSentence): Promise<ExampleSentence> => {
        return await this.connection.invoke("CreateExampleSentence", entryId, senseId, exampleSentence);
    }

    public readonly UpdateExampleSentence = async (entryId: string, senseId: string, exampleSentenceId: string, update: JsonOperation[]): Promise<ExampleSentence> => {
        return await this.connection.invoke("UpdateExampleSentence", entryId, senseId, exampleSentenceId, update);
    }

    public readonly DeleteExampleSentence = async (entryId: string, senseId: string, exampleSentenceId: string): Promise<void> => {
        return await this.connection.invoke("DeleteExampleSentence", entryId, senseId, exampleSentenceId);
    }
}


// Receiver

class ILexboxClient_Binder implements ReceiverRegister<ILexboxClient> {

    public static Instance = new ILexboxClient_Binder();

    private constructor() {
    }

    public readonly register = (connection: HubConnection, receiver: ILexboxClient): Disposable => {

        const __onEntryUpdated = (...args: Parameters<ILexboxClient['OnEntryUpdated']>) => receiver.OnEntryUpdated(...args);
        const __onProjectClosed = (...args: Parameters<ILexboxClient['OnProjectClosed']>) => receiver.OnProjectClosed(...args);

        connection.on("OnEntryUpdated", __onEntryUpdated);
        connection.on("OnProjectClosed", __onProjectClosed);

        const methodList: ReceiverMethod[] = [
            { methodName: "OnEntryUpdated", method: __onEntryUpdated },
            { methodName: "OnProjectClosed", method: __onProjectClosed },
        ]

        return new ReceiverMethodSubscription(connection, methodList);
    }
}

