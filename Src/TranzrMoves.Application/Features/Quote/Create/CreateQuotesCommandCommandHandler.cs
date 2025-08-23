using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.Create;

public class CreateQuotesCommandCommandHandler(
    IUserRepository userRepository,
    IQuoteRepository quoteRepository,
    IUserQuoteRepository userQuoteRepository,
    ILogger<CreateQuotesCommandCommandHandler> logger) 
    : ICommandHandler<CreateQuotesCommand, ErrorOr<(UserDto? User, List<QuoteDto> Quotes, string Etag)>>
{
    public async ValueTask<ErrorOr<(UserDto? User, List<QuoteDto> Quotes, string Etag)>> Handle(CreateQuotesCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.GuestId)) return Error.Unauthorized();

        // if (dto is null) return BadRequest("Request body is required");
        
        var userMapper = new UserMapper();
        logger.LogInformation("Handling CreateJobCommand for User Email: {Email}", command.QuoteContextDto?.Customer?.Email);

        //Get a user by email address
        var userInDb = await userRepository.GetUserByEmailAsync(command.QuoteContextDto?.Customer?.Email, cancellationToken);

        if (userInDb == null)
        {
            logger.LogInformation("User not found in DB. Creating new user for email: {Email}", command.QuoteContextDto?.Customer?.Email);
            var user = userMapper.MapToUser(command.QuoteContextDto?.Customer!);
            var addUserResponse = await userRepository.AddUserAsync(user, cancellationToken);

            if (addUserResponse.IsError)
            {
                logger.LogError("Failed to create user for email: {Email}. Error: {Error}", command.QuoteContextDto?.Customer?.Email, addUserResponse.FirstError.Description);
                return Error.Custom(
                    type: (int)CustomErrorType.BadRequest,
                    code: "UserCannnotBeCreated",
                    description: addUserResponse.FirstError.Description);
            }

            logger.LogInformation("User created successfully for email: {Email}", command.QuoteContextDto?.Customer?.Email);
            userInDb = addUserResponse.Value;
        }
        else
        {
            logger.LogInformation("User found in DB. Updating user with incoming UserDto for email: {Email}", command.QuoteContextDto?.Customer?.Email);
            var updatedUser = userMapper.MapToUser(command.QuoteContextDto?.Customer!);
            updatedUser.Id = userInDb.Id; // preserve existing user ID
            var updateUserResponse = await userRepository.UpdateUserAsync(updatedUser, cancellationToken);
            if (updateUserResponse.IsError)
            {
                logger.LogError("Failed to update user for email: {Email}. Error: {Error}", command.QuoteContextDto?.Customer?.Email, updateUserResponse.FirstError.Description);
                return Error.Custom(
                    type: (int)CustomErrorType.BadRequest,
                    code: "UserCannotBeUpdated",
                    description: updateUserResponse.FirstError.Description);
            }
            logger.LogInformation("User updated successfully for email: {Email}", command.QuoteContextDto?.Customer?.Email);
            userInDb = updateUserResponse.Value;
        }
        
        var quoteMapper = new QuoteMapper();
        
        var quotes = quoteMapper.ToEntityList(command.QuoteContextDto.Quotes.Values.ToList());
        
        logger.LogInformation("Creating quote for session: {SessionId}", command.GuestId);
        
        var quotesDictionary = quotes.GroupBy(p => p.Type).ToDictionary(g => g.Key, g => g.First());
        
        var storedQuotes = await quoteRepository.SaveQuoteContextStateAsync(command.GuestId, quotesDictionary, command.Etag, cancellationToken);

        //Add Job to user
        logger.LogInformation("Associating quotes user id {UserId}", userInDb.Id);
        
        _ = await userQuoteRepository.AddUserQuotesAsync(storedQuotes.Select(x => new CustomerQuote
        {
            UserId = userInDb.Id,
            QuoteId = x.Id
        }).ToList(), cancellationToken);

        var quoteDtos = quoteMapper.ToDtoList(storedQuotes);
        
        // Get updated session to return new ETag
        var updatedSession = await quoteRepository.GetSessionAsync(command.GuestId, cancellationToken);
        
        return (userMapper.MapToUserDto(userInDb), quoteDtos, updatedSession?.ETag);
    }
}