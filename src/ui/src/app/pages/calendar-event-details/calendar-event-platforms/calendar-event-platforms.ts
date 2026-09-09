import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableCell } from 'src/app/shared/components/data-table/data-table-cell';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { type CalendarEventPlatform } from 'src/app/shared/api/calendar-events/calendar-events-service';
import {
  CalendarEventPlatformsState,
  platformStatusText,
  publicationFailureText,
  thumbnailStatusText,
} from './calendar-event-platforms.state';

export function youtubePublicationUrl(platform: CalendarEventPlatform): string | null {
  const externalResourceId = platform.externalResourceId?.trim();
  if (
    platform.platformType !== 'YouTube' ||
    platform.status !== 'Published' ||
    !externalResourceId
  ) {
    return null;
  }

  return `https://www.youtube.com/watch?v=${encodeURIComponent(externalResourceId)}`;
}

const maxPublicationUrlLength = 2048;

export interface PublicationLink {
  href: string;
  ariaLabel: string;
}

export function wordpressPublicationUrl(platform: CalendarEventPlatform): string | null {
  const externalResourceUrl = platform.externalResourceUrl?.trim();
  const externalResourceId = platform.externalResourceId?.trim();
  if (
    platform.platformType !== 'WordPress' ||
    platform.status !== 'Published' ||
    !externalResourceUrl ||
    !externalResourceId ||
    !/^[1-9]\d*$/.test(externalResourceId) ||
    externalResourceUrl.length > maxPublicationUrlLength
  ) {
    return null;
  }

  let url: URL;
  try {
    url = new URL(externalResourceUrl);
  } catch {
    return null;
  }

  if (url.username || url.password || (url.protocol !== 'http:' && url.protocol !== 'https:')) {
    return null;
  }

  const isLocalHost = url.hostname === 'localhost' || url.hostname === '127.0.0.1';
  if (url.protocol === 'http:' && !isLocalHost) {
    return null;
  }

  if (
    !url.pathname.endsWith('/wp-admin/post.php') ||
    url.search !== `?post=${externalResourceId}&action=edit` ||
    url.hash
  ) {
    return null;
  }

  return externalResourceUrl;
}

export function publicationLink(platform: CalendarEventPlatform): PublicationLink | null {
  const youtubeUrl = youtubePublicationUrl(platform);
  if (youtubeUrl) {
    return {
      href: youtubeUrl,
      ariaLabel: `View published stream for ${platform.platformName} on YouTube (opens in a new tab)`,
    };
  }

  const wordpressUrl = wordpressPublicationUrl(platform);
  return wordpressUrl
    ? {
        href: wordpressUrl,
        ariaLabel: `Edit WordPress post for ${platform.platformName} (opens in a new tab)`,
      }
    : null;
}

@Component({
  selector: 'app-calendar-event-platforms',
  imports: [Alert, Button, DataTable, DataTableCell],
  templateUrl: './calendar-event-platforms.html',
  styleUrl: './calendar-event-platforms.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarEventPlatforms {
  readonly state = input.required<CalendarEventPlatformsState>();

  protected readonly platformColumns: readonly DataTableColumn<CalendarEventPlatform>[] = [
    { key: 'type', header: 'Type', value: (platform) => platform.platformType },
    { key: 'name', header: 'Name', value: (platform) => platform.platformName, truncate: true },
    { key: 'status', header: 'Status', value: platformStatusText },
    { key: 'actions', header: 'Actions' },
  ];
  protected readonly thumbnailStatusText = thumbnailStatusText;
  protected readonly publicationFailureText = publicationFailureText;
  protected readonly platformStatusText = platformStatusText;
  protected readonly publicationLink = publicationLink;
}
