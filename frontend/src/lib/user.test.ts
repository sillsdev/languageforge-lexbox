import {describe, expect, it} from 'vitest';
import {jwtToUser} from '$lib/user';
import type {AuthUserOrg} from '$lib/gql/generated/graphql';
describe('jwtToUser', () => {
  it('should convert a jwt token to a LexAuthUser', () => {
    const jwtUser = {
      'sub': '6dc9965b-4021-4606-92df-133fcce75fcb',
      'date': 1717123406,
      'email': 'editor@test.com',
      'user': 'editor',
      'name': 'Test Editor',
      'role': 'user' as const,
      'orgs': [
        {
          // eslint-disable-next-line @typescript-eslint/naming-convention
          'Role': 'User',
          // eslint-disable-next-line @typescript-eslint/naming-convention
          'OrgId': '292c80e6-a815-4cd1-9ea2-34bd01274de6'
        } as unknown as AuthUserOrg
      ],
      'proj': 'e:0ebc5976058d4447aaa7297f8569f968|e91ccdb50b46401a97b1ee6fe6179f73,m:7a43d4788e484a64bd9a52f0d7b1d883|58ff3987c3ec40728717574537d61a67',
      'mkproj': true,
      'loc': 'en',
      'jti': '391c2d93',
      'props.persistent': '',
      'nbf': 1721377515,
      'exp': 1722673515,
      'iat': 1721377515,
      'iss': 'LexboxApi',
      'aud': 'LexboxApi'
    };
    const user = jwtToUser(jwtUser);
    expect(user).toEqual({
      'id': '6dc9965b-4021-4606-92df-133fcce75fcb',
      'name': 'Test Editor',
      'email': 'editor@test.com',
      'username': 'editor',
      'role': 'USER',
      'isAdmin': false,
      'projects': [
        {
          'projectId': '0ebc5976-058d-4447-aaa7-297f8569f968',
          'role': 'EDITOR'
        },
        {
          'projectId': 'e91ccdb5-0b46-401a-97b1-ee6fe6179f73',
          'role': 'EDITOR'
        },
        {
          'projectId': '7a43d478-8e48-4a64-bd9a-52f0d7b1d883',
          'role': 'MANAGER'
        },
        {
          'projectId': '58ff3987-c3ec-4072-8717-574537d61a67',
          'role': 'MANAGER'
        }
      ],
      'orgs': [
        {
          'orgId': '292c80e6-a815-4cd1-9ea2-34bd01274de6',
          'role': 'USER'
        }
      ],
      'locked': false,
      'emailVerified': true,
      'canCreateProjects': true,
      'createdByAdmin': false,
      'locale': 'en',
      'emailOrUsername': 'editor@test.com'
    });
  });
});
