﻿using Microsoft.EntityFrameworkCore;
using nxPinterest.Data.Models;
using nxPinterest.Data.Repositories.Interfaces;
using nxPinterest.Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nxPinterest.Data.Repositories
{
    public class UserAlbumRepository : BaseRepository<UserAlbum>, IUserAlbumRepository
    {
        public UserAlbumRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<bool> CheckExpiryDayAlbum(int albumId)
        {
            if (albumId == 0)
            {
                return false;
            }

            UserAlbum result = await Context.UserAlbums.SingleOrDefaultAsync(n => n.AlbumId == albumId);

            if (result is null)
            {
                return false;
            }

            DateTime? expiryDate = result.AlbumExpireDate;
            TimeSpan diff = (TimeSpan)(DateTime.UtcNow - expiryDate);

            // if day >30 has expired
            return diff.Days > 30;
        }

        public async Task<IEnumerable<UserAlbumViewModel>> GetAlbumByUser(string userId)
        {
            var listAlbum = new List<UserAlbumViewModel>();

            if (string.IsNullOrEmpty(userId)) return new List<UserAlbumViewModel>();

            var result = await Context.UserAlbums.Select(n => new UserAlbumViewModel
            {
                UserId = n.UserId,
                AlbumName = n.AlbumName,
                AlbumId = n.AlbumId,
                AlbumCreatedat = n.AlbumCreatedat,
                AlbumUrl = n.AlbumUrl
            }).Where(n => n.UserId == userId).OrderByDescending(n => n.AlbumCreatedat).ToListAsync();

            foreach (var item in result)
            {
                if (item == null) continue;

                var userAlbum = new UserAlbumViewModel
                {
                    AlbumName = item.AlbumName,
                    UserId = item.UserId,
                    AlbumCreatedat = item.AlbumCreatedat
                };

                var imageUrl = item.AlbumUrl.Split(';', ' ');

                if (!string.IsNullOrEmpty(item.AlbumUrl) && imageUrl.Length > 1)
                    userAlbum.FirstImageAlbum = imageUrl[1];

                listAlbum.Add(userAlbum);
            }

            return listAlbum;
        }

        public (int albumId, string albumName) IsUserAlbumAlreadyExists(string albumName)
        {
            if (string.IsNullOrEmpty(albumName)) return (0, null);

            var result = Context.UserAlbums.Select(n => new
            {
                n.AlbumName,
                n.AlbumId
            }).SingleOrDefault(n => n.AlbumName == albumName);

            return result != null ? (result.AlbumId, result.AlbumName) : (0, null);
        }
    }
}
