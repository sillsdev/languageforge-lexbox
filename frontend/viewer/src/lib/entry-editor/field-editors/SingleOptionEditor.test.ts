/* eslint-disable @typescript-eslint/no-unsafe-return, @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-assignment */
import { readable } from 'svelte/store'
import { beforeEach, describe, expect, test } from 'vitest'

import { render, screen } from '@testing-library/svelte'
import userEvent, { type UserEvent } from '@testing-library/user-event'
import { getState } from '../../utils/test-utils'
import SingleOptionEditor from './SingleOptionEditor.svelte'

const value = '2';
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

describe('SingleOptionEditor', () => {

  let user: UserEvent;
  let component: SingleOptionEditor<string, {id: string}>;

  beforeEach(() => {
    user = userEvent.setup();
    ({component} = render(SingleOptionEditor<string, {id: string}>, {
      context,
      props: {
        ...reusedProps,
        idValue: true,
        value,
        options,
        getOptionLabel: (option) => option.id,
      }
    }));
  });

  test('can change selection', async () => {
    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByRole('option', {name: '5'}));
    expect(getState(component).value).toBe('5');

    await user.click(screen.getByRole('textbox'));
    await user.click(screen.getByRole('option', {name: '3'}));
    expect(getState(component).value).toBe('3');
  });
});

describe('SingleOptionEditor configurations', () => {

  test('supports string or { id: string} values and { id: string } options out of the box', () => {
    () => render(SingleOptionEditor<string, {id: string}>, {
      context,
      props: {
        ...reusedProps,
        idValue: true,
        value,
        options: [],
        getOptionLabel: (option) => option.id,
      }
    });
    () => render(SingleOptionEditor<{id: string}, {id: string}>, {
      context,
      props: {
        ...reusedProps,
        value: {id: value},
        options,
        getOptionLabel: (option) => option.id,
      }
    });
  });

  test('requires idValues to be set to true for out of the box support for string values, because we need to know the type at runtime', () => {
    () => render(SingleOptionEditor<string, {id: string}>, {
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
    () => render(SingleOptionEditor<{value: string} | undefined, {id: string}>, {
      context,
      props: {
        ...reusedProps,
        value: {value},
        getValueId: (v) => v?.value,
        getValueById: (id) => id ? {value: id} : undefined,
        options,
        getOptionLabel: (option) => option.id,
      }
    });
    () => render(SingleOptionEditor<{value: string}, {id: string}>, {
      context,
      // @ts-expect-error missing getValueId and getValueById
      props: {
        ...reusedProps,
        value: {value},
        options,
        getOptionLabel: (option) => option.id,
      }
    });
  });

  test('requires getOptionId for unsupported option types', () => {
    () => render(SingleOptionEditor<string, {code: string}>, {
      context,
      props: {
        ...reusedProps,
        value,
        idValue: true,
        options: options.map((option) => ({code: option.id})),
        getOptionLabel: (option) => option.code,
        getOptionId: (option) => option.code,
      }
    });
    () => render(SingleOptionEditor<string, {code: string}>, {
      context,
      // @ts-expect-error missing getOptionId
      props: {
        ...reusedProps,
        value,
        idValue: true,
        options: options.map((option) => ({code: option.id})),
        getOptionLabel: (option) => option.code,
      }
    });
  });
});
