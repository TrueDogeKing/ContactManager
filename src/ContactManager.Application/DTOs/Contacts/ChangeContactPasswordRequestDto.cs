namespace ContactManager.Application.DTOs.Contacts;

/// Input data for changing a contact's password. RowVersion is the concurrency token
/// fetched during read (optimistic concurrency, conflict returns 409).
public record ChangeContactPasswordRequestDto(
    string NewPassword,
    uint RowVersion);
