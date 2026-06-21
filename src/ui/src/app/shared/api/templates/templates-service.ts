import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import {
  templateByKeyUrl,
  templatesUrl,
  templateTokensUrl,
} from './templates-endpoint';

/**
 * The platform a template targets. The backend supports exactly these two types
 * and treats the type as immutable after create because it drives storage
 * partitioning. See `docs/api/http/templates.md`.
 */
export type TemplateType = 'YouTube' | 'WordPress';

/** A stored template as returned by the list endpoint. */
export interface Template {
  id: string;
  name: string;
  type: TemplateType;
  content: string;
}

/** Envelope returned by `GET /api/templates`. */
export interface TemplateListResponse {
  templates: Template[];
}

export interface CreateTemplateRequest {
  name: string;
  type: TemplateType;
  content: string;
}

/** Body returned by `POST /api/templates`; carries the server-generated id. */
export interface CreateTemplateResponse {
  id: string;
  name: string;
  type: TemplateType;
}

/** The type is immutable, so only name and content travel in the update body. */
export interface UpdateTemplateRequest {
  name: string;
  content: string;
}

export interface UpdateTemplateResponse {
  id: string;
  name: string;
  type: TemplateType;
}

export interface TemplateToken {
  name: string;
}

/** Envelope returned by `GET /api/template-tokens`. */
export interface TemplateTokenListResponse {
  tokens: TemplateToken[];
}

@Injectable({
  providedIn: 'root',
})
export class TemplatesService {
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(APP_CONFIG);

  list(type?: TemplateType): Observable<TemplateListResponse> {
    const options =
      type === undefined ? {} : { params: new HttpParams().set('type', type) };

    return this.http.get<TemplateListResponse>(templatesUrl(this.appConfig.api), options);
  }

  create(request: CreateTemplateRequest): Observable<CreateTemplateResponse> {
    return this.http.post<CreateTemplateResponse>(templatesUrl(this.appConfig.api), request);
  }

  update(
    type: TemplateType,
    id: string,
    request: UpdateTemplateRequest,
  ): Observable<UpdateTemplateResponse> {
    return this.http.put<UpdateTemplateResponse>(
      templateByKeyUrl(this.appConfig.api, type, id),
      request,
    );
  }

  delete(type: TemplateType, id: string): Observable<void> {
    return this.http.delete<void>(templateByKeyUrl(this.appConfig.api, type, id));
  }

  listTokens(): Observable<TemplateTokenListResponse> {
    return this.http.get<TemplateTokenListResponse>(templateTokensUrl(this.appConfig.api));
  }
}
