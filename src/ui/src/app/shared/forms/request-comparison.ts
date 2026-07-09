// Compare normalized API request objects, not raw form models. The form mappers
// own trimming, secret omission, string-to-number conversion, and UI-only field
// removal before values reach this helper.
export function sameRequest<T>(left: T, right: T): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}
