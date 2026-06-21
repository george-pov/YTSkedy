import { describe, expect, it } from 'vitest';

import { toUserBadgeView } from './user-badge-view';

describe('toUserBadgeView', () => {
  it('prefers given and family name', () => {
    expect(toUserBadgeView({ givenName: 'Jane', familyName: 'Doe' })).toEqual({
      fullName: 'Jane Doe',
      initials: 'JD',
    });
  });

  it('falls back to the display name with multi-word initials', () => {
    expect(toUserBadgeView({ name: 'Jane Q Public' })).toEqual({
      fullName: 'Jane Q Public',
      initials: 'JQ',
    });
  });

  it('uses a single initial for a single-word display name', () => {
    expect(toUserBadgeView({ name: 'Cher' })).toEqual({
      fullName: 'Cher',
      initials: 'C',
    });
  });

  it('falls back to the email local-part when no name claim is present', () => {
    expect(toUserBadgeView({ email: 'jane.doe@example.com' })).toEqual({
      fullName: 'jane.doe',
      initials: 'JD',
    });
  });

  it('ignores blank claims and falls through to the next source', () => {
    expect(
      toUserBadgeView({ givenName: '  ', name: '   ', email: 'ada@example.com' }),
    ).toEqual({
      fullName: 'ada',
      initials: 'A',
    });
  });

  it('requires both given and family before using them', () => {
    expect(
      toUserBadgeView({ givenName: 'Jane', name: 'Jane Q Public' }),
    ).toEqual({
      fullName: 'Jane Q Public',
      initials: 'JQ',
    });
  });

  it('returns the unknown placeholder for a null identity', () => {
    expect(toUserBadgeView(null)).toEqual({
      fullName: 'Unknown User',
      initials: 'NA',
    });
  });

  it('returns the unknown placeholder when every claim is empty', () => {
    expect(toUserBadgeView({})).toEqual({
      fullName: 'Unknown User',
      initials: 'NA',
    });
  });
});
