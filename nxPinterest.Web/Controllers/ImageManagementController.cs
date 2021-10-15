﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using nxPinterest.Data;
using nxPinterest.Data.Models;
using nxPinterest.Services;
using nxPinterest.Services.Models.Request;
using nxPinterest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace nxPinterest.Web.Controllers
{
    [Authorize]
    public class ImageManagementController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private StorageBlobService blobService;
        private readonly ApplicationDbContext _context;
        private Base64stringUtility encode = new Base64stringUtility("UTF-8");
        public ImageManagementController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            this._logger = logger;
            this._context = context;
            blobService = new StorageBlobService();
        }


        [HttpPost]
        public async Task<IActionResult> UploadMediaFile(ImageRegistrationRequests request)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string title = request.Title;
                    string description = request.Description;
                    IList<IFormFile> images = request.Images;
                    string projectTags = request.ProjectTags;

                    for (int i = 0; i < images.Count; i++)
                    {
                        IFormFile _imageFile = images[i];


                        // Upload image file to Azure Blob
                        string containerName = Services.dev_Settings.blob_containerName_image;
                        string fileName = _imageFile.FileName;

                        // If same name file exist, change file name.
                        blobService.CreateContainerIfNotExistsAsync(containerName);
                        var existFiles = blobService.GetBlobFileList(containerName);
                        if (existFiles.Result != null)
                        {
                            foreach (var existfile in existFiles.Result)
                            {
                                if (fileName.Equals(existfile.ToString()))
                                {
                                    fileName = DateTime.Now.ToString("yyyyMMddHHmmss_") + fileName;
                                    break;
                                }
                            }
                        }

                        // Upload file (no Validation)
                        Stream imageStream = _imageFile.OpenReadStream();
                        var result = blobService.UploadImageBlobAsync(fileName, containerName, (IFormFile)_imageFile);
                        string tagsString, tagsJson;
                        string[] ids = new string[3];

                        if (result != null)
                        {
                            ComputerVisionService cv = new ComputerVisionService();
                            UserMedia userMedia = new UserMedia();

                            // 2 patterns in the prototype
                            tagsString = cv.GetImageTag_str(result.Result.Uri.ToString());          // Get as parsable string -> 1) SQL and 3) Table
                            tagsJson = cv.GetImageTag_json(result.Result.Uri.ToString());           // Get as json            -> 2) Cosmos and 4) Blob 


                            // Create Model data
                            userMedia.UserId = this.UserId;                                      // User ID
                            userMedia.MediaUrl = result.Result.Uri.ToString();                      // URL of the blob
                            userMedia.MediaFileName = result.Result.Name;                           // File name
                            userMedia.MediaFileType = result.Result.Name.Split('.').Last();         // File type
                            userMedia.Tags = tagsString;                                            // Tags (Parsable string)

                            // Add image info to json (same as model)
                            tagsJson = ImageAnalysisUtility.AddImageInfoToTag_json(tagsJson, userMedia);


                            // ---------- 1) [SQL database] (as SQL schema Record) ---------- 
                            _context.UserMedia.Add(userMedia);
                            await _context.SaveChangesAsync();


                            // ----------  2) [Azure Cosmos DB] (as json Item) ---------- 
                            CosmosDbService cosmosDbService = new CosmosDbService();
                            bool insertImageResult = await cosmosDbService.InsertOneItemAsync(dev_Settings.cosmos_databaseName, dev_Settings.cosmos_containerName, tagsJson);
                            if (!insertImageResult) throw new Exception("Cosmos DB への登録に失敗しましたk");

                            ids[0] = cosmosDbService.inserted_id;  // [TEST] for Corresponding IDs

                            // ---------- 3) [Azure Storage Table] (as NoSQL Entity) ----------

                            UserMediaStorageTableEntity userMediaStorageTableEntity = new UserMediaStorageTableEntity();
                            userMediaStorageTableEntity.UserId = userMedia.UserId;
                            userMediaStorageTableEntity.MediaUrl = userMedia.MediaUrl;
                            userMediaStorageTableEntity.MediaFileName = userMedia.MediaFileName;
                            userMediaStorageTableEntity.MediaFileType = userMedia.MediaFileType;
                            userMediaStorageTableEntity.Tags = userMedia.Tags;
                            userMediaStorageTableEntity.DateTimeUploaded = userMedia.DateTimeUploaded;

                            StorageTableService tableService = new StorageTableService();
                            insertImageResult = await tableService.InsertOrMerageEntityAsync(userMediaStorageTableEntity);
                            if (!insertImageResult) throw new Exception("Storage Table への登録に失敗しました");

                            ids[1] = tableService.inserted_id;      // [TEST] for Corresponding IDs


                            // ---------- 4) [Azure Storage Blob] (as json file) ----------

                            string datetimeStr = DateTime.Now.ToString("yyyyMMddHHmmss_");

                            UserMediaBlobJSON userMediaBlobJSON = JsonConvert.DeserializeObject<UserMediaBlobJSON>(tagsJson);
                            userMediaBlobJSON.Key = (encode.Encode(userMedia.UserId + datetimeStr + userMedia.MediaFileName)).Replace("+", "=");
                            tagsJson = JsonConvert.SerializeObject(userMediaBlobJSON, Formatting.None);
                            fileName = "UserMedia/" + userMedia.UserId + "/" + datetimeStr + userMedia.MediaFileName + ".json";

                            insertImageResult = await blobService.StoreJsonBlobAsync(fileName, dev_Settings.blob_containerName_json, tagsJson);
                            if (!insertImageResult) throw new Exception("Storage Blob への登録に失敗しました");

                            ids[2] = fileName;      // [TEST] for Corresponding IDs

                            // ---------- Save corresponding IDs (use for edit and delete) ----------

                            MediaId mediaId = new MediaId();
                            mediaId.Sql_id = userMedia.MediaId;
                            mediaId.Cosmos_db = ids[0];
                            mediaId.Storage_table = ids[1];
                            mediaId.Storage_blob = ids[2];

                            _context.MediaId.Add(mediaId);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return RedirectToAction("Index", "Home");
        }

    }
}