import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { CalendarEventDefaultsService } from './calendar-event-defaults-service';
import { EventTextFieldsService } from './event-text-fields-service';

describe('EventTextFieldsService', () => {
  let defaults: { get: Mock };
  let service: EventTextFieldsService;

  beforeEach(() => {
    defaults = { get: vi.fn() };
    TestBed.configureTestingModule({
      providers: [{ provide: CalendarEventDefaultsService, useValue: defaults }],
    });
    service = TestBed.inject(EventTextFieldsService);
  });

  it('reads the event text fields from the combined defaults response', async () => {
    const eventTextFields = {
      fields: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText' as const,
          maxLength: 50,
        },
      ],
    };
    defaults.get.mockReturnValue(
      of({
        eventTextFields,
        startDefaults: { dayOfWeek: null, localTime: null, timeZoneId: null },
      }),
    );

    await expect(firstValueFrom(service.get())).resolves.toEqual(eventTextFields);
  });
});
