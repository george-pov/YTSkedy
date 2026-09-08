# WordPress Publish Troubleshooting

Use the publish error shown by the Calendar Event page as the starting point.
It includes a publish attempt reference when the backend recorded a started
failure. Search that reference in Application Insights before retrying.

## Find One Attempt

```kusto
traces
| where timestamp > ago(24h)
| where tostring(customDimensions.PublishAttemptId) == "<attempt-id>"
| project
    timestamp,
    severityLevel,
    message,
    FailureCode = tostring(customDimensions.FailureCode),
    RequestStage = tostring(customDimensions.RequestStage),
    StatusCode = toint(customDimensions.StatusCode),
    ProviderErrorCode = tostring(customDimensions.ProviderErrorCode),
    RetryAfterUtc = todatetime(customDimensions.RetryAfterUtc),
    DurationMs = todouble(customDimensions.DurationMs),
    DiscoveryCacheHit = tobool(customDimensions.DiscoveryCacheHit),
    ProviderRequestCount = toint(customDimensions.ProviderRequestCount),
    EndpointStyle = tostring(customDimensions.EndpointStyle)
| order by timestamp asc
```

The WordPress request also carries the reference as
`X-YTSkedy-Request-Id`. A WordPress administrator can use it to correlate a
request when server or security-plugin logs expose request headers.

## Review Recent WordPress Failures

```kusto
traces
| where timestamp > ago(7d)
| where message startswith "WordPress publish failed"
| summarize
    Count = count(),
    LastSeen = max(timestamp)
    by
    FailureCode = tostring(customDimensions.FailureCode),
    StatusCode = toint(customDimensions.StatusCode),
    RequestStage = tostring(customDimensions.RequestStage)
| order by Count desc
```

## Interpret Common Results

| Code | Meaning | Operator action |
| --- | --- | --- |
| `wordpress_rate_limited` | WordPress or its security layer returned HTTP 429. | Check `RetryAfterUtc`, verify WordPress, and wait before an explicit retry. |
| `wordpress_authentication_failed` | WordPress returned HTTP 401. | Check the saved username and Application Password. |
| `wordpress_permission_denied` | WordPress returned HTTP 403. | Check the WordPress role and security-plugin allow rules. |
| `wordpress_request_rejected` | WordPress rejected the create-post request. | Use `ProviderErrorCode` and the platform settings to identify the invalid request. |
| `wordpress_discovery_failed` | YTSkedy could not locate a supported REST API root. | Check the site URL, REST API availability, redirects, and security rules. |
| `wordpress_invalid_response` | WordPress returned malformed JSON or no valid post id. | Inspect WordPress and proxy/plugin logs for the referenced request. |

`ProviderRequestCount` includes discovery requests and the create-post request.
A cache hit normally makes a publish use one provider request. A discovery cache
miss normally uses three requests when WordPress exposes its REST root through
the standard site link header.

Do not copy authorization headers, Application Passwords, raw request bodies,
or full provider responses into tickets or logs. Provider writes are never
automatically retried. When `verificationRequired` is true, check WordPress for
an existing post before using the explicit retry action.
