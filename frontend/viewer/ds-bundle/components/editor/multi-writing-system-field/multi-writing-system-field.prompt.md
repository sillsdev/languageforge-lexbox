# Multi-writing-system field

FW Lite's signature editor control: one linguistic field (lexeme form, gloss, definition…) holds a value **per writing system**. Rendered as a stack of rows, each row = a writing-system code label + an input. Svelte: `import MultiWsInput from '$lib/components/field-editors/multi-ws-input.svelte';` (rich variant: `rich-multi-ws-input.svelte`).

**Real markup** (from the live editor): a row is
`<div class="grid ... items-baseline"><label>Deu</label><input data-slot="input" class="…input…"></div>`.
The writing-system code label uses the standard Label classes, muted; the input uses the default Input recipe. Multiple writing systems stack vertically under a bold field title.

**When designing FW Lite screens**: dictionary/entry fields are almost always multi-writing-system — prefer this pattern over a single input for any linguistic content.
