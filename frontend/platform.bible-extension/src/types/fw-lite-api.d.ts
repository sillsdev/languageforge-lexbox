import type * as dotnetTypes from '../../../viewer/src/lib/dotnet-types';
import type { IHistoryLineItem } from '../../../viewer/src/lib/dotnet-types/generated-types/LcmCrdt/IHistoryLineItem';
import type { IProjectActivity } from '../../../viewer/src/lib/dotnet-types/generated-types/LcmCrdt/IProjectActivity';
import type { ProjectRole as PR } from '../../../viewer/src/lib/dotnet-types/generated-types/LexCore/Entities/ProjectRole';
import type { ICommitMetadata } from '../../../viewer/src/lib/dotnet-types/generated-types/SIL/Harmony/Core/ICommitMetadata';
import type { IObjectSnapshot } from '../../../viewer/src/lib/dotnet-types/generated-types/SIL/Harmony/Db/IObjectSnapshot';

declare module 'fw-lite-api' {
  /** Return types used in the FwLiteWeb/MiniLcm API. */
  //export type Commit =
  export type CommitMetadata = ICommitMetadata;
  export type ComplexFormComponent = dotnetTypes.IComplexFormComponent;
  export type ComplexFormType = dotnetTypes.IComplexFormType;
  //export type DateTimeOffsetInt64GuidValueTuple =
  export type Entry = dotnetTypes.IEntry;
  export type ExampleSentence = dotnetTypes.IExampleSentence;
  export type HistoryLineItem = IHistoryLineItem;
  //export type HybridDateTime =
  //export type IChange =
  //export type IChangeChangeEntity =
  //export type IObjectBase =
  export type IObjectWithId = dotnetTypes.IObjectWithId;
  export type IProjectIdentifier = dotnetTypes.IProjectIdentifier;
  export type LexboxServer = dotnetTypes.ILexboxServer;
  export type ObjectSnapshot = IObjectSnapshot;
  export type PartOfSpeech = dotnetTypes.IPartOfSpeech;
  export type ProjectActivity = IProjectActivity;
  export type ProjectDataFormat = dotnetTypes.ProjectDataFormat;
  export type ProjectModel = dotnetTypes.IProjectModel;
  export type ProjectRole = PR;
  export type Publication = dotnetTypes.IPublication;
  export type RichSpan = dotnetTypes.IRichSpan;
  export type RichString = dotnetTypes.IRichString;
  export type SemanticDomain = dotnetTypes.ISemanticDomain;
  export type Sense = dotnetTypes.ISense;
  export type ServerStatus = dotnetTypes.IServerStatus;
  export type SortField = dotnetTypes.SortField;
  export type WritingSystem = dotnetTypes.IWritingSystem;
  //export type WritingSystemId = // string
  export type WritingSystemType = dotnetTypes.WritingSystemType;
  export type WritingSystems = dotnetTypes.IWritingSystems;
}
