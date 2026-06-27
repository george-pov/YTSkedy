namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Outcome of a create platform attempt as seen by the HTTP host.
/// <see cref="PlatformId"/> is set only when <see cref="Status"/> is
/// <see cref="CreatePlatformStatus.Created"/>, which maps to 200.
/// <see cref="CreatePlatformStatus.NameAlreadyExists"/> maps to 409 because the
/// name is already used by another platform.
/// <see cref="CreatePlatformStatus.ReferenceKeyAlreadyExists"/> maps to 409
/// because another platform already uses the same non-empty reference key,
/// compared case-insensitively.
/// </summary>
public sealed record CreatePlatformResult(
    CreatePlatformStatus Status,
    string? PlatformId)
{
    public static CreatePlatformResult Created(string platformId) =>
        new(CreatePlatformStatus.Created, platformId);

    public static CreatePlatformResult NameAlreadyExists() =>
        new(CreatePlatformStatus.NameAlreadyExists, null);

    public static CreatePlatformResult ReferenceKeyAlreadyExists() =>
        new(CreatePlatformStatus.ReferenceKeyAlreadyExists, null);
}
