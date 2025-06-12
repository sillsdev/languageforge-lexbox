import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';

export interface IMultiString {
    [key: string]: string;
}

export interface IRichMultiString {
    [key: string]: IRichString;
}
