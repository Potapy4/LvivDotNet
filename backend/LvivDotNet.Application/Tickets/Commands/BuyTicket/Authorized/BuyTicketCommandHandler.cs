﻿using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using LvivDotNet.Application.Exceptions;
using LvivDotNet.Application.Interfaces;
using MediatR;

namespace LvivDotNet.Application.Tickets.Commands.BuyTicket.Authorized
{
    /// <summary>
    /// Buy ticket command handler for authorized user.
    /// </summary>
    public class BuyTicketCommandHandler : BaseHandler<BuyTicketCommand>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuyTicketCommandHandler"/> class.
        /// </summary>
        /// <param name="dbConnectionFactory"> Database connection factory. </param>
        public BuyTicketCommandHandler(IDbConnectionFactory dbConnectionFactory)
            : base(dbConnectionFactory)
        {
        }

        /// <inheritdoc />
        protected override async Task<Unit> Handle(BuyTicketCommand request, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var ticketTemplateId = await connection.QuerySingleAsync<int?>(
                "select top 1 Id from dbo.[ticket_template] where EventId = @EventId and [From] <= @Now and [To] >= @Now",
                new { request.EventId, Now = DateTime.UtcNow },
                transaction);

            if (!ticketTemplateId.HasValue)
            {
                throw new TicketsNotAvailable();
            }

            var maxAttendees = await connection.QuerySingleAsync<int>(
                "select MaxAttendees from dbo.[event] where Id = @EventId",
                new { request.EventId },
                transaction);

            var ticketsCount = await connection.QuerySingleAsync<int>(
                "select count(*) from dbo.[ticket] as ticket " +
                "join dbo.[ticket_template] as ticket_template on ticket.TicketTemplateId = ticket_template.Id " +
                "where ticket_template.EventId = @EventId",
                new { request.EventId, Now = DateTime.UtcNow },
                transaction);

            if (ticketsCount >= maxAttendees)
            {
                var eventName = await connection.QuerySingleAsync<string>(
                    "select event.Name from dbo.[event] as event " +
                    "where event.Id = @EventId",
                    new { request.EventId },
                    transaction);

                throw new SouldOutException(eventName);
            }

            await connection.ExecuteAsync(
                "insert into dbo.[ticket] (TicketTemplateId, AttendeeId, UserId, CreatedDate) " +
                "select @TicketTemplateId, NULL, Id, @CreatedDate from dbo.[user] " +
                "where Email = @Email",
                new { TicketTemplateId = ticketTemplateId, Email = request.UserEmail, CreatedDate = DateTime.UtcNow },
                transaction);

            return Unit.Value;
        }
    }
}
