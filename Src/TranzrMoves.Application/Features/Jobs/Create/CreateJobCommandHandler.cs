using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Jobs.Create;

public class CreateJobCommandHandler(
    IJobRepository jobRepository, 
    IUserRepository userRepository,
    IUserJobRepository userJobRepository,
    ILogger<CreateJobCommandHandler> logger) 
    : ICommandHandler<CreateJobCommand, ErrorOr<JobDto>>
{
    public async ValueTask<ErrorOr<JobDto>> Handle(CreateJobCommand command, CancellationToken cancellationToken)
    {
        // Store user
        
        // var job = new Job
        // {
        //     QuoteId = BookingNumberGenerator.Generate(),
        //     VanType = command.JobDto.VanType,
        //     Destination = new Address
        //     {
        //         AddressLine1 = command.JobDto.Destination.AddressLine1,
        //         AddressLine2 = command.JobDto.Destination.AddressLine2,
        //         City = command.JobDto.Destination.City,
        //         Country = command.JobDto.Destination.Country,
        //         PostCode = command.JobDto.Destination.PostCode
        //     },
        //     Origin = new Address
        //     {
        //         AddressLine1 = command.JobDto.Origin.AddressLine1,
        //         AddressLine2 = command.JobDto.Origin.AddressLine2,
        //         City = command.JobDto.Origin.City,
        //         Country = command.JobDto.Origin.Country,
        //         PostCode = command.JobDto.Origin.PostCode
        //     },
        //     PaymentStatus = command.JobDto.PaymentStatus,
        //     PricingTier = command.JobDto.PricingTier,
        //     InventoryItems = command.JobDto.InventoryItems.Select(x => new InventoryItem
        //     {
        //         Name = x.Name,
        //         Quantity = x.Quantity,
        //         Depth = x.Depth,
        //         Height = x.Height,
        //         Width = x.Width,
        //         Description = x.Description,
        //     }).ToList(),
        //     CollectionDate = command.JobDto.CollectionDate
        // };
        
        var userMapper = new UserMapper();
        logger.LogInformation("Handling CreateJobCommand for QuoteId: {QuoteId}, User Email: {Email}", command.JobDto?.QuoteId, command.JobDto?.User?.Email);

        //Get a user by email address
        var userInDb = await userRepository.GetUserByEmailAsync(command.JobDto?.User?.Email, cancellationToken);

        if (userInDb == null)
        {
            logger.LogInformation("User not found in DB. Creating new user for email: {Email}", command.JobDto?.User?.Email);
            var user = userMapper.MapToUser(command.JobDto?.User!);
            var addUserResponse = await userRepository.AddUserAsync(user, cancellationToken);

            if (addUserResponse.IsError)
            {
                logger.LogError("Failed to create user for email: {Email}. Error: {Error}", command.JobDto?.User?.Email, addUserResponse.FirstError.Description);
                return Error.Custom(
                    type: (int)CustomErrorType.BadRequest,
                    code: "UserCannnotBeCreated",
                    description: addUserResponse.FirstError.Description);
            }

            logger.LogInformation("User created successfully for email: {Email}", command.JobDto?.User?.Email);
            userInDb = addUserResponse.Value;
        }
        else
        {
            logger.LogInformation("User found in DB. Updating user with incoming UserDto for email: {Email}", command.JobDto?.User?.Email);
            var updatedUser = userMapper.MapToUser(command.JobDto?.User!);
            updatedUser.Id = userInDb.Id; // preserve existing user ID
            var updateUserResponse = await userRepository.UpdateUserAsync(updatedUser, cancellationToken);
            if (updateUserResponse.IsError)
            {
                logger.LogError("Failed to update user for email: {Email}. Error: {Error}", command.JobDto?.User?.Email, updateUserResponse.FirstError.Description);
                return Error.Custom(
                    type: (int)CustomErrorType.BadRequest,
                    code: "UserCannotBeUpdated",
                    description: updateUserResponse.FirstError.Description);
            }
            logger.LogInformation("User updated successfully for email: {Email}", command.JobDto?.User?.Email);
            userInDb = updateUserResponse.Value;
        }

        var jobMapper = new JobMapper();
        var job = jobMapper.MapToJob(command.JobDto);
        logger.LogInformation("Creating job for QuoteId: {QuoteId}", command.JobDto?.QuoteId);
        var addJobResponse = await jobRepository.AddJobAsync(job, cancellationToken);

        if (addJobResponse.IsError && addJobResponse.FirstError.Type == CustomErrorType.UnprocessableEntity)
        {
            logger.LogError("Failed to create job for QuoteId: {QuoteId}. Error: {Error}", command.JobDto?.QuoteId, addJobResponse.FirstError.Description);
            return Error.Custom(
                type: (int)CustomErrorType.UnprocessableEntity,
                code: "Null.Value",
                description: addJobResponse.FirstError.Description);
        }

        if (addJobResponse.IsError && addJobResponse.FirstError.Type == ErrorType.Conflict)
        {
            logger.LogWarning("Job creation conflict for QuoteId: {QuoteId}", command.JobDto?.QuoteId);
            return Error.Conflict();
        }

        //Add Job to user
        logger.LogInformation("Associating job (Id: {JobId}) with user (Id: {UserId})", addJobResponse.Value.Id, userInDb.Id);
        await userJobRepository.AddUserJobAsync(new CustomerJob
        {
            JobId = addJobResponse.Value.Id,
            UserId = userInDb.Id
        }, cancellationToken);

        var jobDto = jobMapper.MapJobToDto(job);
        logger.LogInformation("Job created and associated successfully. Returning JobDto for QuoteId: {QuoteId}", command.JobDto?.QuoteId);
        return jobDto;
    }
}