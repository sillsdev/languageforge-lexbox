/* eslint-disable @typescript-eslint/no-unsafe-return, @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-assignment */
import {readable} from 'svelte/store'
import {beforeAll, beforeEach, describe, expect, expectTypeOf, test} from 'vitest'

import {render, screen} from '@testing-library/svelte'
import userEvent, {type UserEvent} from '@testing-library/user-event'
import MultiOptionEditor from './MultiOptionEditor.svelte'
import type {ComponentProps} from 'svelte'
import {WritingSystemService} from '$lib/writing-system-service'
import {WritingSystemType} from '$lib/dotnet-types'
import {views} from '$lib/views/view-data'
import {polyfillMockAnimations} from '../../../test-utils'

type Option = {id: string};

const value = ['2', '3', '4'];
const options: Option[] = ['1', '2', '3', '4', '5'].map(id => ({id}));

const context = new Map<string, unknown>([
  ['writingSystems', readable(new WritingSystemService({
    analysis: [],
    vernacular: [{
      id: 'test',
      wsId: 'test',
      name: 'test',
      abbreviation: 'test',
      font: 'test',
      exemplars: ['test'],
      order: 0,
      type: WritingSystemType.Vernacular,
    }],
  }))],
  ['currentView', readable(views[0])],
]);

let user: UserEvent;

const reusedProps: Pick<ComponentProps<MultiOptionEditor<string, {id: string}>>, 'id' | 'wsType' | 'name' | 'readonly'> = {
  id: 'semanticDomains',
  wsType: 'vernacular',
  name: 'test',
  readonly: false,
};

let props = $state<ComponentProps<MultiOptionEditor<string, {id: string}>>>()!;

beforeEach(() => {
  user = userEvent.setup();
  props = {
    ...reusedProps,
    valuesAreIds: true,
    value,
    options,
    getOptionLabel: (option) => option.id,
  };
  render(MultiOptionEditor<string, {id: string}>, {
    context,
    props,
  });
});

beforeAll(() => {
  polyfillMockAnimations();
});

describe('MultiOptionEditor value sorting', () => {
  test('appends new options to the end in the order they\'re selected', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('1'));
    await user.click(screen.getByLabelText('5'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(props.value).toStrictEqual(['2', '3', '4', '1', '5']);
  });

  test('removes deselected options without changing the order', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('3'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(props.value).toStrictEqual(['2', '4']);
  });

  test('moves double-toggled options to the end', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('3'));
    await user.click(screen.getByLabelText('3'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(props.value).toStrictEqual(['2', '4', '3']);
  });
});

describe('MultiOptionEditor displayed sorting', () => {
  test('matches option sorting if `preserveOrder` option is NOT set', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('1'));
    await user.click(screen.getByLabelText('5'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(props.value).toStrictEqual(['2', '3', '4', '1', '5']);
    expect(screen.getByRole<HTMLInputElement>('textbox').value).toBe('1, 2, 3, 4, 5');
  });

  test('matches values sorting if `preserveOrder` option is set', async () => {
    props.preserveOrder = true;
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('1'));
    await user.click(screen.getByLabelText('5'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(props.value).toStrictEqual(['2', '3', '4', '1', '5']);
    expect(screen.getByRole<HTMLInputElement>('textbox').value).toBe('2, 3, 4, 1, 5');
  });
});

describe('MultiOptionEditor configurations', () => {
  test('supports string or { id: string} values and { id: string } options out of the box', () => {
    expectTypeOf({
      ...reusedProps,
      value,
      options,
      getOptionLabel: (option: Option) => option.id,
      valuesAreIds: true,
    } as const).toMatchTypeOf<ComponentProps<MultiOptionEditor<string, { id: string }>>>();

    expectTypeOf({
      ...reusedProps,
      value: value.map(id => ({id})),
      options,
      getOptionLabel: (option: Option) => option.id,
    } as const).toMatchTypeOf<ComponentProps<MultiOptionEditor<{id: string}, {id: string}>>>();
  });

  test('requires valuesAreIds to be set to true for out of the box support for string values, because we need to know the type at runtime', () => {
    type Props = ComponentProps<MultiOptionEditor<string, { id: string }>>;
    expectTypeOf({
      ...reusedProps,
      value,
      options,
      getOptionLabel: (option: Option) => option.id,
      valuesAreIds: true,
    } as const).toMatchTypeOf<Props>();

    expectTypeOf({
      ...reusedProps,
      value,
      options,
      getOptionLabel: (option: Option) => option.id,
      // valuesAreIds: true,
    } as const).not.toMatchTypeOf<Props>();
  });

  test('requires getValueId and getValueById for unsupported value types', () => {
    type Props = ComponentProps<MultiOptionEditor<{ value: string }, { id: string }>>;
    expectTypeOf({
      ...reusedProps,
      value: value.map((v) => ({value: v})),
      getValueId: (v: {value: string}) => v.value,
      getValueById: (id: string) => ({value: id}),
      options,
      getOptionLabel: (option: Option) => option.id,
    } as const).toMatchTypeOf<Props>();

    expectTypeOf({
      ...reusedProps,
      value: value.map((v) => ({value: v})),
      // getValueId: (v: {value: string}) => v.value,
      // getValueById: (id: string) => ({value: id}),
      options,
      getOptionLabel: (option: Option) => option.id,
    } as const).not.toMatchTypeOf<Props>();
  });

  test('requires getOptionId for unsupported option types', () => {
    type Props = ComponentProps<MultiOptionEditor<string, { code: string }>>;
    expectTypeOf({
      ...reusedProps,
      value,
      valuesAreIds: true,
      options: options.map((option: Option) => ({code: option.id})),
      getOptionLabel: (option: {code: string}) => option.code,
      getOptionId: (option: {code: string}) => option.code,
    } as const).toMatchTypeOf<Props>();

    expectTypeOf({
      ...reusedProps,
      value,
      valuesAreIds: true,
      options: options.map((option: Option) => ({code: option.id})),
      getOptionLabel: (option: {code: string}) => option.code,
      // getOptionId: (option: {code: string}) => option.code,
    } as const).not.toMatchTypeOf<Props>();
  });
});
