/* eslint-disable @typescript-eslint/no-unsafe-return, @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-assignment */
import {readable} from 'svelte/store'
import {beforeEach, describe, expect, test} from 'vitest'

import {render, screen} from '@testing-library/svelte'
import userEvent, {type UserEvent} from '@testing-library/user-event'
import {getState} from '../../utils/test-utils'
import MultiOptionEditor from './MultiOptionEditor.svelte'

const value = ['2', '3', '4'];
const options = ['1', '2', '3', '4', '5'].map(id => ({id}));

const context = new Map<string, unknown>([
  ['writingSystems', readable({
    analysis: [],
    vernacular: [{
      id: 'test',
    }],
  })],
  ['currentView', readable({
    fields: {'test': {show: true}},
  })],
]);

const reusedProps = {
  id: 'test',
  wsType: 'vernacular',
  name: 'test',
  readonly: false,
} as const;

let user: UserEvent;
let component: MultiOptionEditor<string, {id: string}>;

beforeEach(() => {
  user = userEvent.setup();
  ({component} = render(MultiOptionEditor<string, {id: string}>, {
    context,
    props: {
      ...reusedProps,
      idValues: true,
      value,
      options,
      getOptionLabel: (option) => option.id,
    }
  }));
});

describe('MultiOptionEditor value sorting', () => {
  test('appends new options to the end in the order they\'re selected', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('1'));
    await user.click(screen.getByLabelText('5'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(getState(component).value).toStrictEqual(['2', '3', '4', '1', '5']);
  });

  test('removes deselected options without changing the order', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('3'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(getState(component).value).toStrictEqual(['2', '4']);
  });

  test('moves double-toggled options to the end', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('3'));
    await user.click(screen.getByLabelText('3'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(getState(component).value).toStrictEqual(['2', '4', '3']);
  });
});

describe('MultiOptionEditor displayed sorting', () => {
  test('matches option sorting if `ordered` option is NOT set', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('1'));
    await user.click(screen.getByLabelText('5'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(getState(component).value).toStrictEqual(['2', '3', '4', '1', '5']);
    expect(screen.getByRole<HTMLInputElement>('textbox').value).toBe('1, 2, 3, 4, 5');
  });

  test('matches values sorting if `ordered` option is set', async () => {
    component.$set({ordered: true});
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByLabelText('1'));
    await user.click(screen.getByLabelText('5'));
    await user.click(screen.getByRole('button', {name: 'Apply'}));
    expect(getState(component).value).toStrictEqual(['2', '3', '4', '1', '5']);
    expect(screen.getByRole<HTMLInputElement>('textbox').value).toBe('2, 3, 4, 1, 5');
  });
});

describe('MultiOptionEditor configurations', () => {
  test('supports string or { id: string} values and { id: string } options out of the box', () => {
    () => render(MultiOptionEditor<string, {id: string}>, {
      context,
      props: {
        ...reusedProps,
        idValues: true,
        value,
        options: [],
        getOptionLabel: (option) => option.id,
      }
    });
    () => render(MultiOptionEditor<{id: string}, {id: string}>, {
      context,
      props: {
        ...reusedProps,
        value: value.map(id => ({id})),
        options,
        getOptionLabel: (option) => option.id,
      }
    });
  });

  test('requires idValues to be set to true for out of the box support for string values, because we need to know the type at runtime', () => {
    () => render(MultiOptionEditor<string, {id: string}>, {
      context,
      // @ts-expect-error missing idValues: true
      props: {
        ...reusedProps,
        value,
        options,
        getOptionLabel: (option) => option.id,
      }
    });
  });

  test('requires getValueId and getValueById for unsupported value types', () => {
    () => render(MultiOptionEditor<{value: string}, {id: string}>, {
      context,
      props: {
        ...reusedProps,
        value: value.map((v) => ({value: v})),
        getValueId: (v) => v.value,
        getValueById: (id) => ({value: id}),
        options,
        getOptionLabel: (option) => option.id,
      }
    });
    () => render(MultiOptionEditor<{value: string}, {id: string}>, {
      context,
      // @ts-expect-error missing getValueId and getValueById
      props: {
        ...reusedProps,
        value: value.map((v) => ({value: v})),
        options,
        getOptionLabel: (option) => option.id,
      }
    });
  });

  test('requires getOptionId for unsupported option types', () => {
    () => render(MultiOptionEditor<string, {code: string}>, {
      context,
      props: {
        ...reusedProps,
        value,
        idValues: true,
        options: options.map((option) => ({code: option.id})),
        getOptionLabel: (option) => option.code,
        getOptionId: (option) => option.code,
      }
    });
    () => render(MultiOptionEditor<string, {code: string}>, {
      context,
      // @ts-expect-error missing getOptionId
      props: {
        ...reusedProps,
        value,
        idValues: true,
        options: options.map((option) => ({code: option.id})),
        getOptionLabel: (option) => option.code,
      }
    });
  });
});
