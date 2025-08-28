create table __EFMigrationsHistory
(
    MigrationId    TEXT not null
        constraint PK___EFMigrationsHistory
            primary key,
    ProductVersion TEXT not null
);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250115085006_InitialCreate', '8.0.11');


create table ProjectData
(
    Id           TEXT not null
        constraint PK_ProjectData
            primary key,
    Name         TEXT not null,
    OriginDomain TEXT,
    ClientId     TEXT not null,
    FwProjectId  TEXT
);

INSERT INTO ProjectData (Id, Name, OriginDomain, ClientId, FwProjectId) VALUES ('CDEF21EF-A517-42B8-B555-590CAD1B10E8', 'OpeningAProjectWorks', null, '9C38FBC5-6B06-400C-9D53-28BE30015763', null);


create table Commits
(
    Id         TEXT    not null
        constraint PK_Commits
            primary key,
    Hash       TEXT    not null,
    ParentHash TEXT    not null,
    Counter    INTEGER not null,
    DateTime   TEXT    not null,
    Metadata   jsonb   not null,
    ClientId   TEXT    not null
);

INSERT INTO Commits (Id, Hash, ParentHash, Counter, DateTime, Metadata, ClientId) VALUES ('DC60D2A9-0CC2-48ED-803C-A238A14B6EAE', 'DBBDEACCCD3C4FF6', '0000', 0, '2025-03-04 04:03:50.959658', '{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}', '9C38FBC5-6B06-400C-9D53-28BE30015763');
INSERT INTO Commits (Id, Hash, ParentHash, Counter, DateTime, Metadata, ClientId) VALUES ('023FAEBB-711B-4D2F-B34F-A15621FC66BB', '3D9DDAC54A23C3F9', 'DBBDEACCCD3C4FF6', 0, '2025-03-04 04:03:51.2393345', '{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}', '9C38FBC5-6B06-400C-9D53-28BE30015763');
INSERT INTO Commits (Id, Hash, ParentHash, Counter, DateTime, Metadata, ClientId) VALUES ('023FAEBB-711B-4D2F-A14F-A15621FC66BC', '998AF2D62BB93C3C', '3D9DDAC54A23C3F9', 0, '2025-03-04 04:03:51.2839521', '{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}', '9C38FBC5-6B06-400C-9D53-28BE30015763');
INSERT INTO Commits (Id, Hash, ParentHash, Counter, DateTime, Metadata, ClientId) VALUES ('0E5CA1D3-7502-43EC-B03D-FA83899E0972', '3A7F4CF289166784', '998AF2D62BB93C3C', 0, '2025-03-04 04:03:51.4046875', '{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}', '9C38FBC5-6B06-400C-9D53-28BE30015763');
INSERT INTO Commits (Id, Hash, ParentHash, Counter, DateTime, Metadata, ClientId) VALUES ('B9EE0129-FE8A-48D4-95DB-32548DF39267', '97FB860EE3A8CF33', '3A7F4CF289166784', 0, '2025-03-04 04:03:52.0424432', '{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}', '9C38FBC5-6B06-400C-9D53-28BE30015763');
INSERT INTO Commits (Id, Hash, ParentHash, Counter, DateTime, Metadata, ClientId) VALUES ('EDF0C210-1D19-49AA-977C-1C2B99C05A05', 'B7B135F650C09EA9', '97FB860EE3A8CF33', 0, '2025-03-04 04:03:52.136871', '{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}', '9C38FBC5-6B06-400C-9D53-28BE30015763');


create table ChangeEntities
(
    "Index"  INTEGER not null,
    CommitId TEXT    not null
        constraint FK_ChangeEntities_Commits_CommitId
            references Commits
            on delete cascade,
    EntityId TEXT    not null,
    Change   jsonb,
    constraint PK_ChangeEntities
        primary key (CommitId, "Index")
);

INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (0, 'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE', 'C36F55ED-D1EA-4069-90B3-3F35FF696273', '{"$type":"CreateComplexFormType","Name":{"en":"Compound"},"EntityId":"c36f55ed-d1ea-4069-90b3-3f35ff696273"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (1, 'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE', 'EEB78FCE-6009-4932-AAA6-85FAEB180C69', '{"$type":"CreateComplexFormType","Name":{"en":"Unspecified"},"EntityId":"eeb78fce-6009-4932-aaa6-85faeb180c69"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (0, '023FAEBB-711B-4D2F-B34F-A15621FC66BB', '46E4FE08-FFA0-4C8B-BF98-2C56F38904D9', '{"$type":"CreatePartOfSpeechChange","Name":{"en":"Adverb"},"Predefined":true,"EntityId":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D0', '{"$type":"CreateSemanticDomainChange","Name":{"en":"Universe, Creation"},"Predefined":true,"Code":"1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d0"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (1, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D1', '{"$type":"CreateSemanticDomainChange","Name":{"en":"Sky"},"Predefined":true,"Code":"1.1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d1"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (2, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D2', '{"$type":"CreateSemanticDomainChange","Name":{"en":"World"},"Predefined":true,"Code":"1.2","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d2"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (3, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D3', '{"$type":"CreateSemanticDomainChange","Name":{"en":"Person"},"Predefined":true,"Code":"2","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d3"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (4, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D4', '{"$type":"CreateSemanticDomainChange","Name":{"en":"Body"},"Predefined":true,"Code":"2.1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d4"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (5, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D5', '{"$type":"CreateSemanticDomainChange","Name":{"en":"Head"},"Predefined":true,"Code":"2.1.1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d5"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (6, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D6', '{"$type":"CreateSemanticDomainChange","Name":{"en":"Eye"},"Predefined":true,"Code":"2.1.1.1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d6"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (0, '0E5CA1D3-7502-43EC-B03D-FA83899E0972', 'B802E01F-FAC1-42B8-B605-A5BBB7EFCDC1', '{"$type":"CreateEntryChange","LexemeForm":{"en":"Apple"},"CitationForm":{"en":"Apple"},"LiteralMeaning":{"en":"Fruit"},"Note":{},"EntityId":"b802e01f-fac1-42b8-b605-a5bbb7efcdc1"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (1, '0E5CA1D3-7502-43EC-B03D-FA83899E0972', 'D510AA82-5557-4DD4-8B1F-1EC89FACB979', '{"$type":"CreateSenseChange","EntryId":"b802e01f-fac1-42b8-b605-a5bbb7efcdc1","Order":1,"Definition":{"en":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh"},"Gloss":{"en":"Fruit"},"PartOfSpeech":"","PartOfSpeechId":null,"SemanticDomains":[],"EntityId":"d510aa82-5557-4dd4-8b1f-1ec89facb979"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (2, '0E5CA1D3-7502-43EC-B03D-FA83899E0972', '30A0DCC4-06B1-4402-B78E-745712C4CD2D', '{"$type":"CreateExampleSentenceChange","SenseId":"d510aa82-5557-4dd4-8b1f-1ec89facb979","Order":1,"Sentence":{"en":"We ate an apple"},"Translation":{},"Reference":null,"EntityId":"30a0dcc4-06b1-4402-b78e-745712c4cd2d"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (0, 'B9EE0129-FE8A-48D4-95DB-32548DF39267', 'E8B2B514-E1EE-4A2B-9B52-6E32291AA407', '{"$type":"CreateWritingSystemChange","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":0,"Order":0,"EntityId":"e8b2b514-e1ee-4a2b-9b52-6e32291aa407"}');
INSERT INTO ChangeEntities ("Index", CommitId, EntityId, Change) VALUES (0, 'EDF0C210-1D19-49AA-977C-1C2B99C05A05', '1D6F788D-B5A7-4DD8-BDBC-E10B6AC78EF7', '{"$type":"CreateWritingSystemChange","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":1,"Order":0,"EntityId":"1d6f788d-b5a7-4dd8-bdbc-e10b6ac78ef7"}');


create table Snapshots
(
    Id              TEXT    not null
        constraint PK_Snapshots
            primary key,
    TypeName        TEXT    not null,
    Entity          jsonb   not null,
    "References"    TEXT    not null,
    EntityId        TEXT    not null,
    EntityIsDeleted INTEGER not null,
    CommitId        TEXT    not null
        constraint FK_Snapshots_Commits_CommitId
            references Commits
            on delete cascade,
    IsRoot          INTEGER not null
);

create unique index IX_Snapshots_CommitId_EntityId
    on Snapshots (CommitId, EntityId);

INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('C72418FD-3344-493B-B1B5-3BD5846F368D', 'ComplexFormType', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ComplexFormType","Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null},"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","DeletedAt":null,"DbObject":{"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null}}', '[]', 'C36F55ED-D1EA-4069-90B3-3F35FF696273', 0, 'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('F0A3CFE7-7F4B-4B34-9D3E-24C89ED202EB', 'ComplexFormType', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ComplexFormType","Id":"eeb78fce-6009-4932-aaa6-85faeb180c69","Name":{"en":"Unspecified"},"DeletedAt":null},"Id":"eeb78fce-6009-4932-aaa6-85faeb180c69","DeletedAt":null,"DbObject":{"Id":"eeb78fce-6009-4932-aaa6-85faeb180c69","Name":{"en":"Unspecified"},"DeletedAt":null}}', '[]', 'EEB78FCE-6009-4932-AAA6-85FAEB180C69', 0, 'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('5F0470E1-16E3-4A0A-8565-B0077B6F4AC4', 'PartOfSpeech', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"PartOfSpeech","Id":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","Name":{"en":"Adverb"},"DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","Name":{"en":"Adverb"},"DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF98-2C56F38904D9', 0, '023FAEBB-711B-4D2F-B34F-A15621FC66BB', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('624480D1-941C-458F-BDD2-704500605784', 'SemanticDomain', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d5","Name":{"en":"Head"},"Code":"2.1.1","DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d5","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d5","Name":{"en":"Head"},"Code":"2.1.1","DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D5', 0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('6CCB66B0-9670-4235-8270-0A1DF297C10A', 'SemanticDomain', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d0","Name":{"en":"Universe, Creation"},"Code":"1","DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d0","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d0","Name":{"en":"Universe, Creation"},"Code":"1","DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D0', 0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('7BE5BED0-D04C-461D-B788-65A40D73D091', 'SemanticDomain', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d3","Name":{"en":"Person"},"Code":"2","DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d3","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d3","Name":{"en":"Person"},"Code":"2","DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D3', 0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('82B1727D-516F-46F3-8C02-6E26044F8835', 'SemanticDomain', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d1","Name":{"en":"Sky"},"Code":"1.1","DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d1","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d1","Name":{"en":"Sky"},"Code":"1.1","DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D1', 0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('845DCD67-8D87-42A2-9D70-465E2D53DD98', 'SemanticDomain', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d6","Name":{"en":"Eye"},"Code":"2.1.1.1","DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d6","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d6","Name":{"en":"Eye"},"Code":"2.1.1.1","DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D6', 0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('99DF0598-795D-4EFB-B4E6-2117C0EB82BA', 'SemanticDomain', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d2","Name":{"en":"World"},"Code":"1.2","DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d2","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d2","Name":{"en":"World"},"Code":"1.2","DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D2', 0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('B42BD81F-29D0-483A-94A4-C5ACC27D10D4', 'SemanticDomain', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d4","Name":{"en":"Body"},"Code":"2.1","DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d4","DeletedAt":null,"DbObject":{"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d4","Name":{"en":"Body"},"Code":"2.1","DeletedAt":null,"Predefined":true}}', '[]', '46E4FE08-FFA0-4C8B-BF88-2C56F38904D4', 0, '023FAEBB-711B-4D2F-A14F-A15621FC66BC', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('2EBE5828-D6F6-4000-82E2-8B127E06C5E7', 'Sense', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"d510aa82-5557-4dd4-8b1f-1ec89facb979","Order":1,"DeletedAt":null,"EntryId":"b802e01f-fac1-42b8-b605-a5bbb7efcdc1","Definition":{"en":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh"},"Gloss":{"en":"Fruit"},"PartOfSpeech":"","PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]},"Id":"d510aa82-5557-4dd4-8b1f-1ec89facb979","DeletedAt":null,"DbObject":{"Id":"d510aa82-5557-4dd4-8b1f-1ec89facb979","Order":1,"DeletedAt":null,"EntryId":"b802e01f-fac1-42b8-b605-a5bbb7efcdc1","Definition":{"en":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh"},"Gloss":{"en":"Fruit"},"PartOfSpeech":"","PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]}}', '["B802E01F-FAC1-42B8-B605-A5BBB7EFCDC1"]', 'D510AA82-5557-4DD4-8B1F-1EC89FACB979', 0, '0E5CA1D3-7502-43EC-B03D-FA83899E0972', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('55EC97C3-A7D7-416B-B30A-6A242A71CDAF', 'Entry', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"b802e01f-fac1-42b8-b605-a5bbb7efcdc1","DeletedAt":null,"LexemeForm":{"en":"Apple"},"CitationForm":{"en":"Apple"},"LiteralMeaning":{"en":"Fruit"},"Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[]},"Id":"b802e01f-fac1-42b8-b605-a5bbb7efcdc1","DeletedAt":null,"DbObject":{"Id":"b802e01f-fac1-42b8-b605-a5bbb7efcdc1","DeletedAt":null,"LexemeForm":{"en":"Apple"},"CitationForm":{"en":"Apple"},"LiteralMeaning":{"en":"Fruit"},"Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[]}}', '[]', 'B802E01F-FAC1-42B8-B605-A5BBB7EFCDC1', 0, '0E5CA1D3-7502-43EC-B03D-FA83899E0972', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('5BC2C0B3-A1C8-4D81-BFEF-FBE2153CF1C9', 'ExampleSentence', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"30a0dcc4-06b1-4402-b78e-745712c4cd2d","Order":1,"Sentence":{"en":"We ate an apple"},"Translation":{},"Reference":null,"SenseId":"d510aa82-5557-4dd4-8b1f-1ec89facb979","DeletedAt":null},"Id":"30a0dcc4-06b1-4402-b78e-745712c4cd2d","DeletedAt":null,"DbObject":{"Id":"30a0dcc4-06b1-4402-b78e-745712c4cd2d","Order":1,"Sentence":{"en":"We ate an apple"},"Translation":{},"Reference":null,"SenseId":"d510aa82-5557-4dd4-8b1f-1ec89facb979","DeletedAt":null}}', '["D510AA82-5557-4DD4-8B1F-1EC89FACB979"]', '30A0DCC4-06B1-4402-B78E-745712C4CD2D', 0, '0E5CA1D3-7502-43EC-B03D-FA83899E0972', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('33D349FA-D518-4875-916D-EDFA17FA5F22', 'WritingSystem', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"e8b2b514-e1ee-4a2b-9b52-6e32291aa407","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","DeletedAt":null,"Type":0,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":0},"Id":"e8b2b514-e1ee-4a2b-9b52-6e32291aa407","DeletedAt":null,"DbObject":{"Id":"e8b2b514-e1ee-4a2b-9b52-6e32291aa407","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","DeletedAt":null,"Type":0,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":0}}', '[]', 'E8B2B514-E1EE-4A2B-9B52-6E32291AA407', 0, 'B9EE0129-FE8A-48D4-95DB-32548DF39267', 1);
INSERT INTO Snapshots (Id, TypeName, Entity, "References", EntityId, EntityIsDeleted, CommitId, IsRoot) VALUES ('799BC576-E362-4DDA-AD07-75EBCC9FF71A', 'WritingSystem', '{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"1d6f788d-b5a7-4dd8-bdbc-e10b6ac78ef7","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","DeletedAt":null,"Type":1,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":0},"Id":"1d6f788d-b5a7-4dd8-bdbc-e10b6ac78ef7","DeletedAt":null,"DbObject":{"Id":"1d6f788d-b5a7-4dd8-bdbc-e10b6ac78ef7","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","DeletedAt":null,"Type":1,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":0}}', '[]', '1D6F788D-B5A7-4DD8-BDBC-E10B6AC78EF7', 0, 'EDF0C210-1D19-49AA-977C-1C2B99C05A05', 1);


create table WritingSystem
(
    Id           TEXT    not null
        constraint PK_WritingSystem
            primary key,
    WsId         TEXT    not null,
    Name         TEXT    not null,
    Abbreviation TEXT    not null,
    Font         TEXT    not null,
    DeletedAt    TEXT,
    Type         INTEGER not null,
    Exemplars    jsonb   not null,
    "Order"      REAL    not null,
    SnapshotId   TEXT
        constraint FK_WritingSystem_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create unique index IX_WritingSystem_SnapshotId
    on WritingSystem (SnapshotId);

create unique index IX_WritingSystem_WsId_Type
    on WritingSystem (WsId, Type);

INSERT INTO WritingSystem (Id, WsId, Name, Abbreviation, Font, DeletedAt, Type, Exemplars, "Order", SnapshotId) VALUES ('E8B2B514-E1EE-4A2B-9B52-6E32291AA407', 'en', 'English', 'en', 'Arial', null, 0, '["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]', 0, '33D349FA-D518-4875-916D-EDFA17FA5F22');
INSERT INTO WritingSystem (Id, WsId, Name, Abbreviation, Font, DeletedAt, Type, Exemplars, "Order", SnapshotId) VALUES ('1D6F788D-B5A7-4DD8-BDBC-E10B6AC78EF7', 'en', 'English', 'en', 'Arial', null, 1, '["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]', 0, '799BC576-E362-4DDA-AD07-75EBCC9FF71A');


create table Entry
(
    Id               TEXT  not null
        constraint PK_Entry
            primary key,
    DeletedAt        TEXT,
    LexemeForm       jsonb not null,
    CitationForm     jsonb not null,
    LiteralMeaning   jsonb not null,
    Note             jsonb not null,
    ComplexFormTypes jsonb not null,
    SnapshotId       TEXT
        constraint FK_Entry_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create unique index IX_Entry_SnapshotId
    on Entry (SnapshotId);

INSERT INTO Entry (Id, DeletedAt, LexemeForm, CitationForm, LiteralMeaning, Note, ComplexFormTypes, SnapshotId) VALUES ('B802E01F-FAC1-42B8-B605-A5BBB7EFCDC1', null, '{"en":"Apple"}', '{"en":"Apple"}', '{"en":"Fruit"}', '{}', '[]', '55EC97C3-A7D7-416B-B30A-6A242A71CDAF');


create table ComplexFormType
(
    Id         TEXT  not null
        constraint PK_ComplexFormType
            primary key,
    Name       jsonb not null,
    DeletedAt  TEXT,
    SnapshotId TEXT
        constraint FK_ComplexFormType_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create unique index IX_ComplexFormType_SnapshotId
    on ComplexFormType (SnapshotId);

INSERT INTO ComplexFormType (Id, Name, DeletedAt, SnapshotId) VALUES ('C36F55ED-D1EA-4069-90B3-3F35FF696273', '{"en":"Compound"}', null, 'C72418FD-3344-493B-B1B5-3BD5846F368D');
INSERT INTO ComplexFormType (Id, Name, DeletedAt, SnapshotId) VALUES ('EEB78FCE-6009-4932-AAA6-85FAEB180C69', '{"en":"Unspecified"}', null, 'F0A3CFE7-7F4B-4B34-9D3E-24C89ED202EB');


create table Sense
(
    Id              TEXT  not null
        constraint PK_Sense
            primary key,
    "Order"         REAL  not null,
    DeletedAt       TEXT,
    EntryId         TEXT  not null
        constraint FK_Sense_Entry_EntryId
            references Entry
            on delete cascade,
    Definition      jsonb not null,
    Gloss           jsonb not null,
    PartOfSpeech    TEXT  not null,
    PartOfSpeechId  TEXT,
    SemanticDomains jsonb not null,
    SnapshotId      TEXT
        constraint FK_Sense_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create index IX_Sense_EntryId
    on Sense (EntryId);

create unique index IX_Sense_SnapshotId
    on Sense (SnapshotId);

INSERT INTO Sense (Id, "Order", DeletedAt, EntryId, Definition, Gloss, PartOfSpeech, PartOfSpeechId, SemanticDomains, SnapshotId) VALUES ('D510AA82-5557-4DD4-8B1F-1EC89FACB979', 1, null, 'B802E01F-FAC1-42B8-B605-A5BBB7EFCDC1', '{"en":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh"}', '{"en":"Fruit"}', '', null, '[]', '2EBE5828-D6F6-4000-82E2-8B127E06C5E7');


create table ComplexFormComponents
(
    Id                  TEXT not null
        constraint PK_ComplexFormComponents
            primary key,
    DeletedAt           TEXT,
    ComplexFormEntryId  TEXT not null
        constraint FK_ComplexFormComponents_Entry_ComplexFormEntryId
            references Entry
            on delete cascade,
    ComplexFormHeadword TEXT,
    ComponentEntryId    TEXT not null
        constraint FK_ComplexFormComponents_Entry_ComponentEntryId
            references Entry
            on delete cascade,
    ComponentSenseId    TEXT
        constraint FK_ComplexFormComponents_Sense_ComponentSenseId
            references Sense
            on delete cascade,
    ComponentHeadword   TEXT,
    SnapshotId          TEXT
        constraint FK_ComplexFormComponents_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create unique index IX_ComplexFormComponents_ComplexFormEntryId_ComponentEntryId
    on ComplexFormComponents (ComplexFormEntryId, ComponentEntryId)
    where ComponentSenseId IS NULL;

create unique index IX_ComplexFormComponents_ComplexFormEntryId_ComponentEntryId_ComponentSenseId
    on ComplexFormComponents (ComplexFormEntryId, ComponentEntryId, ComponentSenseId)
    where ComponentSenseId IS NOT NULL;

create index IX_ComplexFormComponents_ComponentEntryId
    on ComplexFormComponents (ComponentEntryId);

create index IX_ComplexFormComponents_ComponentSenseId
    on ComplexFormComponents (ComponentSenseId);

create unique index IX_ComplexFormComponents_SnapshotId
    on ComplexFormComponents (SnapshotId);


create table PartOfSpeech
(
    Id         TEXT    not null
        constraint PK_PartOfSpeech
            primary key,
    Name       jsonb   not null,
    DeletedAt  TEXT,
    Predefined INTEGER not null,
    SnapshotId TEXT
        constraint FK_PartOfSpeech_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create unique index IX_PartOfSpeech_SnapshotId
    on PartOfSpeech (SnapshotId);

INSERT INTO PartOfSpeech (Id, Name, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF98-2C56F38904D9', '{"en":"Adverb"}', null, 1, '5F0470E1-16E3-4A0A-8565-B0077B6F4AC4');


create table SemanticDomain
(
    Id         TEXT    not null
        constraint PK_SemanticDomain
            primary key,
    Name       jsonb   not null,
    Code       TEXT    not null,
    DeletedAt  TEXT,
    Predefined INTEGER not null,
    SnapshotId TEXT
        constraint FK_SemanticDomain_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create unique index IX_SemanticDomain_SnapshotId
    on SemanticDomain (SnapshotId);

INSERT INTO SemanticDomain (Id, Name, Code, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF88-2C56F38904D0', '{"en":"Universe, Creation"}', '1', null, 1, '6CCB66B0-9670-4235-8270-0A1DF297C10A');
INSERT INTO SemanticDomain (Id, Name, Code, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF88-2C56F38904D1', '{"en":"Sky"}', '1.1', null, 1, '82B1727D-516F-46F3-8C02-6E26044F8835');
INSERT INTO SemanticDomain (Id, Name, Code, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF88-2C56F38904D2', '{"en":"World"}', '1.2', null, 1, '99DF0598-795D-4EFB-B4E6-2117C0EB82BA');
INSERT INTO SemanticDomain (Id, Name, Code, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF88-2C56F38904D3', '{"en":"Person"}', '2', null, 1, '7BE5BED0-D04C-461D-B788-65A40D73D091');
INSERT INTO SemanticDomain (Id, Name, Code, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF88-2C56F38904D4', '{"en":"Body"}', '2.1', null, 1, 'B42BD81F-29D0-483A-94A4-C5ACC27D10D4');
INSERT INTO SemanticDomain (Id, Name, Code, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF88-2C56F38904D5', '{"en":"Head"}', '2.1.1', null, 1, '624480D1-941C-458F-BDD2-704500605784');
INSERT INTO SemanticDomain (Id, Name, Code, DeletedAt, Predefined, SnapshotId) VALUES ('46E4FE08-FFA0-4C8B-BF88-2C56F38904D6', '{"en":"Eye"}', '2.1.1.1', null, 1, '845DCD67-8D87-42A2-9D70-465E2D53DD98');


create table ExampleSentence
(
    Id          TEXT  not null
        constraint PK_ExampleSentence
            primary key,
    "Order"     REAL  not null,
    Sentence    jsonb not null,
    Translation jsonb not null,
    Reference   TEXT,
    SenseId     TEXT  not null
        constraint FK_ExampleSentence_Sense_SenseId
            references Sense
            on delete cascade,
    DeletedAt   TEXT,
    SnapshotId  TEXT
        constraint FK_ExampleSentence_Snapshots_SnapshotId
            references Snapshots
            on delete set null
);

create index IX_ExampleSentence_SenseId
    on ExampleSentence (SenseId);

create unique index IX_ExampleSentence_SnapshotId
    on ExampleSentence (SnapshotId);

INSERT INTO ExampleSentence (Id, "Order", Sentence, Translation, Reference, SenseId, DeletedAt, SnapshotId) VALUES ('30A0DCC4-06B1-4402-B78E-745712C4CD2D', 1, '{"en":"We ate an apple"}', '{}', null, 'D510AA82-5557-4DD4-8B1F-1EC89FACB979', null, '5BC2C0B3-A1C8-4D81-BFEF-FBE2153CF1C9');
