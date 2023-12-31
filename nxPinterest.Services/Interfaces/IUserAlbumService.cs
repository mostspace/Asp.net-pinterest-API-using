﻿using nxPinterest.Data.Models;
using nxPinterest.Data.ViewModels;
using nxPinterest.Services.Models.Request;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nxPinterest.Services.Interfaces
{
    public interface IUserAlbumService
    {
        Task<bool> Create(CreateUserAlbumRequest model, string userId, int container_id);

        Task<string> CreateAlbumShare(CreateUserAlbumSharedRequest model, string userId);

        Task<IEnumerable<UserAlbumViewModel>> GetAlbumUserByContainer(int container, string searchKey ="");

        Task<int> GetAlbumIdByPathUrlAsync(string pathUrl);

        Task<int> GetAlbumIdByNameAsync(string albumName);

        Task<DateTime?> GetCreateDateAlbumNameAsync(int albumId);

        UserAlbum UpdateAlbumAsync(int albumId, UserAlbum model);

        Task<bool> IsAlbumNameExistAsync(string albumName);

        Task<UserAlbum> RemoveAlbum(string albumName);

        Task<IEnumerable<SharedLinkAlbumMediaViewModel>> GetListAlbumById(int albumId, int pageIndex);

        Task<bool> RemoveMediaFromAlbum(int albumId, List<int> mediaIdList);

        Task<IEnumerable<UserAlbumViewModel>> GetSharedAlbumByUser(string user_id, string role, int containerId);

        Task<bool> DeleteAlbum(int albumId);

        bool UpdateAlbumThumbnail(int albumId, UserAlbum model);

        Task<IList<UserMediaAlbumViewModel>> GetMediaAlbumsAsync(int mediaId, Data.Enums.AlbumType albumType);
    }
}
