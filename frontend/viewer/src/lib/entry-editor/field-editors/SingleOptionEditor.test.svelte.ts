import {readable} from 'svelte/store'
import {beforeAll, beforeEach, describe, expect, expectTypeOf, test} from 'vitest'

import {render, screen} from '@testing-library/svelte'
import userEvent, {type UserEvent} from '@testing-library/user-event'
import SingleOptionEditor from './SingleOptionEditor.svelte'
import type {ComponentProps} from 'svelte'
import {WritingSystemService} from '$lib/writing-system-service'
import {views} from '$lib/views/view-data'
import {writingSystems} from '$lib/demo-entry-data'
import {polyfillMockAnimations} from '../../../test-utils'

type Option = { id: string };

const value = '2';
const options: Option[] = ['1', '2', '3', '4', '5'].map(id => ({id}));

const context = new Map<string, unknown>([
  ['writingSystems', readable(new WritingSystemService(writingSystems))],
  ['currentView', readable(views[0])],
]);


const reusedProps: Pick<ComponentProps<SingleOptionEditor<string, {id: string}>>, 'id' | 'wsType' | 'name' | 'readonly'> = {
  id: 'partOfSpeechId',
  wsType: 'vernacular',
  name: 'test',
  readonly: false,
};

beforeAll(() => {
  polyfillMockAnimations();
});

describe('SingleOptionEditor', () => {

  let user: UserEvent;

  const props = $state({
    ...reusedProps,
    valueIsId: true,
    value,
    options,
    getOptionLabel: (option: {id: string}) => option.id,
  } as const);

  beforeEach(() => {
    user = userEvent.setup();
    render(SingleOptionEditor<string, {id: string}>, {
      context,
      props,
    });
  });

  test('can change selection', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByRole('option', {name: '5'}));
    expect(props.value).toBe('5');

    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByRole('option', {name: '3'}));
    expect(props.value).toBe('3');
  });
});

describe('SingleOptionEditor configurations', () => {

  test('supports string or { id: string} values and { id: string } options out of the box', () => {
    expectTypeOf({
      ...reusedProps,
      value,
      options,
      getOptionLabel: (option: Option) => option.id,
      valueIsId: true,
    } as const).toMatchTypeOf<ComponentProps<SingleOptionEditor<string, { id: string }>>>();

    expectTypeOf({
      ...reusedProps,
      value: {id: value},
      options,
      getOptionLabel: (option: Option) => option.id,
    } as const).toMatchTypeOf<ComponentProps<SingleOptionEditor<{ id: string }, { id: string }>>>();
  });

  test('requires valueIsId to be set to true for out of the box support for string values, because we need to know the type at runtime', () => {
    type Props = ComponentProps<SingleOptionEditor<string, { id: string }>>;
    expectTypeOf({
      ...reusedProps,
      value,
      options,
      getOptionLabel: (option: Option) => option.id,
      valueIsId: true,
    } as const).toMatchTypeOf<Props>();

    expectTypeOf({
      ...reusedProps,
      value,
      options,
      getOptionLabel: (option: Option) => option.id,
      // valueIsId: true,
    } as const).not.toMatchTypeOf<Props>();
  });

  test('requires getValueId and getValueById for unsupported value types', () => {
    type Props = ComponentProps<SingleOptionEditor<{ value: string } | undefined, { id: string }>>;
    expectTypeOf({
      ...reusedProps,
      value: {value},
      getValueId: (v: {value: string} | undefined) => v?.value,
      getValueById: (id: string | undefined) => id ? {value: id} : undefined,
      options,
      getOptionLabel: (option: Option) => option.id,
    } as const).toMatchTypeOf<Props>();

    expectTypeOf({
      ...reusedProps,
      value: {value},
      // getValueId: (v: {value: string} | undefined) => v?.value,
      // getValueById: (id: string | undefined) => id ? {value: id} : undefined,
      options,
      getOptionLabel: (option: Option) => option.id,
    } as const).not.toMatchTypeOf<Props>();
  });

  test('requires getOptionId for unsupported option types', () => {
    type Props = ComponentProps<SingleOptionEditor<string, { code: string }>>;
    expectTypeOf({
      ...reusedProps,
      value,
      valueIsId: true,
      options: options.map((option) => ({code: option.id})),
      getOptionLabel: (option: {code: string}) => option.code,
      getOptionId: (option: {code: string}) => option.code,
    } as const).toMatchTypeOf<Props>();

    expectTypeOf({
      ...reusedProps,
      value,
      valueIsId: true,
      options: options.map((option) => ({code: option.id})),
      getOptionLabel: (option: {code: string}) => option.code,
      // getOptionId: (option: {code: string}) => option.code,
    } as const).not.toMatchTypeOf<Props>();
  });
});
