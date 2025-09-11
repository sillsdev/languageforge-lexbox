PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (

    "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,

    "Timestamp" TEXT NOT NULL

);
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (

    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,

    "ProductVersion" TEXT NOT NULL

);
INSERT INTO __EFMigrationsHistory VALUES('20250115085006_InitialCreate','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250115143740_PartOfSpeechNowObject','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250115153509_Add Order to Complex Form Components','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250120033055_AddProjectDataLastUserInfo','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250225100659_AddPublications','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250306152556_AddCodeToProjectData','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250415063756_AddHarmonyUpdate','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250526075433_MigrateReferenceColumn','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250530084107_AddProjectDataRole','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250609090931_AddEntrySearchTable','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250709092804_AddResources','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250806084710_AddMorphTypes','9.0.6');
INSERT INTO __EFMigrationsHistory VALUES('20250814034436_SetPosIdNullOnDelete','9.0.6');
CREATE TABLE IF NOT EXISTS "Commits" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_Commits" PRIMARY KEY,

    "Hash" TEXT NOT NULL,

    "ParentHash" TEXT NOT NULL,

    "Counter" INTEGER NOT NULL,

    "DateTime" TEXT NOT NULL,

    "Metadata" jsonb NOT NULL,

    "ClientId" TEXT NOT NULL

);
INSERT INTO Commits VALUES('DC60D2A9-0CC2-48ED-803C-A238A14B6EAE','DBBDEACCCD3C4FF6','0000',0,'2025-09-11 04:40:42.5375312','{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('023FAEBB-711B-4D2F-B34F-A15621FC66BB','3D9DDAC54A23C3F9','DBBDEACCCD3C4FF6',0,'2025-09-11 04:40:42.7714242','{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('023FAEBB-711B-4D2F-A14F-A15621FC66BC','998AF2D62BB93C3C','3D9DDAC54A23C3F9',0,'2025-09-11 04:40:42.8070906','{"AuthorName":null,"AuthorId":null,"ClientVersion":null,"ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('B76CB8F9-4854-4E93-AF3C-1C2EEC00E216','1A237AAD83E03371','998AF2D62BB93C3C',0,'2025-09-11 04:40:42.8488206','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('D341A388-62F7-41C2-AF50-2854B2ED941F','1EFCEA3A5BA963EE','1A237AAD83E03371',0,'2025-09-11 04:40:42.9130875','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('2C649B96-4ABA-4D4D-9CCA-3670D4469210','E8F0E82971EE0CC4','1EFCEA3A5BA963EE',0,'2025-09-11 04:40:42.9263159','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('6917532B-C05C-481E-BED7-A0D075822E89','6982446BFA81DCB5','E8F0E82971EE0CC4',0,'2025-09-11 04:40:42.9338667','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('6113B384-911C-4A3C-A8CC-85F36BB92106','2D64A91EE86B4686','6982446BFA81DCB5',0,'2025-09-11 04:40:42.9409786','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('98CAC228-5353-4BD2-811F-E11119AC8694','F823069D7CC2EF02','2D64A91EE86B4686',0,'2025-09-11 04:40:42.9491801','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('6A5981B6-D840-44CF-ADF2-D51D4908314B','3A4690D55ACE201A','F823069D7CC2EF02',0,'2025-09-11 04:40:42.9687415','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('A867FF78-BA45-41FB-986B-6FFAF8E4D5B5','CAC40ACE42202EAA','3A4690D55ACE201A',0,'2025-09-11 04:40:43.3549441','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('191B4774-F571-4E57-8639-D215106E8ABF','3AAB167808CD32C7','CAC40ACE42202EAA',0,'2025-09-11 04:40:43.3713259','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('CD77D4CB-FBB6-4004-9159-F7F28482CA75','DB9C6BD7E0C77F29','3AAB167808CD32C7',0,'2025-09-11 04:40:43.383049','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('5DBA93AF-7604-410F-B109-EC4961BB608F','A560F9ADCFB6C303','DB9C6BD7E0C77F29',0,'2025-09-11 04:41:10.4780385','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('64308E87-7710-48FE-B4D8-2FA131F2456D','75A589E1E908EF6B','A560F9ADCFB6C303',0,'2025-09-11 04:41:12.4029032','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('276EF0D5-677C-46AA-AF7E-D518ABBA15B5','2F7476D19CDB2077','75A589E1E908EF6B',0,'2025-09-11 04:41:22.058834','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('4E078D12-BA55-42A8-8772-F0DB0891FA71','1EE26E7C69C2F5F3','2F7476D19CDB2077',0,'2025-09-11 04:41:24.672275','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('DF118748-6170-40FA-A38F-7ADDF0AA4B4D','A65F0FC4CB76A580','1EE26E7C69C2F5F3',0,'2025-09-11 04:41:29.8914754','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('2F476F0D-BAAE-4428-A534-2447A3E6338C','780F1840B75CB9F9','A65F0FC4CB76A580',0,'2025-09-11 04:41:31.7484795','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('6C1E0DC4-E30B-43DC-889C-DC5787FBF282','A29F9A03435C098B','780F1840B75CB9F9',0,'2025-09-11 04:41:34.4277594','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('241F2698-C899-4476-986B-B339FA50FCE8','9CC759185DF95724','A29F9A03435C098B',0,'2025-09-11 04:41:38.2877384','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('16F056B3-C19A-42D0-A824-BA2640597AC2','04B4308D308C6166','9CC759185DF95724',0,'2025-09-11 04:41:39.5996003','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('C2C3E5CC-4E0A-4E3D-A300-D004DACB5A7E','A1C0623BB8367B51','04B4308D308C6166',0,'2025-09-11 04:41:43.4740918','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('5AAC4FF6-0DD8-4266-875C-00F1800EFAD4','B5098096A23C7B07','A1C0623BB8367B51',0,'2025-09-11 04:41:46.1064111','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('5B954C9F-C86F-463A-BAA2-15EBB7A6074B','56F7E637ED4D61B6','B5098096A23C7B07',0,'2025-09-11 04:41:48.6612136','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Commits VALUES('BAF9DA29-2187-4D2E-9C67-92C295C3FBD7','E412344CF0C2090F','56F7E637ED4D61B6',0,'2025-09-11 04:41:51.1824641','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
CREATE TABLE IF NOT EXISTS "ChangeEntities" (

    "Index" INTEGER NOT NULL,

    "CommitId" TEXT NOT NULL,

    "EntityId" TEXT NOT NULL,

    "Change" jsonb NULL,

    CONSTRAINT "PK_ChangeEntities" PRIMARY KEY ("CommitId", "Index"),

    CONSTRAINT "FK_ChangeEntities_Commits_CommitId" FOREIGN KEY ("CommitId") REFERENCES "Commits" ("Id") ON DELETE CASCADE

);
INSERT INTO ChangeEntities VALUES(0,'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE','C36F55ED-D1EA-4069-90B3-3F35FF696273','{"$type":"CreateComplexFormType","Name":{"en":"Compound"},"EntityId":"c36f55ed-d1ea-4069-90b3-3f35ff696273"}');
INSERT INTO ChangeEntities VALUES(1,'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE','EEB78FCE-6009-4932-AAA6-85FAEB180C69','{"$type":"CreateComplexFormType","Name":{"en":"Unspecified"},"EntityId":"eeb78fce-6009-4932-aaa6-85faeb180c69"}');
INSERT INTO ChangeEntities VALUES(0,'023FAEBB-711B-4D2F-B34F-A15621FC66BB','46E4FE08-FFA0-4C8B-BF98-2C56F38904D9','{"$type":"CreatePartOfSpeechChange","Name":{"en":"Adverb"},"Predefined":true,"EntityId":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9"}');
INSERT INTO ChangeEntities VALUES(0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC','63403699-07C1-43F3-A47C-069D6E4316E5','{"$type":"CreateSemanticDomainChange","Name":{"en":"Universe, Creation"},"Predefined":true,"Code":"1","EntityId":"63403699-07c1-43f3-a47c-069d6e4316e5"}');
INSERT INTO ChangeEntities VALUES(1,'023FAEBB-711B-4D2F-A14F-A15621FC66BC','999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C','{"$type":"CreateSemanticDomainChange","Name":{"en":"Sky"},"Predefined":true,"Code":"1.1","EntityId":"999581c4-1611-4acb-ae1b-5e6c1dfe6f0c"}');
INSERT INTO ChangeEntities VALUES(2,'023FAEBB-711B-4D2F-A14F-A15621FC66BC','DC1A2C6F-1B32-4631-8823-36DACC8CB7BB','{"$type":"CreateSemanticDomainChange","Name":{"en":"World"},"Predefined":true,"Code":"1.2","EntityId":"dc1a2c6f-1b32-4631-8823-36dacc8cb7bb"}');
INSERT INTO ChangeEntities VALUES(3,'023FAEBB-711B-4D2F-A14F-A15621FC66BC','1BD42665-0610-4442-8D8D-7C666FEE3A6D','{"$type":"CreateSemanticDomainChange","Name":{"en":"Person"},"Predefined":true,"Code":"2","EntityId":"1bd42665-0610-4442-8d8d-7c666fee3a6d"}');
INSERT INTO ChangeEntities VALUES(4,'023FAEBB-711B-4D2F-A14F-A15621FC66BC','46E4FE08-FFA0-4C8B-BF88-2C56F38904D4','{"$type":"CreateSemanticDomainChange","Name":{"en":"Body"},"Predefined":false,"Code":"2.1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d4"}');
INSERT INTO ChangeEntities VALUES(5,'023FAEBB-711B-4D2F-A14F-A15621FC66BC','46E4FE08-FFA0-4C8B-BF88-2C56F38904D5','{"$type":"CreateSemanticDomainChange","Name":{"en":"Head"},"Predefined":false,"Code":"2.1.1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d5"}');
INSERT INTO ChangeEntities VALUES(6,'023FAEBB-711B-4D2F-A14F-A15621FC66BC','46E4FE08-FFA0-4C8B-BF88-2C56F38904D6','{"$type":"CreateSemanticDomainChange","Name":{"en":"Eye"},"Predefined":false,"Code":"2.1.1.1","EntityId":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d6"}');
INSERT INTO ChangeEntities VALUES(0,'B76CB8F9-4854-4E93-AF3C-1C2EEC00E216','33C8A15A-1BD4-429E-A400-3304A3E1D997','{"$type":"CreateWritingSystemChange","WsId":"de","Name":"German","Abbreviation":"de","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":0,"Order":1,"EntityId":"33c8a15a-1bd4-429e-a400-3304a3e1d997"}');
INSERT INTO ChangeEntities VALUES(0,'D341A388-62F7-41C2-AF50-2854B2ED941F','9FD79197-95EF-46D7-81A8-BB21AA2579C0','{"$type":"CreateWritingSystemChange","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":0,"Order":2,"EntityId":"9fd79197-95ef-46d7-81a8-bb21aa2579c0"}');
INSERT INTO ChangeEntities VALUES(0,'2C649B96-4ABA-4D4D-9CCA-3670D4469210','485BD12F-976E-40DF-B66C-16D70EE916FA','{"$type":"CreateWritingSystemChange","WsId":"en-Zxxx-x-audio","Name":"English (A)","Abbreviation":"Eng \uD83D\uDD0A","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":0,"Order":3,"EntityId":"485bd12f-976e-40df-b66c-16d70ee916fa"}');
INSERT INTO ChangeEntities VALUES(0,'6917532B-C05C-481E-BED7-A0D075822E89','0B76099F-A102-4D24-8FD7-2C44B6EB3623','{"$type":"CreateWritingSystemChange","WsId":"en","Name":"English","Abbreviation":"en","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":1,"Order":1,"EntityId":"0b76099f-a102-4d24-8fd7-2c44b6eb3623"}');
INSERT INTO ChangeEntities VALUES(0,'6113B384-911C-4A3C-A8CC-85F36BB92106','D4078989-6C7B-4CC5-8501-CE968405484A','{"$type":"CreateWritingSystemChange","WsId":"fr","Name":"French","Abbreviation":"fr","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":1,"Order":2,"EntityId":"d4078989-6c7b-4cc5-8501-ce968405484a"}');
INSERT INTO ChangeEntities VALUES(0,'98CAC228-5353-4BD2-811F-E11119AC8694','ABEAEB03-1A56-49D5-8CC8-A3AFE5FFE89E','{"$type":"CreateWritingSystemChange","WsId":"en-Zxxx-x-audio","Name":"English (A)","Abbreviation":"Eng \uD83D\uDD0A","Font":"Arial","Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Type":1,"Order":3,"EntityId":"abeaeb03-1a56-49d5-8cc8-a3afe5ffe89e"}');
INSERT INTO ChangeEntities VALUES(0,'6A5981B6-D840-44CF-ADF2-D51D4908314B','00EDEFEB-173E-45CD-836C-EEA2BA514429','{"$type":"CreateEntryChange","LexemeForm":{"en":"Apple"},"CitationForm":{"en":"Apple"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"Note":{},"MorphType":"Stem","EntityId":"00edefeb-173e-45cd-836c-eea2ba514429"}');
INSERT INTO ChangeEntities VALUES(1,'6A5981B6-D840-44CF-ADF2-D51D4908314B','B9440091-A9FC-4769-82B1-2EE8D030808D','{"$type":"CreateSenseChange","EntryId":"00edefeb-173e-45cd-836c-eea2ba514429","Order":1,"Definition":{"en":{"Spans":[{"Text":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeechId":null,"SemanticDomains":[],"EntityId":"b9440091-a9fc-4769-82b1-2ee8d030808d"}');
INSERT INTO ChangeEntities VALUES(2,'6A5981B6-D840-44CF-ADF2-D51D4908314B','135D7C84-95D8-4707-A400-E1F3619D90FB','{"$type":"CreateExampleSentenceChange","SenseId":"b9440091-a9fc-4769-82b1-2ee8d030808d","Order":1,"Sentence":{"en":{"Spans":[{"Text":"We ate an apple","Ws":"en"}]}},"Translation":{},"Reference":null,"EntityId":"135d7c84-95d8-4707-a400-e1f3619d90fb"}');
INSERT INTO ChangeEntities VALUES(0,'A867FF78-BA45-41FB-986B-6FFAF8E4D5B5','D9F70CF9-A479-4141-92E5-44155D063DA2','{"$type":"CreateEntryChange","LexemeForm":{"en":"Banana"},"CitationForm":{"en":"Banana"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"Note":{},"MorphType":"Stem","EntityId":"d9f70cf9-a479-4141-92e5-44155d063da2"}');
INSERT INTO ChangeEntities VALUES(1,'A867FF78-BA45-41FB-986B-6FFAF8E4D5B5','C77D7553-209D-45BE-A83E-D07E69808873','{"$type":"CreateSenseChange","EntryId":"d9f70cf9-a479-4141-92e5-44155d063da2","Order":1,"Definition":{"en":{"Spans":[{"Text":"long curved fruit with yellow skin and soft sweet flesh","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeechId":null,"SemanticDomains":[],"EntityId":"c77d7553-209d-45be-a83e-d07e69808873"}');
INSERT INTO ChangeEntities VALUES(2,'A867FF78-BA45-41FB-986B-6FFAF8E4D5B5','D9D515E8-D695-44C3-9766-AFBEAB8C470F','{"$type":"CreateExampleSentenceChange","SenseId":"c77d7553-209d-45be-a83e-d07e69808873","Order":1,"Sentence":{"en":{"Spans":[{"Text":"The monkey peeled a banana","Ws":"en"}]}},"Translation":{},"Reference":null,"EntityId":"d9d515e8-d695-44c3-9766-afbeab8c470f"}');
INSERT INTO ChangeEntities VALUES(0,'191B4774-F571-4E57-8639-D215106E8ABF','75050583-70D7-4605-A6E6-872B8F36C482','{"$type":"CreateEntryChange","LexemeForm":{"en":"Orange"},"CitationForm":{"en":"Orange"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"Note":{},"MorphType":"Stem","EntityId":"75050583-70d7-4605-a6e6-872b8f36c482"}');
INSERT INTO ChangeEntities VALUES(1,'191B4774-F571-4E57-8639-D215106E8ABF','241D95A7-6D90-411B-8569-5648CC40B42B','{"$type":"CreateSenseChange","EntryId":"75050583-70d7-4605-a6e6-872b8f36c482","Order":1,"Definition":{"en":{"Spans":[{"Text":"round citrus fruit with orange skin and juicy segments inside","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeechId":null,"SemanticDomains":[],"EntityId":"241d95a7-6d90-411b-8569-5648cc40b42b"}');
INSERT INTO ChangeEntities VALUES(2,'191B4774-F571-4E57-8639-D215106E8ABF','9AA9F5A4-FAEA-45E9-BEE7-42929EEBF691','{"$type":"CreateExampleSentenceChange","SenseId":"241d95a7-6d90-411b-8569-5648cc40b42b","Order":1,"Sentence":{"en":{"Spans":[{"Text":"I squeezed the orange for juice","Ws":"en"}]}},"Translation":{},"Reference":null,"EntityId":"9aa9f5a4-faea-45e9-bee7-42929eebf691"}');
INSERT INTO ChangeEntities VALUES(0,'CD77D4CB-FBB6-4004-9159-F7F28482CA75','9E84DB09-1885-4BBB-9482-4891A46F6F49','{"$type":"CreateEntryChange","LexemeForm":{"en":"Grape"},"CitationForm":{"en":"Grape"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"Note":{},"MorphType":"Stem","EntityId":"9e84db09-1885-4bbb-9482-4891a46f6f49"}');
INSERT INTO ChangeEntities VALUES(1,'CD77D4CB-FBB6-4004-9159-F7F28482CA75','1B423127-DEB2-468D-8100-E1F1BFF20826','{"$type":"CreateSenseChange","EntryId":"9e84db09-1885-4bbb-9482-4891a46f6f49","Order":1,"Definition":{"en":{"Spans":[{"Text":"small round or oval fruit growing in clusters, used for wine or eating","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeechId":null,"SemanticDomains":[],"EntityId":"1b423127-deb2-468d-8100-e1f1bff20826"}');
INSERT INTO ChangeEntities VALUES(2,'CD77D4CB-FBB6-4004-9159-F7F28482CA75','288D64C3-93F9-48A2-A014-D8773369B4A8','{"$type":"CreateExampleSentenceChange","SenseId":"1b423127-deb2-468d-8100-e1f1bff20826","Order":1,"Sentence":{"en":{"Spans":[{"Text":"The vineyard was full of ripe grapes","Ws":"en"}]}},"Translation":{},"Reference":null,"EntityId":"288d64c3-93f9-48a2-a014-d8773369b4a8"}');
INSERT INTO ChangeEntities VALUES(0,'5DBA93AF-7604-410F-B109-EC4961BB608F','00EDEFEB-173E-45CD-836C-EEA2BA514429','{"$type":"jsonPatch:Entry","PatchDocument":[{"op":"add","path":"/LexemeForm/de","value":"de"}],"EntityId":"00edefeb-173e-45cd-836c-eea2ba514429"}');
INSERT INTO ChangeEntities VALUES(0,'64308E87-7710-48FE-B4D8-2FA131F2456D','00EDEFEB-173E-45CD-836C-EEA2BA514429','{"$type":"jsonPatch:Entry","PatchDocument":[{"op":"add","path":"/CitationForm/de","value":"de"}],"EntityId":"00edefeb-173e-45cd-836c-eea2ba514429"}');
INSERT INTO ChangeEntities VALUES(0,'276EF0D5-677C-46AA-AF7E-D518ABBA15B5','A4A74738-3914-43CA-8C25-BEE34EB517B7','{"$type":"AddEntryComponentChange","Order":1,"ComplexFormEntryId":"75050583-70d7-4605-a6e6-872b8f36c482","ComponentEntryId":"00edefeb-173e-45cd-836c-eea2ba514429","ComponentSenseId":null,"EntityId":"a4a74738-3914-43ca-8c25-bee34eb517b7"}');
INSERT INTO ChangeEntities VALUES(0,'4E078D12-BA55-42A8-8772-F0DB0891FA71','00EDEFEB-173E-45CD-836C-EEA2BA514429','{"$type":"AddComplexFormTypeChange","ComplexFormType":{"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null},"EntityId":"00edefeb-173e-45cd-836c-eea2ba514429"}');
INSERT INTO ChangeEntities VALUES(0,'DF118748-6170-40FA-A38F-7ADDF0AA4B4D','00EDEFEB-173E-45CD-836C-EEA2BA514429','{"$type":"jsonPatch:Entry","PatchDocument":[{"op":"add","path":"/LiteralMeaning/fr","value":{"Spans":[{"Text":"fr","Ws":"fr"}]}}],"EntityId":"00edefeb-173e-45cd-836c-eea2ba514429"}');
INSERT INTO ChangeEntities VALUES(0,'2F476F0D-BAAE-4428-A534-2447A3E6338C','00EDEFEB-173E-45CD-836C-EEA2BA514429','{"$type":"jsonPatch:Entry","PatchDocument":[{"op":"add","path":"/Note/en","value":{"Spans":[{"Text":"note","Ws":"en"}]}}],"EntityId":"00edefeb-173e-45cd-836c-eea2ba514429"}');
INSERT INTO ChangeEntities VALUES(0,'6C1E0DC4-E30B-43DC-889C-DC5787FBF282','B9440091-A9FC-4769-82B1-2EE8D030808D','{"$type":"jsonPatch:Sense","PatchDocument":[{"op":"add","path":"/Gloss/fr","value":"fr"}],"EntityId":"b9440091-a9fc-4769-82b1-2ee8d030808d"}');
INSERT INTO ChangeEntities VALUES(0,'241F2698-C899-4476-986B-B339FA50FCE8','B9440091-A9FC-4769-82B1-2EE8D030808D','{"$type":"jsonPatch:Sense","PatchDocument":[{"op":"add","path":"/Definition/fr","value":{"Spans":[{"Text":"fr","Ws":"fr"}]}}],"EntityId":"b9440091-a9fc-4769-82b1-2ee8d030808d"}');
INSERT INTO ChangeEntities VALUES(0,'16F056B3-C19A-42D0-A824-BA2640597AC2','B9440091-A9FC-4769-82B1-2EE8D030808D','{"$type":"SetPartOfSpeechChange","PartOfSpeechId":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","EntityId":"b9440091-a9fc-4769-82b1-2ee8d030808d"}');
INSERT INTO ChangeEntities VALUES(0,'C2C3E5CC-4E0A-4E3D-A300-D004DACB5A7E','B9440091-A9FC-4769-82B1-2EE8D030808D','{"$type":"AddSemanticDomainChange","SemanticDomain":{"Id":"63403699-07c1-43f3-a47c-069d6e4316e5","Name":{"en":"Universe, Creation"},"Code":"1","DeletedAt":null,"Predefined":true},"EntityId":"b9440091-a9fc-4769-82b1-2ee8d030808d"}');
INSERT INTO ChangeEntities VALUES(0,'5AAC4FF6-0DD8-4266-875C-00F1800EFAD4','135D7C84-95D8-4707-A400-E1F3619D90FB','{"$type":"jsonPatch:ExampleSentence","PatchDocument":[{"op":"add","path":"/Sentence/de","value":{"Spans":[{"Text":"de","Ws":"de"}]}}],"EntityId":"135d7c84-95d8-4707-a400-e1f3619d90fb"}');
INSERT INTO ChangeEntities VALUES(0,'5B954C9F-C86F-463A-BAA2-15EBB7A6074B','135D7C84-95D8-4707-A400-E1F3619D90FB','{"$type":"jsonPatch:ExampleSentence","PatchDocument":[{"op":"add","path":"/Translation/en","value":{"Spans":[{"Text":"en","Ws":"en"}]}}],"EntityId":"135d7c84-95d8-4707-a400-e1f3619d90fb"}');
INSERT INTO ChangeEntities VALUES(0,'BAF9DA29-2187-4D2E-9C67-92C295C3FBD7','135D7C84-95D8-4707-A400-E1F3619D90FB','{"$type":"jsonPatch:ExampleSentence","PatchDocument":[{"op":"replace","path":"/Reference","value":{"Spans":[{"Text":"ref","Ws":"en"}]}}],"EntityId":"135d7c84-95d8-4707-a400-e1f3619d90fb"}');
CREATE TABLE IF NOT EXISTS "Snapshots" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_Snapshots" PRIMARY KEY,

    "TypeName" TEXT NOT NULL,

    "Entity" jsonb NOT NULL,

    "References" TEXT NOT NULL,

    "EntityId" TEXT NOT NULL,

    "EntityIsDeleted" INTEGER NOT NULL,

    "CommitId" TEXT NOT NULL,

    "IsRoot" INTEGER NOT NULL,

    CONSTRAINT "FK_Snapshots_Commits_CommitId" FOREIGN KEY ("CommitId") REFERENCES "Commits" ("Id") ON DELETE CASCADE

);
INSERT INTO Snapshots VALUES('83CA281B-4698-4DDD-8C5E-45FE1A9B5128','ComplexFormType','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ComplexFormType","Id":"eeb78fce-6009-4932-aaa6-85faeb180c69","Name":{"en":"Unspecified"},"DeletedAt":null},"Id":"eeb78fce-6009-4932-aaa6-85faeb180c69","DeletedAt":null}','[]','EEB78FCE-6009-4932-AAA6-85FAEB180C69',0,'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE',1);
INSERT INTO Snapshots VALUES('D9672868-5457-4132-ACB1-40A736CB6286','ComplexFormType','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ComplexFormType","Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null},"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","DeletedAt":null}','[]','C36F55ED-D1EA-4069-90B3-3F35FF696273',0,'DC60D2A9-0CC2-48ED-803C-A238A14B6EAE',1);
INSERT INTO Snapshots VALUES('659A8536-28DA-4671-A63D-36EE158619A1','PartOfSpeech','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"PartOfSpeech","Id":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","Name":{"en":"Adverb"},"DeletedAt":null,"Predefined":true},"Id":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","DeletedAt":null}','[]','46E4FE08-FFA0-4C8B-BF98-2C56F38904D9',0,'023FAEBB-711B-4D2F-B34F-A15621FC66BB',1);
INSERT INTO Snapshots VALUES('14ACC90E-F0B8-4C54-A73A-D72E08D08319','SemanticDomain','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"63403699-07c1-43f3-a47c-069d6e4316e5","Name":{"en":"Universe, Creation"},"Code":"1","DeletedAt":null,"Predefined":true},"Id":"63403699-07c1-43f3-a47c-069d6e4316e5","DeletedAt":null}','[]','63403699-07C1-43F3-A47C-069D6E4316E5',0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC',1);
INSERT INTO Snapshots VALUES('6984026F-F359-464B-8C0A-38911D6B432B','SemanticDomain','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"999581c4-1611-4acb-ae1b-5e6c1dfe6f0c","Name":{"en":"Sky"},"Code":"1.1","DeletedAt":null,"Predefined":true},"Id":"999581c4-1611-4acb-ae1b-5e6c1dfe6f0c","DeletedAt":null}','[]','999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C',0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC',1);
INSERT INTO Snapshots VALUES('7EDD8E1F-47FD-48BC-9B59-4CEC9C1AD2A4','SemanticDomain','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"dc1a2c6f-1b32-4631-8823-36dacc8cb7bb","Name":{"en":"World"},"Code":"1.2","DeletedAt":null,"Predefined":true},"Id":"dc1a2c6f-1b32-4631-8823-36dacc8cb7bb","DeletedAt":null}','[]','DC1A2C6F-1B32-4631-8823-36DACC8CB7BB',0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC',1);
INSERT INTO Snapshots VALUES('9689DD1F-0084-464D-89F2-52A05361DAEC','SemanticDomain','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"1bd42665-0610-4442-8d8d-7c666fee3a6d","Name":{"en":"Person"},"Code":"2","DeletedAt":null,"Predefined":true},"Id":"1bd42665-0610-4442-8d8d-7c666fee3a6d","DeletedAt":null}','[]','1BD42665-0610-4442-8D8D-7C666FEE3A6D',0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC',1);
INSERT INTO Snapshots VALUES('B4940DD3-2AD6-4391-98BF-8C26CD6D9BA9','SemanticDomain','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d6","Name":{"en":"Eye"},"Code":"2.1.1.1","DeletedAt":null,"Predefined":false},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d6","DeletedAt":null}','[]','46E4FE08-FFA0-4C8B-BF88-2C56F38904D6',0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC',1);
INSERT INTO Snapshots VALUES('D8F964B5-312D-4D2E-9D47-3A00C42B7B52','SemanticDomain','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d5","Name":{"en":"Head"},"Code":"2.1.1","DeletedAt":null,"Predefined":false},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d5","DeletedAt":null}','[]','46E4FE08-FFA0-4C8B-BF88-2C56F38904D5',0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC',1);
INSERT INTO Snapshots VALUES('EAB8B267-9E9B-48A3-A593-C890771B743D','SemanticDomain','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"SemanticDomain","Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d4","Name":{"en":"Body"},"Code":"2.1","DeletedAt":null,"Predefined":false},"Id":"46e4fe08-ffa0-4c8b-bf88-2c56f38904d4","DeletedAt":null}','[]','46E4FE08-FFA0-4C8B-BF88-2C56F38904D4',0,'023FAEBB-711B-4D2F-A14F-A15621FC66BC',1);
INSERT INTO Snapshots VALUES('F001C69D-04B4-4B0F-B049-A56E94041CAF','WritingSystem','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"33c8a15a-1bd4-429e-a400-3304a3e1d997","WsId":"de","IsAudio":false,"Name":"German","Abbreviation":"de","Font":"Arial","DeletedAt":null,"Type":0,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":1},"Id":"33c8a15a-1bd4-429e-a400-3304a3e1d997","DeletedAt":null}','[]','33C8A15A-1BD4-429E-A400-3304A3E1D997',0,'B76CB8F9-4854-4E93-AF3C-1C2EEC00E216',1);
INSERT INTO Snapshots VALUES('1A9E7520-9D95-476B-BA35-A4962AD9A0B6','WritingSystem','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"9fd79197-95ef-46d7-81a8-bb21aa2579c0","WsId":"en","IsAudio":false,"Name":"English","Abbreviation":"en","Font":"Arial","DeletedAt":null,"Type":0,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":2},"Id":"9fd79197-95ef-46d7-81a8-bb21aa2579c0","DeletedAt":null}','[]','9FD79197-95EF-46D7-81A8-BB21AA2579C0',0,'D341A388-62F7-41C2-AF50-2854B2ED941F',1);
INSERT INTO Snapshots VALUES('B03E10DE-3708-4D19-8A9E-DAC133FA1A74','WritingSystem','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"485bd12f-976e-40df-b66c-16d70ee916fa","WsId":"en-Zxxx-x-audio","IsAudio":true,"Name":"English (A)","Abbreviation":"Eng \uD83D\uDD0A","Font":"Arial","DeletedAt":null,"Type":0,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":3},"Id":"485bd12f-976e-40df-b66c-16d70ee916fa","DeletedAt":null}','[]','485BD12F-976E-40DF-B66C-16D70EE916FA',0,'2C649B96-4ABA-4D4D-9CCA-3670D4469210',1);
INSERT INTO Snapshots VALUES('0F01F15D-938D-495A-B3CB-948CAEDE7540','WritingSystem','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"0b76099f-a102-4d24-8fd7-2c44b6eb3623","WsId":"en","IsAudio":false,"Name":"English","Abbreviation":"en","Font":"Arial","DeletedAt":null,"Type":1,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":1},"Id":"0b76099f-a102-4d24-8fd7-2c44b6eb3623","DeletedAt":null}','[]','0B76099F-A102-4D24-8FD7-2C44B6EB3623',0,'6917532B-C05C-481E-BED7-A0D075822E89',1);
INSERT INTO Snapshots VALUES('A2380046-BC58-4B62-9815-86E6E4D68304','WritingSystem','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"d4078989-6c7b-4cc5-8501-ce968405484a","WsId":"fr","IsAudio":false,"Name":"French","Abbreviation":"fr","Font":"Arial","DeletedAt":null,"Type":1,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":2},"Id":"d4078989-6c7b-4cc5-8501-ce968405484a","DeletedAt":null}','[]','D4078989-6C7B-4CC5-8501-CE968405484A',0,'6113B384-911C-4A3C-A8CC-85F36BB92106',1);
INSERT INTO Snapshots VALUES('2923D72F-238A-44EE-AF1D-C5613D7CBE54','WritingSystem','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"WritingSystem","Id":"abeaeb03-1a56-49d5-8cc8-a3afe5ffe89e","WsId":"en-Zxxx-x-audio","IsAudio":true,"Name":"English (A)","Abbreviation":"Eng \uD83D\uDD0A","Font":"Arial","DeletedAt":null,"Type":1,"Exemplars":["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],"Order":3},"Id":"abeaeb03-1a56-49d5-8cc8-a3afe5ffe89e","DeletedAt":null}','[]','ABEAEB03-1A56-49D5-8CC8-A3AFE5FFE89E',0,'98CAC228-5353-4BD2-811F-E11119AC8694',1);
INSERT INTO Snapshots VALUES('929EF062-41E8-45E5-ABF3-3C20507A80DC','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","Order":1,"Sentence":{"en":{"Spans":[{"Text":"We ate an apple","Ws":"en"}]}},"Translation":{},"Reference":null,"SenseId":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null},"Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","DeletedAt":null}','["B9440091-A9FC-4769-82B1-2EE8D030808D"]','135D7C84-95D8-4707-A400-E1F3619D90FB',0,'6A5981B6-D840-44CF-ADF2-D51D4908314B',1);
INSERT INTO Snapshots VALUES('99C1758F-1CE7-4FE7-B261-6AF7C66A031A','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null,"LexemeForm":{"en":"Apple"},"CitationForm":{"en":"Apple"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[],"PublishIn":[]},"Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null}','[]','00EDEFEB-173E-45CD-836C-EEA2BA514429',0,'6A5981B6-D840-44CF-ADF2-D51D4908314B',1);
INSERT INTO Snapshots VALUES('C18904B3-5D12-4EEC-A239-B5A3B5AE6A7D','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","Order":1,"DeletedAt":null,"EntryId":"00edefeb-173e-45cd-836c-eea2ba514429","Definition":{"en":{"Spans":[{"Text":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeech":null,"PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]},"Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null}','["00EDEFEB-173E-45CD-836C-EEA2BA514429"]','B9440091-A9FC-4769-82B1-2EE8D030808D',0,'6A5981B6-D840-44CF-ADF2-D51D4908314B',1);
INSERT INTO Snapshots VALUES('2F77033C-F317-4D01-A486-2F976CE7850E','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"d9d515e8-d695-44c3-9766-afbeab8c470f","Order":1,"Sentence":{"en":{"Spans":[{"Text":"The monkey peeled a banana","Ws":"en"}]}},"Translation":{},"Reference":null,"SenseId":"c77d7553-209d-45be-a83e-d07e69808873","DeletedAt":null},"Id":"d9d515e8-d695-44c3-9766-afbeab8c470f","DeletedAt":null}','["C77D7553-209D-45BE-A83E-D07E69808873"]','D9D515E8-D695-44C3-9766-AFBEAB8C470F',0,'A867FF78-BA45-41FB-986B-6FFAF8E4D5B5',1);
INSERT INTO Snapshots VALUES('DCFD50CE-413B-4D12-B4E7-6EED289D3AB2','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"c77d7553-209d-45be-a83e-d07e69808873","Order":1,"DeletedAt":null,"EntryId":"d9f70cf9-a479-4141-92e5-44155d063da2","Definition":{"en":{"Spans":[{"Text":"long curved fruit with yellow skin and soft sweet flesh","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeech":null,"PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]},"Id":"c77d7553-209d-45be-a83e-d07e69808873","DeletedAt":null}','["D9F70CF9-A479-4141-92E5-44155D063DA2"]','C77D7553-209D-45BE-A83E-D07E69808873',0,'A867FF78-BA45-41FB-986B-6FFAF8E4D5B5',1);
INSERT INTO Snapshots VALUES('ECF9A05C-0E55-4342-B674-16FA1B347FED','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"d9f70cf9-a479-4141-92e5-44155d063da2","DeletedAt":null,"LexemeForm":{"en":"Banana"},"CitationForm":{"en":"Banana"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[],"PublishIn":[]},"Id":"d9f70cf9-a479-4141-92e5-44155d063da2","DeletedAt":null}','[]','D9F70CF9-A479-4141-92E5-44155D063DA2',0,'A867FF78-BA45-41FB-986B-6FFAF8E4D5B5',1);
INSERT INTO Snapshots VALUES('82C4E9D7-883B-46C3-8837-DD5E85065350','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"75050583-70d7-4605-a6e6-872b8f36c482","DeletedAt":null,"LexemeForm":{"en":"Orange"},"CitationForm":{"en":"Orange"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[],"PublishIn":[]},"Id":"75050583-70d7-4605-a6e6-872b8f36c482","DeletedAt":null}','[]','75050583-70D7-4605-A6E6-872B8F36C482',0,'191B4774-F571-4E57-8639-D215106E8ABF',1);
INSERT INTO Snapshots VALUES('8994D7EE-7EF6-4C19-9549-DFD8A40144C2','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"9aa9f5a4-faea-45e9-bee7-42929eebf691","Order":1,"Sentence":{"en":{"Spans":[{"Text":"I squeezed the orange for juice","Ws":"en"}]}},"Translation":{},"Reference":null,"SenseId":"241d95a7-6d90-411b-8569-5648cc40b42b","DeletedAt":null},"Id":"9aa9f5a4-faea-45e9-bee7-42929eebf691","DeletedAt":null}','["241D95A7-6D90-411B-8569-5648CC40B42B"]','9AA9F5A4-FAEA-45E9-BEE7-42929EEBF691',0,'191B4774-F571-4E57-8639-D215106E8ABF',1);
INSERT INTO Snapshots VALUES('ABC7FA87-8A65-4C55-8243-719DA4372BDB','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"241d95a7-6d90-411b-8569-5648cc40b42b","Order":1,"DeletedAt":null,"EntryId":"75050583-70d7-4605-a6e6-872b8f36c482","Definition":{"en":{"Spans":[{"Text":"round citrus fruit with orange skin and juicy segments inside","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeech":null,"PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]},"Id":"241d95a7-6d90-411b-8569-5648cc40b42b","DeletedAt":null}','["75050583-70D7-4605-A6E6-872B8F36C482"]','241D95A7-6D90-411B-8569-5648CC40B42B',0,'191B4774-F571-4E57-8639-D215106E8ABF',1);
INSERT INTO Snapshots VALUES('45CA39E0-64CD-4EE3-B721-476791CBCFED','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"1b423127-deb2-468d-8100-e1f1bff20826","Order":1,"DeletedAt":null,"EntryId":"9e84db09-1885-4bbb-9482-4891a46f6f49","Definition":{"en":{"Spans":[{"Text":"small round or oval fruit growing in clusters, used for wine or eating","Ws":"en"}]}},"Gloss":{"en":"Fruit"},"PartOfSpeech":null,"PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]},"Id":"1b423127-deb2-468d-8100-e1f1bff20826","DeletedAt":null}','["9E84DB09-1885-4BBB-9482-4891A46F6F49"]','1B423127-DEB2-468D-8100-E1F1BFF20826',0,'CD77D4CB-FBB6-4004-9159-F7F28482CA75',1);
INSERT INTO Snapshots VALUES('4B52C42E-4781-45EC-BAB9-A2C55D6D394D','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"288d64c3-93f9-48a2-a014-d8773369b4a8","Order":1,"Sentence":{"en":{"Spans":[{"Text":"The vineyard was full of ripe grapes","Ws":"en"}]}},"Translation":{},"Reference":null,"SenseId":"1b423127-deb2-468d-8100-e1f1bff20826","DeletedAt":null},"Id":"288d64c3-93f9-48a2-a014-d8773369b4a8","DeletedAt":null}','["1B423127-DEB2-468D-8100-E1F1BFF20826"]','288D64C3-93F9-48A2-A014-D8773369B4A8',0,'CD77D4CB-FBB6-4004-9159-F7F28482CA75',1);
INSERT INTO Snapshots VALUES('5E1E9631-D246-4A9D-8052-1084B2D07441','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"9e84db09-1885-4bbb-9482-4891a46f6f49","DeletedAt":null,"LexemeForm":{"en":"Grape"},"CitationForm":{"en":"Grape"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[],"PublishIn":[]},"Id":"9e84db09-1885-4bbb-9482-4891a46f6f49","DeletedAt":null}','[]','9E84DB09-1885-4BBB-9482-4891A46F6F49',0,'CD77D4CB-FBB6-4004-9159-F7F28482CA75',1);
INSERT INTO Snapshots VALUES('32B90AC0-9184-487C-866A-8152A9EA17FC','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null,"LexemeForm":{"en":"Apple","de":"de"},"CitationForm":{"en":"Apple"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[],"PublishIn":[]},"Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null}','[]','00EDEFEB-173E-45CD-836C-EEA2BA514429',0,'5DBA93AF-7604-410F-B109-EC4961BB608F',0);
INSERT INTO Snapshots VALUES('EBCC8E22-2FF8-4911-A1B3-A40B2C6606AA','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null,"LexemeForm":{"en":"Apple","de":"de"},"CitationForm":{"en":"Apple","de":"de"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[],"PublishIn":[]},"Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null}','[]','00EDEFEB-173E-45CD-836C-EEA2BA514429',0,'64308E87-7710-48FE-B4D8-2FA131F2456D',0);
INSERT INTO Snapshots VALUES('3823E325-8715-4DA1-995D-8F39C8E3AF29','ComplexFormComponent','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ComplexFormComponent","Id":"a4a74738-3914-43ca-8c25-bee34eb517b7","MaybeId":"a4a74738-3914-43ca-8c25-bee34eb517b7","Order":1,"DeletedAt":null,"ComplexFormEntryId":"75050583-70d7-4605-a6e6-872b8f36c482","ComplexFormHeadword":"Orange","ComponentEntryId":"00edefeb-173e-45cd-836c-eea2ba514429","ComponentSenseId":null,"ComponentHeadword":"de"},"Id":"a4a74738-3914-43ca-8c25-bee34eb517b7","DeletedAt":null}','["75050583-70D7-4605-A6E6-872B8F36C482","00EDEFEB-173E-45CD-836C-EEA2BA514429"]','A4A74738-3914-43CA-8C25-BEE34EB517B7',0,'276EF0D5-677C-46AA-AF7E-D518ABBA15B5',1);
INSERT INTO Snapshots VALUES('8CDE8DAC-409C-4276-B018-499CF92409CA','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null,"LexemeForm":{"en":"Apple","de":"de"},"CitationForm":{"en":"Apple","de":"de"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[{"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null}],"PublishIn":[]},"Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null}','[]','00EDEFEB-173E-45CD-836C-EEA2BA514429',0,'4E078D12-BA55-42A8-8772-F0DB0891FA71',0);
INSERT INTO Snapshots VALUES('82AFBBFD-3C0C-4465-A467-31242C50EECF','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null,"LexemeForm":{"en":"Apple","de":"de"},"CitationForm":{"en":"Apple","de":"de"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]},"fr":{"Spans":[{"Text":"fr","Ws":"fr"}]}},"MorphType":"Stem","Senses":[],"Note":{},"Components":[],"ComplexForms":[],"ComplexFormTypes":[{"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null}],"PublishIn":[]},"Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null}','[]','00EDEFEB-173E-45CD-836C-EEA2BA514429',0,'DF118748-6170-40FA-A38F-7ADDF0AA4B4D',0);
INSERT INTO Snapshots VALUES('2FDA581C-6C02-4B64-908B-1E0509F9EDBE','Entry','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Entry","Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null,"LexemeForm":{"en":"Apple","de":"de"},"CitationForm":{"en":"Apple","de":"de"},"LiteralMeaning":{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]},"fr":{"Spans":[{"Text":"fr","Ws":"fr"}]}},"MorphType":"Stem","Senses":[],"Note":{"en":{"Spans":[{"Text":"note","Ws":"en"}]}},"Components":[],"ComplexForms":[],"ComplexFormTypes":[{"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null}],"PublishIn":[]},"Id":"00edefeb-173e-45cd-836c-eea2ba514429","DeletedAt":null}','[]','00EDEFEB-173E-45CD-836C-EEA2BA514429',0,'2F476F0D-BAAE-4428-A534-2447A3E6338C',0);
INSERT INTO Snapshots VALUES('B0D152AF-5FA5-4DF4-81A6-630DD552499F','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","Order":1,"DeletedAt":null,"EntryId":"00edefeb-173e-45cd-836c-eea2ba514429","Definition":{"en":{"Spans":[{"Text":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh","Ws":"en"}]}},"Gloss":{"en":"Fruit","fr":"fr"},"PartOfSpeech":null,"PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]},"Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null}','["00EDEFEB-173E-45CD-836C-EEA2BA514429"]','B9440091-A9FC-4769-82B1-2EE8D030808D',0,'6C1E0DC4-E30B-43DC-889C-DC5787FBF282',0);
INSERT INTO Snapshots VALUES('04A68EC6-1B56-4303-A379-35DA06346E0B','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","Order":1,"DeletedAt":null,"EntryId":"00edefeb-173e-45cd-836c-eea2ba514429","Definition":{"en":{"Spans":[{"Text":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh","Ws":"en"}]},"fr":{"Spans":[{"Text":"fr","Ws":"fr"}]}},"Gloss":{"en":"Fruit","fr":"fr"},"PartOfSpeech":null,"PartOfSpeechId":null,"SemanticDomains":[],"ExampleSentences":[]},"Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null}','["00EDEFEB-173E-45CD-836C-EEA2BA514429"]','B9440091-A9FC-4769-82B1-2EE8D030808D',0,'241F2698-C899-4476-986B-B339FA50FCE8',0);
INSERT INTO Snapshots VALUES('0A5BE7B9-9C5C-4786-ABE3-2310DAE6CCCA','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","Order":1,"DeletedAt":null,"EntryId":"00edefeb-173e-45cd-836c-eea2ba514429","Definition":{"en":{"Spans":[{"Text":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh","Ws":"en"}]},"fr":{"Spans":[{"Text":"fr","Ws":"fr"}]}},"Gloss":{"en":"Fruit","fr":"fr"},"PartOfSpeech":null,"PartOfSpeechId":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","SemanticDomains":[],"ExampleSentences":[]},"Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null}','["00EDEFEB-173E-45CD-836C-EEA2BA514429","46E4FE08-FFA0-4C8B-BF98-2C56F38904D9"]','B9440091-A9FC-4769-82B1-2EE8D030808D',0,'16F056B3-C19A-42D0-A824-BA2640597AC2',0);
INSERT INTO Snapshots VALUES('87F3BBAD-ED5D-4CBE-987C-786746A94C06','Sense','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"Sense","Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","Order":1,"DeletedAt":null,"EntryId":"00edefeb-173e-45cd-836c-eea2ba514429","Definition":{"en":{"Spans":[{"Text":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh","Ws":"en"}]},"fr":{"Spans":[{"Text":"fr","Ws":"fr"}]}},"Gloss":{"en":"Fruit","fr":"fr"},"PartOfSpeech":null,"PartOfSpeechId":"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9","SemanticDomains":[{"Id":"63403699-07c1-43f3-a47c-069d6e4316e5","Name":{"en":"Universe, Creation"},"Code":"1","DeletedAt":null,"Predefined":true}],"ExampleSentences":[]},"Id":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null}','["00EDEFEB-173E-45CD-836C-EEA2BA514429","46E4FE08-FFA0-4C8B-BF98-2C56F38904D9","63403699-07C1-43F3-A47C-069D6E4316E5"]','B9440091-A9FC-4769-82B1-2EE8D030808D',0,'C2C3E5CC-4E0A-4E3D-A300-D004DACB5A7E',0);
INSERT INTO Snapshots VALUES('94950C41-D5F9-442F-B91F-1807EE743904','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","Order":1,"Sentence":{"en":{"Spans":[{"Text":"We ate an apple","Ws":"en"}]},"de":{"Spans":[{"Text":"de","Ws":"de"}]}},"Translation":{},"Reference":null,"SenseId":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null},"Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","DeletedAt":null}','["B9440091-A9FC-4769-82B1-2EE8D030808D"]','135D7C84-95D8-4707-A400-E1F3619D90FB',0,'5AAC4FF6-0DD8-4266-875C-00F1800EFAD4',0);
INSERT INTO Snapshots VALUES('BEB54E61-57F8-4227-9D2E-4AD56982CB70','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","Order":1,"Sentence":{"en":{"Spans":[{"Text":"We ate an apple","Ws":"en"}]},"de":{"Spans":[{"Text":"de","Ws":"de"}]}},"Translation":{"en":{"Spans":[{"Text":"en","Ws":"en"}]}},"Reference":null,"SenseId":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null},"Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","DeletedAt":null}','["B9440091-A9FC-4769-82B1-2EE8D030808D"]','135D7C84-95D8-4707-A400-E1F3619D90FB',0,'5B954C9F-C86F-463A-BAA2-15EBB7A6074B',0);
INSERT INTO Snapshots VALUES('7D383194-90E8-45DE-839B-E53FBC9BCFB7','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","Order":1,"Sentence":{"en":{"Spans":[{"Text":"We ate an apple","Ws":"en"}]},"de":{"Spans":[{"Text":"de","Ws":"de"}]}},"Translation":{"en":{"Spans":[{"Text":"en","Ws":"en"}]}},"Reference":{"Spans":[{"Text":"ref","Ws":"en"}]},"SenseId":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null},"Id":"135d7c84-95d8-4707-a400-e1f3619d90fb","DeletedAt":null}','["B9440091-A9FC-4769-82B1-2EE8D030808D"]','135D7C84-95D8-4707-A400-E1F3619D90FB',0,'BAF9DA29-2187-4D2E-9C67-92C295C3FBD7',0);
CREATE TABLE IF NOT EXISTS "ComplexFormType" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_ComplexFormType" PRIMARY KEY,

    "Name" jsonb NOT NULL,

    "DeletedAt" TEXT NULL,

    "SnapshotId" TEXT NULL,

    CONSTRAINT "FK_ComplexFormType_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO ComplexFormType VALUES('C36F55ED-D1EA-4069-90B3-3F35FF696273','{"en":"Compound"}',NULL,'D9672868-5457-4132-ACB1-40A736CB6286');
INSERT INTO ComplexFormType VALUES('EEB78FCE-6009-4932-AAA6-85FAEB180C69','{"en":"Unspecified"}',NULL,'83CA281B-4698-4DDD-8C5E-45FE1A9B5128');
CREATE TABLE IF NOT EXISTS "Entry" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_Entry" PRIMARY KEY,

    "DeletedAt" TEXT NULL,

    "LexemeForm" jsonb NOT NULL,

    "CitationForm" jsonb NOT NULL,

    "LiteralMeaning" jsonb NOT NULL,

    "Note" jsonb NOT NULL,

    "ComplexFormTypes" jsonb NOT NULL,

    "SnapshotId" TEXT NULL, "PublishIn" jsonb NOT NULL DEFAULT '', "MorphType" INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT "FK_Entry_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO Entry VALUES('00EDEFEB-173E-45CD-836C-EEA2BA514429',NULL,'{"en":"Apple","de":"de"}','{"en":"Apple","de":"de"}','{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]},"fr":{"Spans":[{"Text":"fr","Ws":"fr"}]}}','{"en":{"Spans":[{"Text":"note","Ws":"en"}]}}','[{"Id":"c36f55ed-d1ea-4069-90b3-3f35ff696273","Name":{"en":"Compound"},"DeletedAt":null}]','2FDA581C-6C02-4B64-908B-1E0509F9EDBE','[]',12);
INSERT INTO Entry VALUES('D9F70CF9-A479-4141-92E5-44155D063DA2',NULL,'{"en":"Banana"}','{"en":"Banana"}','{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}}','{}','[]','ECF9A05C-0E55-4342-B674-16FA1B347FED','[]',12);
INSERT INTO Entry VALUES('75050583-70D7-4605-A6E6-872B8F36C482',NULL,'{"en":"Orange"}','{"en":"Orange"}','{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}}','{}','[]','82C4E9D7-883B-46C3-8837-DD5E85065350','[]',12);
INSERT INTO Entry VALUES('9E84DB09-1885-4BBB-9482-4891A46F6F49',NULL,'{"en":"Grape"}','{"en":"Grape"}','{"en":{"Spans":[{"Text":"Fruit","Ws":"en"}]}}','{}','[]','5E1E9631-D246-4A9D-8052-1084B2D07441','[]',12);
CREATE TABLE IF NOT EXISTS "PartOfSpeech" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_PartOfSpeech" PRIMARY KEY,

    "Name" jsonb NOT NULL,

    "DeletedAt" TEXT NULL,

    "Predefined" INTEGER NOT NULL,

    "SnapshotId" TEXT NULL,

    CONSTRAINT "FK_PartOfSpeech_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO PartOfSpeech VALUES('46E4FE08-FFA0-4C8B-BF98-2C56F38904D9','{"en":"Adverb"}',NULL,1,'659A8536-28DA-4671-A63D-36EE158619A1');
CREATE TABLE IF NOT EXISTS "SemanticDomain" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_SemanticDomain" PRIMARY KEY,

    "Name" jsonb NOT NULL,

    "Code" TEXT NOT NULL,

    "DeletedAt" TEXT NULL,

    "Predefined" INTEGER NOT NULL,

    "SnapshotId" TEXT NULL,

    CONSTRAINT "FK_SemanticDomain_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO SemanticDomain VALUES('1BD42665-0610-4442-8D8D-7C666FEE3A6D','{"en":"Person"}','2',NULL,1,'9689DD1F-0084-464D-89F2-52A05361DAEC');
INSERT INTO SemanticDomain VALUES('46E4FE08-FFA0-4C8B-BF88-2C56F38904D4','{"en":"Body"}','2.1',NULL,0,'EAB8B267-9E9B-48A3-A593-C890771B743D');
INSERT INTO SemanticDomain VALUES('46E4FE08-FFA0-4C8B-BF88-2C56F38904D5','{"en":"Head"}','2.1.1',NULL,0,'D8F964B5-312D-4D2E-9D47-3A00C42B7B52');
INSERT INTO SemanticDomain VALUES('46E4FE08-FFA0-4C8B-BF88-2C56F38904D6','{"en":"Eye"}','2.1.1.1',NULL,0,'B4940DD3-2AD6-4391-98BF-8C26CD6D9BA9');
INSERT INTO SemanticDomain VALUES('63403699-07C1-43F3-A47C-069D6E4316E5','{"en":"Universe, Creation"}','1',NULL,1,'14ACC90E-F0B8-4C54-A73A-D72E08D08319');
INSERT INTO SemanticDomain VALUES('999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C','{"en":"Sky"}','1.1',NULL,1,'6984026F-F359-464B-8C0A-38911D6B432B');
INSERT INTO SemanticDomain VALUES('DC1A2C6F-1B32-4631-8823-36DACC8CB7BB','{"en":"World"}','1.2',NULL,1,'7EDD8E1F-47FD-48BC-9B59-4CEC9C1AD2A4');
CREATE TABLE IF NOT EXISTS "WritingSystem" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_WritingSystem" PRIMARY KEY,

    "WsId" TEXT NOT NULL,

    "Name" TEXT NOT NULL,

    "Abbreviation" TEXT NOT NULL,

    "Font" TEXT NOT NULL,

    "DeletedAt" TEXT NULL,

    "Type" INTEGER NOT NULL,

    "Exemplars" jsonb NOT NULL,

    "Order" REAL NOT NULL,

    "SnapshotId" TEXT NULL,

    CONSTRAINT "FK_WritingSystem_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO WritingSystem VALUES('33C8A15A-1BD4-429E-A400-3304A3E1D997','de','German','de','Arial',NULL,0,'["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]',1.0,'F001C69D-04B4-4B0F-B049-A56E94041CAF');
INSERT INTO WritingSystem VALUES('9FD79197-95EF-46D7-81A8-BB21AA2579C0','en','English','en','Arial',NULL,0,'["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]',2.0,'1A9E7520-9D95-476B-BA35-A4962AD9A0B6');
INSERT INTO WritingSystem VALUES('485BD12F-976E-40DF-B66C-16D70EE916FA','en-Zxxx-x-audio','English (A)','Eng 🔊','Arial',NULL,0,'["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]',3.0,'B03E10DE-3708-4D19-8A9E-DAC133FA1A74');
INSERT INTO WritingSystem VALUES('0B76099F-A102-4D24-8FD7-2C44B6EB3623','en','English','en','Arial',NULL,1,'["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]',1.0,'0F01F15D-938D-495A-B3CB-948CAEDE7540');
INSERT INTO WritingSystem VALUES('D4078989-6C7B-4CC5-8501-CE968405484A','fr','French','fr','Arial',NULL,1,'["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]',2.0,'A2380046-BC58-4B62-9815-86E6E4D68304');
INSERT INTO WritingSystem VALUES('ABEAEB03-1A56-49D5-8CC8-A3AFE5FFE89E','en-Zxxx-x-audio','English (A)','Eng 🔊','Arial',NULL,1,'["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"]',3.0,'2923D72F-238A-44EE-AF1D-C5613D7CBE54');
CREATE TABLE IF NOT EXISTS "ComplexFormComponents" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_ComplexFormComponents" PRIMARY KEY,

    "DeletedAt" TEXT NULL,

    "ComplexFormEntryId" TEXT NOT NULL,

    "ComplexFormHeadword" TEXT NULL,

    "ComponentEntryId" TEXT NOT NULL,

    "ComponentSenseId" TEXT NULL,

    "ComponentHeadword" TEXT NULL,

    "SnapshotId" TEXT NULL, "Order" REAL NOT NULL DEFAULT 0.0,

    CONSTRAINT "FK_ComplexFormComponents_Entry_ComplexFormEntryId" FOREIGN KEY ("ComplexFormEntryId") REFERENCES "Entry" ("Id") ON DELETE CASCADE,

    CONSTRAINT "FK_ComplexFormComponents_Entry_ComponentEntryId" FOREIGN KEY ("ComponentEntryId") REFERENCES "Entry" ("Id") ON DELETE CASCADE,

    CONSTRAINT "FK_ComplexFormComponents_Sense_ComponentSenseId" FOREIGN KEY ("ComponentSenseId") REFERENCES "Sense" ("Id") ON DELETE CASCADE,

    CONSTRAINT "FK_ComplexFormComponents_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO ComplexFormComponents VALUES('A4A74738-3914-43CA-8C25-BEE34EB517B7',NULL,'75050583-70D7-4605-A6E6-872B8F36C482','Orange','00EDEFEB-173E-45CD-836C-EEA2BA514429',NULL,'de','3823E325-8715-4DA1-995D-8F39C8E3AF29',1.0);
CREATE TABLE IF NOT EXISTS "Publication" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_Publication" PRIMARY KEY,

    "DeletedAt" TEXT NULL,

    "Name" jsonb NOT NULL,

    "SnapshotId" TEXT NULL,

    CONSTRAINT "FK_Publication_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
CREATE TABLE IF NOT EXISTS "ProjectData" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectData" PRIMARY KEY,

    "ClientId" TEXT NOT NULL,

    "Code" TEXT NOT NULL,

    "FwProjectId" TEXT NULL,

    "LastUserId" TEXT NULL,

    "LastUserName" TEXT NULL,

    "Name" TEXT NOT NULL,

    "OriginDomain" TEXT NULL

, "Role" TEXT NOT NULL DEFAULT 'Editor');
INSERT INTO ProjectData VALUES('B467051E-A492-4E5B-9C17-858D7797292C','ED59326F-B0AA-406F-857A-50FA2B1DC417','Example-Project-dev-2025-09-11-04-40-41-412',NULL,NULL,NULL,'Example-Project-dev-2025-09-11-04-40-41-412',NULL,'Editor');
CREATE TABLE IF NOT EXISTS "ExampleSentence" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_ExampleSentence" PRIMARY KEY,

    "DeletedAt" TEXT NULL,

    "Order" REAL NOT NULL,

    "Reference" jsonb NULL,

    "SenseId" TEXT NOT NULL,

    "Sentence" jsonb NOT NULL,

    "SnapshotId" TEXT NULL,

    "Translation" jsonb NOT NULL,

    CONSTRAINT "FK_ExampleSentence_Sense_SenseId" FOREIGN KEY ("SenseId") REFERENCES "Sense" ("Id") ON DELETE CASCADE,

    CONSTRAINT "FK_ExampleSentence_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO ExampleSentence VALUES('135D7C84-95D8-4707-A400-E1F3619D90FB',NULL,1.0,'{"Spans":[{"Text":"ref","Ws":"en"}]}','B9440091-A9FC-4769-82B1-2EE8D030808D','{"en":{"Spans":[{"Text":"We ate an apple","Ws":"en"}]},"de":{"Spans":[{"Text":"de","Ws":"de"}]}}','7D383194-90E8-45DE-839B-E53FBC9BCFB7','{"en":{"Spans":[{"Text":"en","Ws":"en"}]}}');
INSERT INTO ExampleSentence VALUES('D9D515E8-D695-44C3-9766-AFBEAB8C470F',NULL,1.0,NULL,'C77D7553-209D-45BE-A83E-D07E69808873','{"en":{"Spans":[{"Text":"The monkey peeled a banana","Ws":"en"}]}}','2F77033C-F317-4D01-A486-2F976CE7850E','{}');
INSERT INTO ExampleSentence VALUES('9AA9F5A4-FAEA-45E9-BEE7-42929EEBF691',NULL,1.0,NULL,'241D95A7-6D90-411B-8569-5648CC40B42B','{"en":{"Spans":[{"Text":"I squeezed the orange for juice","Ws":"en"}]}}','8994D7EE-7EF6-4C19-9549-DFD8A40144C2','{}');
INSERT INTO ExampleSentence VALUES('288D64C3-93F9-48A2-A014-D8773369B4A8',NULL,1.0,NULL,'1B423127-DEB2-468D-8100-E1F1BFF20826','{"en":{"Spans":[{"Text":"The vineyard was full of ripe grapes","Ws":"en"}]}}','4B52C42E-4781-45EC-BAB9-A2C55D6D394D','{}');
PRAGMA writable_schema=ON;
INSERT INTO sqlite_schema(type,name,tbl_name,rootpage,sql)VALUES('table','EntrySearchRecord','EntrySearchRecord',0,'CREATE VIRTUAL TABLE EntrySearchRecord USING fts5(Headword, LexemeForm, CitationForm, Definition, Gloss, Id UNINDEXED, tokenize="trigram remove_diacritics 1")');
CREATE TABLE IF NOT EXISTS 'EntrySearchRecord_data'(id INTEGER PRIMARY KEY, block BLOB);
INSERT INTO EntrySearchRecord_data VALUES(1,X'0400111182020f00');
INSERT INTO EntrySearchRecord_data VALUES(10,X'00000001010d0d000d0101010201010301010401010501010601010701010801010901010a01010b01010c01010d0101');
INSERT INTO EntrySearchRecord_data VALUES(137438953473,X'000002ad0430206120010601032c02026372010601033c0202666c010601034902026772010601031c02026f7201080103191d02027265010601030c0202736b0106010322030177010601032e02027461010601033702027768010601034303016901080103072202027965010601031101032c206f010601031803017901060103100103612073010601032d02027070010c0101020102020202727401060103390103637269010601033d0103642c20010601030f010365206601060103480202642c010601030e0202656e010601031f030174010601033102026c6c010601031302026e20010601032002027368010601034c0202742001060103320103666c65010601034a02027275010c0103020104020103677265010601031d0103682061010601032b030172010601030b0202697401060103450103696e20010601032502027370010601033f020274200106010305030165010601034603016801080103092201036b696e010601032401036c6573010601034b02026c6f010601031402026f77010601031501036e20730106010321030177010601032601036f7220010801031a1d0202772c01060103160103706c65010c0101040102040202706c010c0101030102030202792001060103410103722067010601031b030174010601033602026564010601030d030165010601031e02026973010601033e02027420010601033a02027569010c0103030104030103736b69010601032302027079010601034002027765010601032f0103742063010601033b03016f0106010333030177010601030602026172010601033802026520010601034702026820010801030a220103756974010c0103040104040103772c20010601031702026565010601033002026869010601034402026974010801030822010379207701060103420202656c0106010312040a0909090a090908090909090a080a0c090a0a0a090908090909090a0c0a0a08090a090908090a0a09090a080b090d0c090a08090809090c0a09090a080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(274877906945,X'0000021c043020616e02060103240202637502060103060202666c0206010333030172020601030d0202736b020601031f03016f0206010328030177020601032d0202776902060103130202796502060103180103616e61021001010304010203040301640206010325010362616e020c010102010202010363757202060103070103642066020601030c03017302060103270103656420020601030b02026574020601033002026c6c020601031a0202736802060103360202742002060103310103666c65020601033402027275020c01030e01040202027420020601032b01036720630206010305010368207902060103170103696e200206010322020274200206010311030168020601031501036b696e020601032101036c6573020601033502026c6f020601031b02026f6e0206010302030177020601031c01036e206102060103230202616e020c01010401020402026420020601032602026720020601030401036f6674020601032a02026e67020601030302027720020601031d0103727569020c01030f0104030202766502060103090103736b69020601032002026f66020601032902027765020601032e01037420660206010332030173020601032c03017702060103120202682002060103160103756974020c0103100104040202727602060103080103766564020601030a0103772073020601031e02026565020601032f020269740206010314010379656c0206010319040a09090809080809090f080d0a0a080a090909090a0c090a0a0a09080a0a0909080a0c09090a09090d090a09090a0808090d090a0a0909');
INSERT INTO EntrySearchRecord_data VALUES(412316860417,X'0000023b043020616e030601032502026369030601030702026672030601030e0202696e030601033802026a75030601032902026f72030601031902027365030601032f03016b03060103200202776903060103140103616e640306010326030167031201010401020401031c0103636974030601030802027920030601032d0103642063030601030603016a03060103280103652073030601031f0202676d030601033102026e7403060103340103667275030c01030f0104020103676520030601031e02026d650306010332010368206f03060103180103696379030601032c02026465030601033c02026e20030601032303017303060103390202742003060103120301680306010316030172030601030901036a7569030601032a01036b696e030601032201036d656e030601033301036e206103060103240202642003080103052402026765031201010501020501031d02027369030601033a02027473030601033501036f7261031201010201020201031a0202756e0306010303010372616e031201010301020301031b02026f75030601030202027569030c010310010403030173030601030b0103732066030601030d030169030601033702026567030601033002026964030601033b02026b6903060103210103742077030601031302026820030601031702027275030601030a0202732003060103360103756963030601032b030174030c01031101040402026e64030601030402027320030601030c010377697403060103150103792073030601032e040a09090909090908090a0e0a090a080a09090d0a090a0a0909080908080a0a0a0a0a0f0909100910090c080a080909090a0909090a0b09090a');
INSERT INTO EntrySearchRecord_data VALUES(549755813889,X'00000274043020636c04060103260202656104060103410202666f0406010335030172040601031502026772040601031b0202696e040601032302026f72040801030d3303017604060103100202726f040601030702027573040601033002027769040601033901032c2075040601032f0103616c20040601031303016c040601030402027065040c0101040102040202746904060103430103636c7504060103270103642066040601033403016f040601030c010365206f040601033d02026174040601034202026420040601033302027273040601032c0103666f72040601033602027275040c0103160104020103672069040601032202027261040c01010201020203016f040601031c0103696e200406010324030165040601033b03016704080103202702027420040601031901036c20660406010314030172040601030602026c20040601030502027573040601032801036d616c040601030301036e2063040601032502026420040601030b02026520040601033c02026720040601032101036f7220040a01030e2b0a0202756e040601030902027661040601031102027769040601031e0103722065040601034003016f040601030f030177040601033802026170040c01010301020302026f750406010308030177040601031d0202732c040601032d02027569040c0103170104030103732c20040601032e02026564040601033202026d61040601030202027465040601032a0103742067040601031a02026572040601032b0202696e04060103440103756974040c01031801040402026e64040601030a0202736504060103310301740406010329010376616c0406010312010377696e040801031f1d040a09090809090a080909090a0a080c090a0a080a0909090a0c0a0c080a0809090a0809090a0a0909090c0909090a08080c0908090c0a0909090a09090d0909080a');
INSERT INTO EntrySearchRecord_data VALUES(687194767361,X'000002c60430206120010701032c030170010601010402026372010701033c0202666c010701034902026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102020202727401070103390103637269010701033d0103642c20010701030f0202652001060101020103652061010601010303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030172010701030b0202697401070103450103696e20010701032502027370010701033f020274200107010305030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102040202706c010d0101060102030202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736b69010701032302027079010701034002027765010701032f0103742063010701033b03016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a080909090a090908090909090a080a0c090a0a090a08090908090909090a0c0a0a08090a090908090a0a09090a080b090d0c090a08090809090c0a09090a080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(824633720833,X'000002cf0430206120010701032c030170010d01010401020402026372010701033c0202666c010701034902026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030172010701030b0202697401070103450103696e20010701032502027370010701033f020274200107010305030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736b69010701032302027079010701034002027765010701032f0103742063010701033b03016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b0909090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a08090a090908090a0a09090a080b090d0c090a08090809090c0a09090a080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(962072674305,X'000002cf0430206120010701032c030170010d01010401020402026372010701033c0202666c010701034902026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030172010701030b0202697401070103450103696e20010701032502027370010701033f020274200107010305030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736b69010701032302027079010701034002027765010701032f0103742063010701033b03016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b0909090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a08090a090908090a0a09090a080b090d0c090a08090809090c0a09090a080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(1099511627777,X'000002cf0430206120010701032c030170010d01010401020402026372010701033c0202666c010701034902026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030172010701030b0202697401070103450103696e20010701032502027370010701033f020274200107010305030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736b69010701032302027079010701034002027765010701032f0103742063010701033b03016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b0909090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a08090a090908090a0a09090a080b090d0c090a08090809090c0a09090a080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(1236950581249,X'000002cf0430206120010701032c030170010d01010401020402026372010701033c0202666c010701034902026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030172010701030b0202697401070103450103696e20010701032502027370010701033f020274200107010305030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736b69010701032302027079010701034002027765010701032f0103742063010701033b03016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b0909090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a08090a090908090a0a09090a080b090d0c090a08090809090c0a09090a080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(1374389534721,X'000002e20430206120010701032c030170010d01010401020402026372010701033c0202666c0107010349030172010601040702026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030172010701030b0202697401070103450103696e20010701032502027370010701033f02027420010d010305010405030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736b69010701032302027079010701034002027765010701032f0103742063010701033b030166010601040603016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b090908090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a08090a090c08090a0a09090a080b090d0c090a08090809090c0a09090a08080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(1511828488193,X'000002f60430206120010701032c030170010d01010401020402026372010701033c0202666c0107010349030172010d01034f01040702026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030166010601034e030172010701030b0202697401070103450103696e20010701032502027370010701033f02027420010d010305010405030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736820010601034d02026b69010701032302027079010701034002027765010701032f0103742063010701033b030166010701040603016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b09090b090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a0808090a090c08090a0a09090a080b090d0c090a08090809090c0a0909090a08080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(1649267441665,X'000002f60430206120010701032c030170010d01010401020402026372010701033c0202666c0107010349030172010d01034f01040702026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030166010701034e030172010701030b0202697401070103450103696e20010701032502027370010701033f02027420010d010305010405030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736820010701034d02026b69010701032302027079010701034002027765010701032f0103742063010701033b030166010701040603016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b09090b090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a0808090a090c08090a0a09090a080b090d0c090a08090809090c0a0909090a08080809090a0d0a09090a0a');
INSERT INTO EntrySearchRecord_data VALUES(1786706395137,X'000002f60430206120010701032c030170010d01010401020402026372010701033c0202666c0107010349030172010d01034f01040702026772010701031c02026f7201090103191d02027265010701030c0202736b0107010322030177010701032e02027461010701033702027768010701034303016901090103072202027965010701031101032c206f010701031803017901070103100103612073010701032d02027070010d0101050102050202727401070103390103637269010701033d0103642c20010701030f02026520010d0101020102020103652061010d01010301020303016601070103480202642c010701030e0202656e010701031f030174010701033102026c6c010701031302026e20010701032002027368010701034c0202742001070103320103666c65010701034a02027275010d0103020104020103677265010701031d0103682061010701032b030166010701034e030172010701030b0202697401070103450103696e20010701032502027370010701033f02027420010d010305010405030165010701034603016801090103092201036b696e010701032401036c6573010701034b02026c6f010701031402026f77010701031501036e20730107010321030177010701032601036f7220010901031a1d0202772c01070103160103706c65010d0101070102070202706c010d0101060102060202792001070103410103722067010701031b030174010701033602026564010701030d030165010701031e02026973010701033e02027420010701033a02027569010d0103030104030103736820010701034d02026b69010701032302027079010701034002027765010701032f0103742063010701033b030166010701040603016f0107010333030177010701030602026172010701033802026520010701034702026820010901030a220103756974010d0103040104040103772c20010701031702026565010701033002026869010701034402026974010901030822010379207701070103420202656c0107010312040a0b09090b090a090908090909090a080a0c090a0a0c0d08090908090909090a0c0a0a0808090a090c08090a0a09090a080b090d0c090a08090809090c0a0909090a08080809090a0d0a09090a0a');
CREATE TABLE IF NOT EXISTS 'EntrySearchRecord_idx'(segid, term, pgno, PRIMARY KEY(segid, term)) WITHOUT ROWID;
INSERT INTO EntrySearchRecord_idx VALUES(1,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(2,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(3,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(4,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(5,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(6,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(7,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(8,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(9,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(10,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(11,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(12,X'',2);
INSERT INTO EntrySearchRecord_idx VALUES(13,X'',2);
CREATE TABLE IF NOT EXISTS 'EntrySearchRecord_content'(id INTEGER PRIMARY KEY, c0, c1, c2, c3, c4, c5);
INSERT INTO EntrySearchRecord_content VALUES(1,'de','de Apple','de Apple','fruit with red, yellow, or green skin with a sweet or tart crispy white flesh fr','Fruit fr','00EDEFEB-173E-45CD-836C-EEA2BA514429');
INSERT INTO EntrySearchRecord_content VALUES(2,'','Banana','Banana','long curved fruit with yellow skin and soft sweet flesh','Fruit','D9F70CF9-A479-4141-92E5-44155D063DA2');
INSERT INTO EntrySearchRecord_content VALUES(3,'','Orange','Orange','round citrus fruit with orange skin and juicy segments inside','Fruit','75050583-70D7-4605-A6E6-872B8F36C482');
INSERT INTO EntrySearchRecord_content VALUES(4,'','Grape','Grape','small round or oval fruit growing in clusters, used for wine or eating','Fruit','9E84DB09-1885-4BBB-9482-4891A46F6F49');
CREATE TABLE IF NOT EXISTS 'EntrySearchRecord_docsize'(id INTEGER PRIMARY KEY, sz BLOB);
INSERT INTO EntrySearchRecord_docsize VALUES(1,X'0006064e0600');
INSERT INTO EntrySearchRecord_docsize VALUES(2,X'000404350300');
INSERT INTO EntrySearchRecord_docsize VALUES(3,X'0004043b0300');
INSERT INTO EntrySearchRecord_docsize VALUES(4,X'000303440300');
CREATE TABLE IF NOT EXISTS 'EntrySearchRecord_config'(k PRIMARY KEY, v) WITHOUT ROWID;
INSERT INTO EntrySearchRecord_config VALUES('rank','bm25(5, 3, 4, 1, 2)');
INSERT INTO EntrySearchRecord_config VALUES('version',4);
CREATE TABLE IF NOT EXISTS "LocalResource" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_LocalResource" PRIMARY KEY,

    "LocalPath" TEXT NOT NULL

);
CREATE TABLE IF NOT EXISTS "RemoteResource" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_RemoteResource" PRIMARY KEY,

    "DeletedAt" TEXT NULL,

    "RemoteId" TEXT NULL,

    "SnapshotId" TEXT NULL,

    CONSTRAINT "FK_RemoteResource_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
CREATE TABLE IF NOT EXISTS "Sense" (

    "Id" TEXT NOT NULL CONSTRAINT "PK_Sense" PRIMARY KEY,

    "Definition" jsonb NOT NULL,

    "DeletedAt" TEXT NULL,

    "EntryId" TEXT NOT NULL,

    "Gloss" jsonb NOT NULL,

    "Order" REAL NOT NULL,

    "PartOfSpeechId" TEXT NULL,

    "SemanticDomains" jsonb NOT NULL,

    "SnapshotId" TEXT NULL,

    CONSTRAINT "FK_Sense_Entry_EntryId" FOREIGN KEY ("EntryId") REFERENCES "Entry" ("Id") ON DELETE CASCADE,

    CONSTRAINT "FK_Sense_PartOfSpeech_PartOfSpeechId" FOREIGN KEY ("PartOfSpeechId") REFERENCES "PartOfSpeech" ("Id") ON DELETE SET NULL,

    CONSTRAINT "FK_Sense_Snapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "Snapshots" ("Id") ON DELETE SET NULL

);
INSERT INTO Sense VALUES('B9440091-A9FC-4769-82B1-2EE8D030808D','{"en":{"Spans":[{"Text":"fruit with red, yellow, or green skin with a sweet or tart crispy white flesh","Ws":"en"}]},"fr":{"Spans":[{"Text":"fr","Ws":"fr"}]}}',NULL,'00EDEFEB-173E-45CD-836C-EEA2BA514429','{"en":"Fruit","fr":"fr"}',1.0,'46E4FE08-FFA0-4C8B-BF98-2C56F38904D9','[{"Id":"63403699-07c1-43f3-a47c-069d6e4316e5","Name":{"en":"Universe, Creation"},"Code":"1","DeletedAt":null,"Predefined":true}]','87F3BBAD-ED5D-4CBE-987C-786746A94C06');
INSERT INTO Sense VALUES('C77D7553-209D-45BE-A83E-D07E69808873','{"en":{"Spans":[{"Text":"long curved fruit with yellow skin and soft sweet flesh","Ws":"en"}]}}',NULL,'D9F70CF9-A479-4141-92E5-44155D063DA2','{"en":"Fruit"}',1.0,NULL,'[]','DCFD50CE-413B-4D12-B4E7-6EED289D3AB2');
INSERT INTO Sense VALUES('241D95A7-6D90-411B-8569-5648CC40B42B','{"en":{"Spans":[{"Text":"round citrus fruit with orange skin and juicy segments inside","Ws":"en"}]}}',NULL,'75050583-70D7-4605-A6E6-872B8F36C482','{"en":"Fruit"}',1.0,NULL,'[]','ABC7FA87-8A65-4C55-8243-719DA4372BDB');
INSERT INTO Sense VALUES('1B423127-DEB2-468D-8100-E1F1BFF20826','{"en":{"Spans":[{"Text":"small round or oval fruit growing in clusters, used for wine or eating","Ws":"en"}]}}',NULL,'9E84DB09-1885-4BBB-9482-4891A46F6F49','{"en":"Fruit"}',1.0,NULL,'[]','45CA39E0-64CD-4EE3-B721-476791CBCFED');
CREATE UNIQUE INDEX "IX_ComplexFormComponents_ComplexFormEntryId_ComponentEntryId" ON "ComplexFormComponents" ("ComplexFormEntryId", "ComponentEntryId") WHERE ComponentSenseId IS NULL;
CREATE UNIQUE INDEX "IX_ComplexFormComponents_ComplexFormEntryId_ComponentEntryId_ComponentSenseId" ON "ComplexFormComponents" ("ComplexFormEntryId", "ComponentEntryId", "ComponentSenseId") WHERE ComponentSenseId IS NOT NULL;
CREATE INDEX "IX_ComplexFormComponents_ComponentEntryId" ON "ComplexFormComponents" ("ComponentEntryId");
CREATE INDEX "IX_ComplexFormComponents_ComponentSenseId" ON "ComplexFormComponents" ("ComponentSenseId");
CREATE UNIQUE INDEX "IX_ComplexFormComponents_SnapshotId" ON "ComplexFormComponents" ("SnapshotId");
CREATE UNIQUE INDEX "IX_ComplexFormType_SnapshotId" ON "ComplexFormType" ("SnapshotId");
CREATE UNIQUE INDEX "IX_Entry_SnapshotId" ON "Entry" ("SnapshotId");
CREATE UNIQUE INDEX "IX_PartOfSpeech_SnapshotId" ON "PartOfSpeech" ("SnapshotId");
CREATE UNIQUE INDEX "IX_SemanticDomain_SnapshotId" ON "SemanticDomain" ("SnapshotId");
CREATE UNIQUE INDEX "IX_Snapshots_CommitId_EntityId" ON "Snapshots" ("CommitId", "EntityId");
CREATE UNIQUE INDEX "IX_WritingSystem_SnapshotId" ON "WritingSystem" ("SnapshotId");
CREATE UNIQUE INDEX "IX_WritingSystem_WsId_Type" ON "WritingSystem" ("WsId", "Type");
CREATE UNIQUE INDEX "IX_Publication_SnapshotId" ON "Publication" ("SnapshotId");
CREATE INDEX "IX_Snapshots_EntityId" ON "Snapshots" ("EntityId");
CREATE INDEX "IX_ExampleSentence_SenseId" ON "ExampleSentence" ("SenseId");
CREATE UNIQUE INDEX "IX_ExampleSentence_SnapshotId" ON "ExampleSentence" ("SnapshotId");
CREATE UNIQUE INDEX "IX_RemoteResource_SnapshotId" ON "RemoteResource" ("SnapshotId");
CREATE INDEX "IX_Sense_EntryId" ON "Sense" ("EntryId");
CREATE INDEX "IX_Sense_PartOfSpeechId" ON "Sense" ("PartOfSpeechId");
CREATE UNIQUE INDEX "IX_Sense_SnapshotId" ON "Sense" ("SnapshotId");
PRAGMA writable_schema=OFF;
COMMIT;
