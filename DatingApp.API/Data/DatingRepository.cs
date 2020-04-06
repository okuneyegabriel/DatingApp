using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext m_context;
        public DatingRepository(DataContext context)
        {
            this.m_context = context;

        }
        public void Add<T>(T entity) where T : class
        {
            m_context.Add<T>(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            m_context.Remove<T>(entity);
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await m_context.Photos.Where(u => u.UserId == userId).FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await m_context.Photos.FirstOrDefaultAsync(p => p.Id == id);
            return photo;
        }

        public async Task<User> GetUser(int id)
        {
            var user = await m_context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = m_context.Users.Include(p => p.Photos).AsQueryable();
            users = users.Where(u => u.Id != userParams.UserId).Where(u => u.Gender == userParams.Gender).OrderByDescending(u => u.LastActive);

            if (userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                var minDob = DateTime.Today.AddYears(-userParams.MinAge);
                var maxDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                users = users.Where(u => u.DateOfBirth <= minDob && u.DateOfBirth>= maxDob);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy)){
                switch (userParams.OrderBy){
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<bool> SaveAll()
        {
            return await m_context.SaveChangesAsync() > 0;
        }
    }
}