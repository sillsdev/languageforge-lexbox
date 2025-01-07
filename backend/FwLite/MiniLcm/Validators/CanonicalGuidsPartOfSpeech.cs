﻿using FluentValidation;

namespace MiniLcm.Validators;

public static class CanonicalGuidsPartOfSpeech
{
    // GUID list taken from src/SIL.LCModel/Templates/GOLDEtic.xml in liblcm
    // TODO: Consider loading GOLDEtic.xml into app as a resource and add singleton providing access to it, then look up GUIDs there rather than using this hardcoded list
    public static HashSet<Guid> CanonicalPosGuids = [
        new Guid("30d07580-5052-4d91-bc24-469b8b2d7df9"),
        new Guid("ae115ea8-2cd7-4501-8ae7-dc638e4f17c5"),
        new Guid("18f1b2b8-0ce3-4889-90e9-003fed6a969f"),
        new Guid("923e5aed-d84a-48b0-9b7a-331c1336864a"),
        new Guid("46e4fe08-ffa0-4c8b-bf98-2c56f38904d9"),
        new Guid("31616603-aadd-47af-a710-cb565fbbbd57"),
        new Guid("131209ac-b8f1-44aa-b4f0-9a983e3d5bad"),
        new Guid("6e0682a7-efd4-43c9-b083-22c4ce245419"),
        new Guid("75ac4332-a445-4ce8-918e-b27b04073745"),
        new Guid("2d7a5bc6-bbc7-4be9-a036-3d046dbc65f7"),
        new Guid("09052f32-bf65-400a-8179-6a1c54ef30c9"),
        new Guid("a0a9906d-d987-42cb-8a65-f549462b16fc"),
        new Guid("8f7ba502-e7c9-4bc4-a881-b0cb1b630466"),
        new Guid("c5f222a3-e1aa-4250-b196-d94f0eb0d47b"),
        new Guid("6df1c8ee-5530-4180-99e8-be2afab0f60d"),
        new Guid("af3f65de-b0d5-4243-a196-53b67bd6a4df"),
        new Guid("92ab3e14-e1af-4e7f-8492-e37a1f386e3f"),
        new Guid("d07c625d-ff8b-4c4e-99be-e32b2924626e"),
        new Guid("093264d7-06c3-42e1-bc4d-5a965ce63887"),
        new Guid("a4a759b4-5a10-4d7a-80a3-accf5bd840b1"),
        new Guid("e680330e-2b41-4bec-b96b-514743c47cae"),
        new Guid("a5311f3b-ff98-47d2-8ece-b1aca03a8bbd"),
        new Guid("b330eb7d-f43d-4531-846e-5cd5820851ad"),
        new Guid("1c030229-affa-4729-9773-878100c1fd28"),
        new Guid("3d9d43d6-527c-4e79-be00-82cf2d0fd9bd"),
        new Guid("0e652cc3-ef08-4cb1-8b73-a68ebd8e7c04"),
        new Guid("25b2ef8c-d87e-4868-898c-8f5375afeeb3"),
        new Guid("cc60cb18-5067-442b-a740-d3b913b2610a"),
        new Guid("d32dff62-4117-4762-a887-96478406769b"),
        new Guid("a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5"),
        new Guid("085360ef-166c-4324-a5c4-5f4d8eabf75a"),
        new Guid("18b523c8-7dca-4361-93d8-6d039335f26d"),
        new Guid("a8d31dff-1cdd-4bc7-ab3a-befef67711c2"),
        new Guid("584412e9-3a96-4251-99e2-438f1394432e"),
        new Guid("83fdec31-e15b-40e7-bcd2-69134c5a0fcd"),
        new Guid("6e758bbc-2b40-427d-b79f-bf14a7c96c6a"),
        new Guid("c6853f58-a74c-483e-9494-732002a7ab5b"),
        new Guid("07455e91-118a-4d7d-848c-39bedd355a3d"),
        new Guid("12435333-c423-43d7-acf9-6028e3d20a42"),
        new Guid("f1ac9eab-5f8c-41cf-b234-e53405aaaac5"),
        new Guid("4e676ad2-542d-461d-9d78-1dbcb55ec456"),
        new Guid("a4fc78d6-7591-4fb3-8edd-82f10ae3739d"),
        new Guid("e5e7d0cb-36c5-497d-831c-cb614e283d7c"),
        new Guid("e28bb667-fcaa-4c6e-944b-9b90683a2570"),
        new Guid("d599dd69-b445-4627-a7f3-b9647abdb905"),
        new Guid("a3274cfd-225f-45fd-8851-a7b1a1e1037a"),
        new Guid("b5d9ab85-fa93-4d6a-892b-837efb299ef7"),
        new Guid("2fad3a89-47d7-472a-a8cc-270e7e3e0239"),
        new Guid("98f5507f-bf51-43e4-8e08-e066c36c6d6e"),
        new Guid("64e3b502-90f5-4df0-9212-65f6e5c96c30"),
        new Guid("605c54f9-a81f-4bca-8d8c-a6fb08c29676"),
        new Guid("d9a90c6c-3575-4937-bfaf-b3585a1954a9"),
        new Guid("3f9bffe2-da9b-42fa-afbd-d7ca8bca7d4a"),
        new Guid("86ff66f6-0774-407a-a0dc-3eeaf873daf7"),
        new Guid("55f2a00e-5f07-4ace-8a44-8794ed1a38a8"),
        new Guid("efadf1d3-580a-4e4b-a94c-3f1d6e59c5fc"),
        new Guid("4459ff09-9ee0-4b50-8787-ae40fd76d3b7"),
        new Guid("54712931-442f-42d5-8634-f12bd2e310ce"),
    ];
}
