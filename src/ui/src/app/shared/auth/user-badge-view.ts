import { trimToUndefined } from './trim-to-undefined';
import { UserIdentity } from './user-identity';

/**
 * View model the presentational {@link UserBadge} consumes: a full name and a
 * pre-computed monogram. Derived purely from {@link UserIdentity}; holds no MSAL
 * or DOM dependency.
 */
export interface UserBadgeView {
  fullName: string;
  initials: string;
}

const UNKNOWN: UserBadgeView = { fullName: 'Unknown User', initials: 'NA' };

/**
 * Maps the signed-in user's claims to a badge view with a defined fallback
 * chain. Full name prefers `givenName` + `familyName`, then `name`, then the
 * local-part of `email`, then a neutral placeholder. Initials follow the same
 * source priority.
 */
export function toUserBadgeView(identity: UserIdentity | null): UserBadgeView {
  if (identity === null) {
    return UNKNOWN;
  }

  return {
    fullName: resolveFullName(identity),
    initials: resolveInitials(identity),
  };
}

function resolveFullName(identity: UserIdentity): string {
  const given = trimToUndefined(identity.givenName);
  const family = trimToUndefined(identity.familyName);
  if (given !== undefined && family !== undefined) {
    return `${given} ${family}`;
  }

  const name = trimToUndefined(identity.name);
  if (name !== undefined) {
    return name;
  }

  return emailLocalPart(identity.email) ?? UNKNOWN.fullName;
}

function resolveInitials(identity: UserIdentity): string {
  const given = trimToUndefined(identity.givenName);
  const family = trimToUndefined(identity.familyName);
  if (given !== undefined && family !== undefined) {
    return (given[0] + family[0]).toUpperCase();
  }

  const name = trimToUndefined(identity.name);
  if (name !== undefined) {
    return initialsFromText(name);
  }

  const local = emailLocalPart(identity.email);
  if (local !== undefined) {
    return initialsFromText(local);
  }

  return UNKNOWN.initials;
}

// Take the first letter of up to two segments, splitting on whitespace and the
// common name separators found in email local-parts (".", "_", "-").
function initialsFromText(text: string): string {
  const segments = text.split(/[\s._-]+/).filter((segment) => segment.length > 0);
  if (segments.length === 0) {
    return UNKNOWN.initials;
  }

  return segments
    .slice(0, 2)
    .map((segment) => segment[0])
    .join('')
    .toUpperCase();
}

function emailLocalPart(email: string | undefined): string | undefined {
  const normalized = trimToUndefined(email);
  if (normalized === undefined) {
    return undefined;
  }

  const at = normalized.indexOf('@');
  const local = at === -1 ? normalized : normalized.slice(0, at);
  return local.length === 0 ? undefined : local;
}
