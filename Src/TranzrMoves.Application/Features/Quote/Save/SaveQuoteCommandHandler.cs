using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.Save;

public class SaveQuoteCommandHandler(
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    IUserQuoteRepository userQuoteRepository,
    ILogger<SaveQuoteCommandHandler> logger) 
    : ICommandHandler<SaveQuoteCommand, ErrorOr<SaveQuoteResponse>>
{
    public async ValueTask<ErrorOr<SaveQuoteResponse>> Handle(
        SaveQuoteCommand command, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Quote.SessionId))
        {
            return Error.Custom((int)CustomErrorType.BadRequest, "SessionId.Required", "Session Id is required");
        }

        try
        {
            // Get existing quote
            var existingQuote = await quoteRepository.GetQuoteAsync(command.Quote.Id, cancellationToken);
            
            if (existingQuote is null)
            {
                logger.LogWarning("No existing quote found for guest {SessionId} and type {QuoteType}", 
                    command.Quote.SessionId, command.Quote.Type!.Value);
                return Error.Custom((int)CustomErrorType.NotFound, "Quote.NotFound", "Quote not found");
            }

            // Update quote entity
            var mapper = new QuoteMapper();
            mapper.UpdateEntity(command.Quote, existingQuote);
            
            var result = await quoteRepository.UpdateQuoteAsync(existingQuote, cancellationToken);
            
            if (result.IsError)
            {
                if (result.FirstError.Type == ErrorType.Conflict)
                {
                    logger.LogInformation("ETag mismatch for guest {SessionId}", command.Quote.SessionId);
                    return Error.Custom((int)ErrorType.Conflict, "Quote.ConcurrencyConflict", "Quote was modified by another request");
                }
                
                logger.LogWarning("Failed to update quote for guest {SessionId}: {Error}", 
                    command.Quote.SessionId, result.FirstError.Description);
                return result.Errors;
            }
            
            var updatedQuote = result.Value;
            
            User? userToSave = null;
            
            // Handle customer data if provided
            if (command.Customer != null)
            {
                try
                {
                    // Check if user already exists by email
                    if (!string.IsNullOrEmpty(command.Customer.Email))
                    {
                        userToSave = await userRepository.GetUserByEmailAsync(command.Customer.Email, cancellationToken);
                    }

                    if (userToSave != null)
                    {
                        // Update existing user
                        userToSave.FullName = command.Customer.FullName ?? userToSave.FullName;
                        userToSave.PhoneNumber = command.Customer.PhoneNumber ?? userToSave.PhoneNumber;
                            
                        // Update billing address if provided
                        if (command.Customer.BillingAddress != null)
                        {
                            if (userToSave.BillingAddress == null)
                            {
                                userToSave.BillingAddress = new Address
                                {
                                    Line1 = command.Customer.BillingAddress.Line1,
                                    PostCode = command.Customer.BillingAddress.PostCode
                                };
                            }
                                
                            userToSave.BillingAddress.Line1 = command.Customer.BillingAddress.Line1;
                            userToSave.BillingAddress.Line2 = command.Customer.BillingAddress.Line2;
                            userToSave.BillingAddress.City = command.Customer.BillingAddress.City;
                            userToSave.BillingAddress.County = command.Customer.BillingAddress.County;
                            userToSave.BillingAddress.PostCode = command.Customer.BillingAddress.PostCode;
                            userToSave.BillingAddress.Country = command.Customer.BillingAddress.Country;
                            userToSave.BillingAddress.HasElevator = command.Customer.BillingAddress.HasElevator;
                            userToSave.BillingAddress.Floor = command.Customer.BillingAddress.Floor;
                        }
                            
                        var updateResult = await userRepository.UpdateUserAsync(userToSave, cancellationToken);
                        if (updateResult.IsError)
                        {
                            logger.LogWarning("Failed to update existing user {UserId}: {Error}", 
                                userToSave.Id, updateResult.FirstError.Description);
                        }
                        else
                        {
                            userToSave = updateResult.Value;
                        }
                    }
                    else
                    {
                        // Create new user
                        var newUserCreation = new User
                        {
                            FullName = command.Customer.FullName,
                            Email = command.Customer.Email,
                            PhoneNumber = command.Customer.PhoneNumber,
                            BillingAddress = command.Customer.BillingAddress != null ? new Address
                            {
                                Line1 = command.Customer.BillingAddress.Line1,
                                Line2 = command.Customer.BillingAddress.Line2,
                                City = command.Customer.BillingAddress.City,
                                County = command.Customer.BillingAddress.County,
                                PostCode = command.Customer.BillingAddress.PostCode,
                                Country = command.Customer.BillingAddress.Country,
                                HasElevator = command.Customer.BillingAddress.HasElevator,
                                Floor = command.Customer.BillingAddress.Floor
                            } : null
                        };
                            
                        var createResult = await userRepository.AddUserAsync(newUserCreation, cancellationToken);
                        if (createResult.IsError)
                        {
                            logger.LogWarning("Failed to create new user: {Error}", createResult.FirstError.Description);
                        }
                        else
                        {
                            userToSave = createResult.Value;
                        }
                    }

                    // Create CustomerQuote relationship if user was successfully saved
                    if (userToSave != null)
                    {
                        var customerQuote = new CustomerQuote
                        {
                            UserId = userToSave.Id,
                            QuoteId = updatedQuote.Id
                        };
                            
                        var relationshipResult = await userQuoteRepository.AddUserQuoteAsync(customerQuote, cancellationToken);
                        if (relationshipResult.IsError && relationshipResult.FirstError.Type == ErrorType.Conflict)
                        {
                            logger.LogWarning("User quote relationship already exists: {Error}", 
                                relationshipResult.FirstError.Description);
                        }
                        else
                        {
                            logger.LogInformation("Successfully created CustomerQuote relationship for user {UserId} and quote {QuoteId}", 
                                userToSave.Id, updatedQuote.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error handling customer data for quote {QuoteId}, continuing without customer data", updatedQuote.Id);
                }
            }

            // Return the updated quote's Version as the new ETag
            var newEtag = updatedQuote.Version.ToString();
            
            logger.LogInformation("Successfully saved quote for guest {SessionId} with new ETag {ETag}", 
                command.Quote.SessionId, newEtag);
            
            var userMapper = new UserMapper();

            return new SaveQuoteResponse(mapper.ToDto(updatedQuote), userMapper.ToDto(userToSave!), newEtag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error mapping QuoteDto to Quote entity for guest {SessionId}", command.Quote.SessionId);
            return Error.Custom((int)CustomErrorType.BadRequest, "Quote.InvalidFormat", "Invalid quote data format");
        }
    }
}
