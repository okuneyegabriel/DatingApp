using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userid}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository m_repo;
        private readonly IMapper m_mapper;
        private readonly IOptions<CloudinarySettings> m_cloudinaryConfig;
        private Cloudinary m_cloudinary;
        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this.m_cloudinaryConfig = cloudinaryConfig;
            this.m_mapper = mapper;
            this.m_repo = repo;
            Account acc = new Account{
                Cloud = cloudinaryConfig.Value.CloudName,
                ApiKey = cloudinaryConfig.Value.ApiKey,
                ApiSecret = cloudinaryConfig.Value.ApiSecret
            };
            m_cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name= "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id){
            var photoFromRepo = await m_repo.GetPhoto(id);
            var photo = m_mapper.Map<PhotoForReturnDto>(photoFromRepo);
            return Ok(photo);
        }


        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto){
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var userFromRepo = await m_repo.GetUser(userId);

            var file = photoForCreationDto.File;
            var uploadResults = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream()){
                    var uploadParams = new ImageUploadParams(){
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation()
                            .Width(500)
                            .Height(500)
                            .Crop("fill")
                            .Gravity("face")
                    };
                    uploadResults = m_cloudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResults.Uri.ToString();
            photoForCreationDto.PublicId = uploadResults.PublicId;

            var photo = m_mapper.Map<Photo>(photoForCreationDto);

            if (userFromRepo.Photos.Any(u => u.IsMain))
                photo.IsMain = false;

            userFromRepo.Photos.Add(photo);

            if (await m_repo.SaveAll()){
                var photoToReturn = m_mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new { userId = userId, id = photo.Id}, photoToReturn);
            }
                
            return BadRequest("Could not add photo.");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id){
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var userFromRepo = await m_repo.GetUser(userId);
            if (!userFromRepo.Photos.Any(p => p.Id == id)){
                return Unauthorized();
            }

            var photoFromRepo = await m_repo.GetPhoto(id);
            if (photoFromRepo.IsMain) return BadRequest("Already set to main photo!");

            var currentMainPhoto = await m_repo.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if (await m_repo.SaveAll())
                return NoContent();
            return BadRequest("Could not set photo to main");
            
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var userFromRepo = await m_repo.GetUser(userId);
            if (!userFromRepo.Photos.Any(p => p.Id == id)){
                return Unauthorized();
            }

            var photoFromRepo = await m_repo.GetPhoto(id);
            if (photoFromRepo.IsMain) return BadRequest("Cannot delete your main photo");

            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);
                var result = m_cloudinary.Destroy(deleteParams);

                if (result.Result ==  "ok"){
                    m_repo.Delete<Photo>(photoFromRepo);
                } 
            }

            else{
                 m_repo.Delete(photoFromRepo);
            }
            
            if (await m_repo.SaveAll())
                    return Ok();

                    return BadRequest("Failed to delete photo");
        }
    }
}