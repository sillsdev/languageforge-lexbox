/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export interface WritingSystem {
    /**
     * This is a GUID for the writing system, not used in FWData.
     */
    id: string;
    /**
     * This is the language tag for the writing system, e.g. "en" for English.
     */
    wsId: string;
    name: string;
    abbreviation: string;
    font: string;
    exemplars: string[];
}
