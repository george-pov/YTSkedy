import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';

import {
  PlatformNameConflictError,
  PlatformsService,
} from './platforms-service';

describe('PlatformsService (in-memory)', () => {
  let service: PlatformsService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [PlatformsService] });
    service = TestBed.inject(PlatformsService);
  });

  it('lists the seeded YouTube and WordPress platforms', async () => {
    const response = await firstValueFrom(service.list());

    expect(response.platforms.map((platform) => platform.type).sort()).toEqual([
      'WordPress',
      'YouTube',
    ]);
  });

  it('returns cloned platforms so callers cannot mutate the store', async () => {
    const first = await firstValueFrom(service.list());
    const youTube = first.platforms.find((p) => p.type === 'YouTube');
    youTube!.name = 'Mutated';

    const second = await firstValueFrom(service.list());
    expect(second.platforms.some((p) => p.name === 'Mutated')).toBe(false);
  });

  it('creates a platform with a generated id and includes it in the list', async () => {
    const created = await firstValueFrom(
      service.create({
        name: 'Second channel',
        type: 'YouTube',
        publishSettings: {
          credentials: 'second-channel',
          privacyStatus: 'public',
          selfDeclaredMadeForKids: true,
        },
      }),
    );

    expect(created.id).toBeTruthy();

    const response = await firstValueFrom(service.list());
    expect(response.platforms.some((p) => p.id === created.id)).toBe(true);
  });

  it('rejects a create whose name duplicates an existing platform', async () => {
    await expect(
      firstValueFrom(
        service.create({
          name: 'main youtube channel',
          type: 'YouTube',
          publishSettings: {
            credentials: 'dupe',
            privacyStatus: 'private',
            selfDeclaredMadeForKids: false,
          },
        }),
      ),
    ).rejects.toBeInstanceOf(PlatformNameConflictError);
  });

  it('updates the name of an existing platform', async () => {
    const { platforms } = await firstValueFrom(service.list());
    const target = platforms.find((p) => p.type === 'YouTube')!;

    await firstValueFrom(
      service.update('YouTube', target.id, {
        name: 'Renamed channel',
        publishSettings: target.publishSettings,
      }),
    );

    const after = await firstValueFrom(service.list());
    expect(after.platforms.find((p) => p.id === target.id)?.name).toBe(
      'Renamed channel',
    );
  });

  it('deletes a platform and is idempotent for an unknown id', async () => {
    const { platforms } = await firstValueFrom(service.list());
    const target = platforms[0];

    await firstValueFrom(service.delete(target.type, target.id));
    await firstValueFrom(service.delete(target.type, target.id));

    const after = await firstValueFrom(service.list());
    expect(after.platforms.some((p) => p.id === target.id)).toBe(false);
  });
});
