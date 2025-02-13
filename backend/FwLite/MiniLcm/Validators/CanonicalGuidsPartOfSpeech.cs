﻿using System.Collections.Frozen;

namespace MiniLcm.Validators;

public static class CanonicalGuidsPartOfSpeech
{
    // GUID list taken from src/SIL.LCModel/Templates/GOLDEtic.xml in liblcm
    // TODO: Consider loading GOLDEtic.xml into app as a resource and add singleton providing access to it, then look up GUIDs there rather than using this hardcoded list
    public static readonly FrozenSet<Guid> CanonicalPosGuids = [
        new Guid([128, 117, 208, 48, 82, 80, 145, 77, 188, 36, 70, 155, 139, 45, 125, 249]),       // 30d07580-5052-4d91-bc24-469b8b2d7df9
        new Guid([168, 94, 17, 174, 215, 44, 1, 69, 138, 231, 220, 99, 142, 79, 23, 197]),         // ae115ea8-2cd7-4501-8ae7-dc638e4f17c5
        new Guid([184, 178, 241, 24, 227, 12, 137, 72, 144, 233, 0, 63, 237, 106, 150, 159]),      // 18f1b2b8-0ce3-4889-90e9-003fed6a969f
        new Guid([237, 90, 62, 146, 74, 216, 176, 72, 155, 122, 51, 28, 19, 54, 134, 74]),         // 923e5aed-d84a-48b0-9b7a-331c1336864a
        new Guid([8, 254, 228, 70, 160, 255, 139, 76, 191, 152, 44, 86, 243, 137, 4, 217]),        // 46e4fe08-ffa0-4c8b-bf98-2c56f38904d9
        new Guid([3, 102, 97, 49, 221, 170, 175, 71, 167, 16, 203, 86, 95, 187, 189, 87]),         // 31616603-aadd-47af-a710-cb565fbbbd57
        new Guid([172, 9, 18, 19, 241, 184, 170, 68, 180, 240, 154, 152, 62, 61, 91, 173]),        // 131209ac-b8f1-44aa-b4f0-9a983e3d5bad
        new Guid([167, 130, 6, 110, 212, 239, 201, 67, 176, 131, 34, 196, 206, 36, 84, 25]),       // 6e0682a7-efd4-43c9-b083-22c4ce245419
        new Guid([50, 67, 172, 117, 69, 164, 232, 76, 145, 142, 178, 123, 4, 7, 55, 69]),          // 75ac4332-a445-4ce8-918e-b27b04073745
        new Guid([198, 91, 122, 45, 199, 187, 233, 75, 160, 54, 61, 4, 109, 188, 101, 247]),       // 2d7a5bc6-bbc7-4be9-a036-3d046dbc65f7
        new Guid([50, 47, 5, 9, 101, 191, 10, 64, 129, 121, 106, 28, 84, 239, 48, 201]),           // 09052f32-bf65-400a-8179-6a1c54ef30c9
        new Guid([109, 144, 169, 160, 135, 217, 203, 66, 138, 101, 245, 73, 70, 43, 22, 252]),     // a0a9906d-d987-42cb-8a65-f549462b16fc
        new Guid([2, 165, 123, 143, 201, 231, 196, 75, 168, 129, 176, 203, 27, 99, 4, 102]),       // 8f7ba502-e7c9-4bc4-a881-b0cb1b630466
        new Guid([163, 34, 242, 197, 170, 225, 80, 66, 177, 150, 217, 79, 14, 176, 212, 123]),     // c5f222a3-e1aa-4250-b196-d94f0eb0d47b
        new Guid([238, 200, 241, 109, 48, 85, 128, 65, 153, 232, 190, 42, 250, 176, 246, 13]),     // 6df1c8ee-5530-4180-99e8-be2afab0f60d
        new Guid([222, 101, 63, 175, 213, 176, 67, 66, 161, 150, 83, 182, 123, 214, 164, 223]),    // af3f65de-b0d5-4243-a196-53b67bd6a4df
        new Guid([20, 62, 171, 146, 175, 225, 127, 78, 132, 146, 227, 122, 31, 56, 110, 63]),      // 92ab3e14-e1af-4e7f-8492-e37a1f386e3f
        new Guid([93, 98, 124, 208, 139, 255, 78, 76, 153, 190, 227, 43, 41, 36, 98, 110]),        // d07c625d-ff8b-4c4e-99be-e32b2924626e
        new Guid([215, 100, 50, 9, 195, 6, 225, 66, 188, 77, 90, 150, 92, 230, 56, 135]),          // 093264d7-06c3-42e1-bc4d-5a965ce63887
        new Guid([180, 89, 167, 164, 16, 90, 122, 77, 128, 163, 172, 207, 91, 216, 64, 177]),      // a4a759b4-5a10-4d7a-80a3-accf5bd840b1
        new Guid([14, 51, 128, 230, 65, 43, 236, 75, 185, 107, 81, 71, 67, 196, 124, 174]),        // e680330e-2b41-4bec-b96b-514743c47cae
        new Guid([59, 31, 49, 165, 152, 255, 210, 71, 142, 206, 177, 172, 160, 58, 139, 189]),     // a5311f3b-ff98-47d2-8ece-b1aca03a8bbd
        new Guid([125, 235, 48, 179, 61, 244, 49, 69, 132, 110, 92, 213, 130, 8, 81, 173]),        // b330eb7d-f43d-4531-846e-5cd5820851ad
        new Guid([41, 2, 3, 28, 250, 175, 41, 71, 151, 115, 135, 129, 0, 193, 253, 40]),           // 1c030229-affa-4729-9773-878100c1fd28
        new Guid([214, 67, 157, 61, 124, 82, 121, 78, 190, 0, 130, 207, 45, 15, 217, 189]),        // 3d9d43d6-527c-4e79-be00-82cf2d0fd9bd
        new Guid([195, 44, 101, 14, 8, 239, 177, 76, 139, 115, 166, 142, 189, 142, 124, 4]),       // 0e652cc3-ef08-4cb1-8b73-a68ebd8e7c04
        new Guid([140, 239, 178, 37, 126, 216, 104, 72, 137, 140, 143, 83, 117, 175, 238, 179]),   // 25b2ef8c-d87e-4868-898c-8f5375afeeb3
        new Guid([24, 203, 96, 204, 103, 80, 43, 68, 167, 64, 211, 185, 19, 178, 97, 10]),         // cc60cb18-5067-442b-a740-d3b913b2610a
        new Guid([98, 255, 45, 211, 23, 65, 98, 71, 168, 135, 150, 71, 132, 6, 118, 155]),         // d32dff62-4117-4762-a887-96478406769b
        new Guid([211, 31, 228, 168, 67, 227, 124, 76, 170, 5, 1, 234, 61, 213, 207, 181]),        // a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5
        new Guid([239, 96, 83, 8, 108, 22, 36, 67, 165, 196, 95, 77, 142, 171, 247, 90]),          // 085360ef-166c-4324-a5c4-5f4d8eabf75a
        new Guid([200, 35, 181, 24, 202, 125, 97, 67, 147, 216, 109, 3, 147, 53, 242, 109]),       // 18b523c8-7dca-4361-93d8-6d039335f26d
        new Guid([255, 29, 211, 168, 221, 28, 199, 75, 171, 58, 190, 254, 246, 119, 17, 194]),     // a8d31dff-1cdd-4bc7-ab3a-befef67711c2
        new Guid([233, 18, 68, 88, 150, 58, 81, 66, 153, 226, 67, 143, 19, 148, 67, 46]),          // 584412e9-3a96-4251-99e2-438f1394432e
        new Guid([49, 236, 253, 131, 91, 225, 231, 64, 188, 210, 105, 19, 76, 90, 15, 205]),       // 83fdec31-e15b-40e7-bcd2-69134c5a0fcd
        new Guid([188, 139, 117, 110, 64, 43, 125, 66, 183, 159, 191, 20, 167, 201, 108, 106]),    // 6e758bbc-2b40-427d-b79f-bf14a7c96c6a
        new Guid([88, 63, 133, 198, 76, 167, 62, 72, 148, 148, 115, 32, 2, 167, 171, 91]),         // c6853f58-a74c-483e-9494-732002a7ab5b
        new Guid([145, 94, 69, 7, 138, 17, 125, 77, 132, 140, 57, 190, 221, 53, 90, 61]),          // 07455e91-118a-4d7d-848c-39bedd355a3d
        new Guid([51, 83, 67, 18, 35, 196, 215, 67, 172, 249, 96, 40, 227, 210, 10, 66]),          // 12435333-c423-43d7-acf9-6028e3d20a42
        new Guid([171, 158, 172, 241, 140, 95, 207, 65, 178, 52, 229, 52, 5, 170, 170, 197]),      // f1ac9eab-5f8c-41cf-b234-e53405aaaac5
        new Guid([210, 106, 103, 78, 45, 84, 29, 70, 157, 120, 29, 188, 181, 94, 196, 86]),        // 4e676ad2-542d-461d-9d78-1dbcb55ec456
        new Guid([214, 120, 252, 164, 145, 117, 179, 79, 142, 221, 130, 241, 10, 227, 115, 157]),  // a4fc78d6-7591-4fb3-8edd-82f10ae3739d
        new Guid([203, 208, 231, 229, 197, 54, 125, 73, 131, 28, 203, 97, 78, 40, 61, 124]),       // e5e7d0cb-36c5-497d-831c-cb614e283d7c
        new Guid([103, 182, 139, 226, 170, 252, 110, 76, 148, 75, 155, 144, 104, 58, 37, 112]),    // e28bb667-fcaa-4c6e-944b-9b90683a2570
        new Guid([105, 221, 153, 213, 69, 180, 39, 70, 167, 243, 185, 100, 122, 189, 185, 5]),     // d599dd69-b445-4627-a7f3-b9647abdb905
        new Guid([253, 76, 39, 163, 95, 34, 253, 69, 136, 81, 167, 177, 161, 225, 3, 122]),        // a3274cfd-225f-45fd-8851-a7b1a1e1037a
        new Guid([133, 171, 217, 181, 147, 250, 106, 77, 137, 43, 131, 126, 251, 41, 158, 247]),   // b5d9ab85-fa93-4d6a-892b-837efb299ef7
        new Guid([137, 58, 173, 47, 215, 71, 42, 71, 168, 204, 39, 14, 126, 62, 2, 57]),           // 2fad3a89-47d7-472a-a8cc-270e7e3e0239
        new Guid([127, 80, 245, 152, 81, 191, 228, 67, 142, 8, 224, 102, 195, 108, 109, 110]),     // 98f5507f-bf51-43e4-8e08-e066c36c6d6e
        new Guid([2, 181, 227, 100, 245, 144, 240, 77, 146, 18, 101, 246, 229, 201, 108, 48]),     // 64e3b502-90f5-4df0-9212-65f6e5c96c30
        new Guid([249, 84, 92, 96, 31, 168, 202, 75, 141, 140, 166, 251, 8, 194, 150, 118]),       // 605c54f9-a81f-4bca-8d8c-a6fb08c29676
        new Guid([108, 12, 169, 217, 117, 53, 55, 73, 191, 175, 179, 88, 90, 25, 84, 169]),        // d9a90c6c-3575-4937-bfaf-b3585a1954a9
        new Guid([226, 255, 155, 63, 155, 218, 250, 66, 175, 189, 215, 202, 139, 202, 125, 74]),   // 3f9bffe2-da9b-42fa-afbd-d7ca8bca7d4a
        new Guid([246, 102, 255, 134, 116, 7, 122, 64, 160, 220, 62, 234, 248, 115, 218, 247]),    // 86ff66f6-0774-407a-a0dc-3eeaf873daf7
        new Guid([14, 160, 242, 85, 7, 95, 206, 74, 138, 68, 135, 148, 237, 26, 56, 168]),         // 55f2a00e-5f07-4ace-8a44-8794ed1a38a8
        new Guid([211, 241, 173, 239, 10, 88, 75, 78, 169, 76, 63, 29, 110, 89, 197, 252]),        // efadf1d3-580a-4e4b-a94c-3f1d6e59c5fc
        new Guid([9, 255, 89, 68, 224, 158, 80, 75, 135, 135, 174, 64, 253, 118, 211, 183]),       // 4459ff09-9ee0-4b50-8787-ae40fd76d3b7
        new Guid([49, 41, 113, 84, 47, 68, 213, 66, 134, 52, 241, 43, 210, 227, 16, 206]),         // 54712931-442f-42d5-8634-f12bd2e310ce
    ];
}
