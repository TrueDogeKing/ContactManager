using System.Security.Claims;
using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Api.Controllers;

/// Contact endpoints. Reads are public; create, update and delete require authentication.
[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly IValidator<CreateContactRequestDto> _createValidator;
    private readonly IValidator<UpdateContactRequestDto> _updateValidator;
    private readonly IValidator<ChangeContactPasswordRequestDto> _changePasswordValidator;

    /// Creates controller with dependencies.
    public ContactsController(
        IContactService contactService,
        IValidator<CreateContactRequestDto> createValidator,
        IValidator<UpdateContactRequestDto> updateValidator,
        IValidator<ChangeContactPasswordRequestDto> changePasswordValidator
    )
    {
        _contactService = contactService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    /// Returns all contacts. Public endpoint.
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ContactResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var contacts = await _contactService.GetAllAsync(cancellationToken);
        return Ok(contacts);
    }

    /// Returns a single contact by id. Public endpoint.
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ContactResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var contact = await _contactService.GetByIdAsync(id, cancellationToken);
        if (contact is null)
        {
            return NotFound();
        }

        return Ok(contact);
    }

    /// Creates a new contact. Requires authentication.
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ContactResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateContactRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var created = await _contactService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// Updates an existing contact with optimistic concurrency control. Requires authentication.
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateContactRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var updated = await _contactService.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// Changes a contact's password with optimistic concurrency control. Requires authentication;
    /// only the signed-in owner (matching email) may change a contact's password.
    [HttpPut("{id:guid}/password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePassword(
        Guid id,
        [FromBody] ChangeContactPasswordRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        // Email from the JWT (mapped to ClaimTypes.Email, or the raw "email" claim when mapping is off).
        var callerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (string.IsNullOrEmpty(callerEmail))
        {
            return Unauthorized();
        }

        var changed = await _contactService.ChangePasswordAsync(
            id,
            request,
            callerEmail,
            cancellationToken
        );
        if (!changed)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// Deletes a contact. Requires authentication.
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _contactService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
