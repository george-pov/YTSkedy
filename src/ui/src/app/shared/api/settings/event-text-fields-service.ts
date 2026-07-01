import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { eventTextFieldsUrl } from './event-text-fields-endpoint';

export type EventTextType = 'ShortText' | 'LongText';

export interface EventTextField {
  fieldKey: string;
  label: string;
  type: EventTextType;
  maxLength: number;
}

export interface EventTextFieldsResponse {
  fields: EventTextField[];
}

export interface UpdateEventTextFieldsRequest {
  fields: EventTextField[];
}

@Injectable({
  providedIn: 'root',
})
export class EventTextFieldsService {
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(APP_CONFIG);

  get(): Observable<EventTextFieldsResponse> {
    return this.http.get<EventTextFieldsResponse>(eventTextFieldsUrl(this.appConfig.api));
  }

  update(request: UpdateEventTextFieldsRequest): Observable<EventTextFieldsResponse> {
    return this.http.put<EventTextFieldsResponse>(
      eventTextFieldsUrl(this.appConfig.api),
      request,
    );
  }
}
