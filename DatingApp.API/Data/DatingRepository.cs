using System.Collections.Generic;
using System.Threading.Tasks;
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
            throw new System.NotImplementedException();
        }

        public async Task<User> GetUser(int id)
        {
            var user = await m_context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            var users = await m_context.Users.Include(p => p.Photos).ToListAsync();
            return users;
        }

        public async Task<bool> SaveAll()
        {
            return await m_context.SaveChangesAsync() > 0;
        }
    }
}