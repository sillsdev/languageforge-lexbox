import type { AnyZodObject } from 'zod';
import type { SuperForm } from 'sveltekit-superforms/client';
import type { ZodValidation } from 'sveltekit-superforms';

export type AnySuperForm = SuperForm<ZodValidation<AnyZodObject>>;

// So that strings that represent errors (e.g. the return type of form submit callbacks)
// can be explicit about what they actually mean.
export type ErrorMessage = string | undefined | void;
