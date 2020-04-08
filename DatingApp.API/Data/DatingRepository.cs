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

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await m_context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
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
            var user = await m_context.Users.FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = m_context.Users.AsQueryable();
            users = users.Where(u => u.Id != userParams.UserId).Where(u => u.Gender == userParams.Gender).OrderByDescending(u => u.LastActive);

            if (userParams.Likers){
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }

            if (userParams.Likees){
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }

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

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers){
            var user = await m_context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

            if (likers){
                return user.Likers.Where(u => u.LikeeId == id).Select(i => i.LikerId);
            }
            else
                return user.Likees.Where(u => u.LikerId == id).Select(i => i.LikeeId);
        }

        public async Task<bool> SaveAll()
        {
            return await m_context.SaveChangesAsync() > 0;
        }

        public async Task<Message> GetMessage(int id)
        {
            return await m_context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            var messages = m_context.Messages
            .AsQueryable();

            switch (messageParams.MessageContainer){
                case "Inbox":
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.RecipientDeleted == false);
                    break;
                case "Outbox":
                    messages = messages.Where(u => u.SenderId == messageParams.UserId && u.SenderDeleted == false);
                    break;
                default:
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.IsRead == false && u.RecipientDeleted == false);
                    break;
            }
            messages = messages.OrderByDescending(m => m.MessageSent);
            return await PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            var messages = await m_context.Messages
            .Where(m => m.RecipientId == userId && m.RecipientDeleted == false && m.SenderId == recipientId || 
                m.RecipientId == recipientId && m.SenderDeleted == false && m.SenderId == userId)
            .OrderByDescending(m => m.MessageSent).ToListAsync();
            return messages;
        }
    }
}