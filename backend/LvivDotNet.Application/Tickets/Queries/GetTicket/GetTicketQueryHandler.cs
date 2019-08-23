﻿using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using LvivDotNet.Application.Exceptions;
using LvivDotNet.Application.Interfaces;
using LvivDotNet.Application.Tickets.Models;
using LvivDotNet.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace LvivDotNet.Application.Tickets.Queries.GetTicket
{
    /// <summary>
    /// Get ticket by id query handler.
    /// </summary>
    public class GetTicketQueryHandler : BaseHandler<GetTicketQuery, TicketModel>
    {
        /// <summary>
        /// Get ticket sql query.
        /// </summary>
        private const string GetTicketSqlQuery =
                "select " +
                @"""event"".""Name"" as EventName," +
                @"""event"".""StartDate"" as Start," +
                @"""event"".""EndDate"" as End," +
                @"""ticket"".""CreatedDate"" as Bought," +
                @"""ticket_template"".""Price"" as Price " +
                "from public.ticket as ticket " +
                @"join public.ticket_template as ticket_template on ""ticket"".""TicketTemplateId"" = ""ticket_template"".""Id"" " +
                @"join public.event as event on ""event"".""Id"" = ""ticket_template"".""EventId"" " +
                @"where ""ticket"".""Id"" = cast(@TicketId as integer) and (""ticket"".""UserId"" is NULL or ""ticket"".""UserId"" = cast(@UserId as integer))";

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTicketQueryHandler"/> class.
        /// </summary>
        /// <param name="dbConnectionFactory"> Database connection factory. </param>
        /// <param name="httpContextAccessor"> See <see cref="IHttpContextAccessor"/>. </param>
        public GetTicketQueryHandler(IDbConnectionFactory dbConnectionFactory, IHttpContextAccessor httpContextAccessor)
            : base(dbConnectionFactory, httpContextAccessor)
        {
        }

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "We already have a not-null check for request in MediatR")]
        protected override async Task<TicketModel> Handle(GetTicketQuery request, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            var tickets = await connection.QueryAsync<TicketModel>(GetTicketSqlQuery, new { request.TicketId, UserId = this.User.GetId() }, transaction)
                .ConfigureAwait(true);

            if (tickets.Count() == 1)
            {
                return tickets.Single();
            }

            throw new NotFoundException("Ticket", request.TicketId);
        }
    }
}
