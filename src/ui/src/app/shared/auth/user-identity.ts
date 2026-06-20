/**
 * Display identity for the signed-in user, sourced from the Microsoft Entra
 * External ID ID token claims on the MSAL active account. Every field is
 * optional: the user flow currently returns Display Name (`name`) and email,
 * while `givenName` / `familyName` are only present if the flow is later
 * configured to return them.
 */
export interface UserIdentity {
  name?: string;
  givenName?: string;
  familyName?: string;
  email?: string;
}
