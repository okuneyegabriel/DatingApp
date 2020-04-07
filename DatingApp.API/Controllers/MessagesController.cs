using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Models;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository m_repo;
        private readonly IMapper m_mapper;
        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            this.m_mapper = mapper;
            this.m_repo = repo;
        }

        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var messageFromRepo = await m_repo.GetMessage(id);

            if (messageFromRepo == null)
                return NotFound();
            return Ok(messageFromRepo);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId){
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var messageFromRepo = await m_repo.GetMessageThread(userId, recipientId);
            var messageThread = m_mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);
            return Ok(messageThread);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto){
            var sender = await m_repo.GetUser(userId);

            if (sender.Id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            messageForCreationDto.SenderId = userId;

            var recipient = await m_repo.GetUser(messageForCreationDto.RecipientId);
            if (recipient == null)
                BadRequest("Could not find user!");

            var message = m_mapper.Map<Message>(messageForCreationDto);
            m_repo.Add(message);
            
            if (await m_repo.SaveAll())
            {
                var messageToReturn = m_mapper.Map<MessageToReturnDto>(message);
                return CreatedAtRoute("GetMessage", new {userId, id = message.Id}, messageToReturn);
            }
            throw new Exception("Failed on save.");
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery]MessageParams messageParams){
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            messageParams.UserId = userId;
            var messageFromRepo = await m_repo.GetMessagesForUser(messageParams);
            var messages = m_mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);

            Response.AddPagination(messageFromRepo.CurrentPage, messageFromRepo.PageSize, messageFromRepo.TotalCount, messageFromRepo.TotalPages);
            return Ok(messages); 
        }

        [HttpPost("{id}")]
        public async Task<IActionResult>DeleteMessage(int id, int userId){
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var messageFromRepo = await m_repo.GetMessage(id);

            if (messageFromRepo.SenderId == userId){
                messageFromRepo.SenderDeleted = true;
            }

            if (messageFromRepo.RecipientId == userId){
                messageFromRepo.RecipientDeleted = true;
            }

            if (messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
                m_repo.Delete(messageFromRepo);
            
            if (await m_repo.SaveAll())
                return NoContent();
            throw new Exception("Error deleting the message!");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id){
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var message = await m_repo.GetMessage(id);

            if (message.RecipientId != userId) return Unauthorized();

            message.IsRead = true;
            message.DateRead = DateTime.Now;

            await m_repo.SaveAll();
            return NoContent();
        }
        
    }
}