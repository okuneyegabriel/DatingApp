using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
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
        public async Task<IActionResult> GetUsers()
        {
            var users = await m_repo.GetUsers();
            var usersToReturn = m_mapper.Map<IEnumerable<UserForListDto>>(users);
            return Ok(usersToReturn);
        }

        [HttpGet("{id}")]
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
    }
}