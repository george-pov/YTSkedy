import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  CalendarEventDefaultsService,
  type EventTextFieldsResponse,
} from './calendar-event-defaults-service';

export type {
  EventTextField,
  EventTextFieldsResponse,
  EventTextType,
  UpdateEventTextFieldsRequest,
} from './calendar-event-defaults-service';

@Injectable({
  providedIn: 'root',
})
export class EventTextFieldsService {
  private readonly defaults = inject(CalendarEventDefaultsService);

  get(): Observable<EventTextFieldsResponse> {
    return this.defaults.get().pipe(map((response) => response.eventTextFields));
  }
}
