using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository m_repo;
        private readonly IMapper m_mapper;
        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            this.m_mapper = mapper;
            this.m_repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var userFromRepo = await m_repo.GetUser(currentUserId);

            userParams.UserId = currentUserId;
            if (string.IsNullOrEmpty(userParams.Gender)){
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }

            var users = await m_repo.GetUsers(userParams);
            var usersToReturn = m_mapper.Map<IEnumerable<UserForListDto>>(users);

            Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
            return Ok(usersToReturn);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await m_repo.GetUser(id);
            var userToReturn = m_mapper.Map<UserForDetailsDto>(user);
            return Ok(userToReturn);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var userFromRepo = await m_repo.GetUser(id);
            m_mapper.Map(userForUpdateDto, userFromRepo);
            if (await m_repo.SaveAll())
                return NoContent();
            throw new System.Exception($"Updating uder {id} failed on save!");
        }

        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            
            var like = await m_repo.GetLike(id, recipientId);

            if (like != null){
                return BadRequest("You already liked this user.");
            }

            if (await m_repo.GetUser(recipientId) == null)
                return NotFound();
            
            like = new Like{
                LikerId = id,
                LikeeId = recipientId
            };

            m_repo.Add<Like>(like);
            if (await m_repo.SaveAll())
                return Ok();

            return BadRequest("Failed to like user.");
        }
    }
}