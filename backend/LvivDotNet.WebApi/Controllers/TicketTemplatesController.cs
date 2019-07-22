﻿using System.Threading.Tasks;
using LvivDotNet.Application.TicketTemplates.Commands.AddTicketTemplate;
using LvivDotNet.Application.TicketTemplates.Commands.DeleteTicketTemplate;
using LvivDotNet.Application.TicketTemplates.Commands.UpdateTicketTemplate;
using LvivDotNet.Application.TicketTemplates.Models;
using LvivDotNet.Application.TicketTemplates.Queries.GetTicketTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvivDotNet.WebApi.Controllers
{
    /// <summary>
    /// Ticket templates controller.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketTemplatesController : BaseController
    {
        private readonly IMediator mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="TicketTemplatesController"/> class.
        /// </summary>
        /// <param name="mediator"> Mediator service. </param>
        public TicketTemplatesController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        /// <summary>
        /// Add ticket template.
        /// </summary>
        /// <param name="command"> Add ticket template command. </param>
        /// <returns> New ticket template id. </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public Task<int> AddTicketTemplate(AddTicketTemplateCommand command)
            => this.mediator.Send(command);

        /// <summary>
        /// Gets ticket template.
        /// </summary>
        /// <param name="id"> Ticket template id.  </param>
        /// <returns>Ticket template model. </returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public Task<TicketTemplateModel> GetTicketTemplate(int id)
            => this.mediator.Send(new GetTicketTemplateQuery { Id = id });

        /// <summary>
        /// Deletes ticket template.
        /// </summary>
        /// <param name="id"> Ticket template id. </param>
        /// <returns> Empty Task. </returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public Task DeleteTicketTemplate(int id)
            => this.mediator.Send(new DeleteTicketTemplateCommand { Id = id });

        /// <summary>
        /// Updates ticket template.
        /// </summary>
        /// <param name="command"> Update ticket template command. <see cref="AddTicketTemplateCommand"/>. </param>
        /// <returns> Empty Task. </returns>
        [HttpPut]
        [Authorize(Roles = "Admin")]
        public Task UpdateTicketTemplate(UpdateTicketTemplateCommand command)
            => this.mediator.Send(command);
    }
}