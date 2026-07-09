export type PlatformType = 'YouTube' | 'WordPress';

export interface PlatformTypeOption {
  value: PlatformType;
  label: string;
}

export const defaultPlatformType: PlatformType = 'YouTube';

export const platformTypeOptions: readonly PlatformTypeOption[] = [
  { value: 'YouTube', label: 'YouTube' },
  { value: 'WordPress', label: 'WordPress' },
];

export function isPlatformType(value: string): value is PlatformType {
  return value === 'YouTube' || value === 'WordPress';
}
