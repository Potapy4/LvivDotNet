﻿using Dapper;
using LvivDotNet.Application.Exceptions;
using LvivDotNet.Application.Interfaces;
using LvivDotNet.Application.Users.Models;
using LvivDotNet.Common;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace LvivDotNet.Application.Users.Commands.Refresh
{
    public class RefreshTokenCommandHandler : BaseHandler<RefreshTokenCommand, AuthTokensModel>
    {
        private readonly IConfiguration configuration;

        public RefreshTokenCommandHandler(IDbConnectionFactory dbConnectionFactory, IConfiguration configuration)
            : base(dbConnectionFactory)
        {
            this.configuration = configuration;
        }

        protected override async Task<AuthTokensModel> Handle(RefreshTokenCommand request, CancellationToken cancellationToken, IDbConnection connection, IDbTransaction transaction)
        {
            var token = SecurityHelpers.DecodeJwtToken(request.JwnToken);

            var userId = int.Parse(token.Claims.First(claim => claim.Type == "id").Value);

            var refreshTokenExpires = await connection.QueryAsync<DateTime>(
                    "select Expires from dbo.[refresh_token] " +
                    "where UserId = @UserId and RefreshToken = @RefreshToken",
                    new { UserId = userId, request.RefreshToken },
                    transaction
                );
            var refreshTokenExists = refreshTokenExpires.Count() == 1;

            if (!refreshTokenExists)
            {
                throw new InvalidRefreshTokenException();
            }
            if (refreshTokenExpires.First() < DateTime.UtcNow)
            {
                throw new RefreshTokenExpiredException();
            }

            var user = await connection.QuerySingleAsync<UserModel>(
                    "select [user].*, [role].[name] as 'RoleName', [role].Id as 'RoleId' from dbo.[user] " +
                    "join dbo.[role] on [role].Id = [user].RoleId " +
                    "where [user].Id = @Id",
                    new { Id = userId },
                    transaction
                );

            await connection.ExecuteAsync(
                    "delete from dbo.refresh_token " +
                    "where UserId = @UserId and RefreshToken = @RefreshToken",
                    new { UserId = userId, request.RefreshToken },
                    transaction
                );

            var newRefreshToken = Convert.ToBase64String(SecurityHelpers.GetRandomBytes(32));
            var newToken = SecurityHelpers.GenerateJwtToken(userId, configuration["Secret"], user.RoleName);

            await connection.ExecuteAsync(
                    "insert into dbo.refresh_token(UserId, RefreshToken, Expires) " +
                    "values (@UserId, @RefreshToken, @Expires)",
                    new { UserId = userId, RefreshToken = newRefreshToken, Expires = DateTime.UtcNow.AddDays(14) },
                    transaction
                );

            return new AuthTokensModel
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RefreshToken = newRefreshToken,
                JwtToken = newToken,
                Role = token.Claims.First(claim => claim.Type == "role").Value
            };
        }
    }
}
