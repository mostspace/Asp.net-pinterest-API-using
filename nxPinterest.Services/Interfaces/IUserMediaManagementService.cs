﻿using nxPinterest.Data.Models;
using nxPinterest.Services.Models.Request;
//using nxPinterest.Services.Models.Response;
using nxPinterest.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace nxPinterest.Services.Interfaces
{
    public interface IUserMediaManagementService
    {
        //Task<IList<Data.Models.UserMedia>> ListUserMediaAsyc(string userId = "");
        Task<IList<UserMedia>> SearchUserMediaAsync(string searchKey, int containerId, int skip = 0, int take = dev_Settings.displayMaxItems_search);
        //Task<IList<Data.Models.UserMedia>> SearchSimilarImagesAsync(UserMedia userMedia);
        Task<IList<UserMedia>> SearchAlbumMediaAsync(string searchKey, int skip = 0, int take = dev_Settings.displayMaxItems_search);

        Task<UserMedia> GetUserMediaAsync(int mediaId);
        //Task<UserMediaDetailModel> GetUserMediaDetailsByIDAsync(int mediaId);
        Task<IList<UserMedia>> GetUserMediaSameTitleMediasAsync(UserMedia media);
        Task<IList<UserMedia>> GetUserMediaRelatedMediasAsync(UserMedia media, int skip = 0, int take = dev_Settings.displayMaxItems_search);
        //Task DeleteFromUserMedia(UserMedia userMedia);
        Task DeleteFromUserMediaList(List<UserMedia> userMediaList);
        //Task DeleteFromUserMedia(string media_id);
        Task<IList<string>> GetOftenUseTagsAsyc(int containerId, string searchKey = "", int take = 100);
        IList<UserMedia> UploadMediaFile(ImageRegistrationRequests request, string UserId);
        public void UploadIndividualMediaFile(IndividualImageRegistrationRequests request, string UserId);
        public bool UpdateUserMedia(UserMedia userMedia);
        public void thumbnailRecovery(string UserId);
    }
}
