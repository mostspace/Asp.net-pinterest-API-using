﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using nxPinterest.Data;
using nxPinterest.Data.Models;
//using nxPinterest.Services.Models.Response;
using nxPinterest.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using nxPinterest.Services.Interfaces;
//using Microsoft.Azure.Cosmos;

namespace nxPinterest.Web.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        public const int pageSize = nxPinterest.Services.dev_Settings.displayMaxItems_search;
        private readonly IUserMediaManagementService userMediaManagementService;
        private readonly IUserAlbumService userAlbumService;
        private readonly IUserManagementService userService;
        private readonly ApplicationDbContext _context;
        //private CosmosDbService _cosmosDbService;

        public static string NoIMGURL = "https://sozosya.blob.core.windows.net/pinterest/1/thumb/noimage.png";

        public HomeController(ILogger<HomeController> logger,
                              ApplicationDbContext context,
                              IUserMediaManagementService userMediaManagementService,
                              IUserAlbumService userAlbumService,
                              IUserManagementService userService
                              )
        {
            _logger = logger;
            _context = context;
            this.userMediaManagementService = userMediaManagementService;
            this.userAlbumService = userAlbumService;
            this.userService = userService;
            //_cosmosDbService = new CosmosDbService();
        }

        /// <summary>
        /// Get view index home
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="searchKey"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(int pageIndex = 1, string searchKey = "")
        {
            HomeViewModel vm = new HomeViewModel();

            //nxPinterest.Services.CognitiveSearchService cognitiveSearchService = new Services.CognitiveSearchService();

            List<ApplicationUser> user = this._context.Users.Where(c => c.Id.Equals(this.UserId)).ToList();
            if (user[0].DisplayMode != null && user[0].DisplayMode.Equals("ALBUM")) 
            {
                string redirectUrl = string.Concat("/Home/Album?pageIndex=", pageIndex, "&searchKey=", searchKey);
                return Redirect(redirectUrl);
            }
            if (user == null || user.Count == 0) return RedirectToAction("LogOut", "Account");

            int skip = (pageIndex - 1) * pageSize;

            int container = user[0].container_id;
            
            //画面のajaxで取得と表示をしている ViewModelのListを未使用のためコメント
            //vm.UserMediaList = await this.userMediaManagementService.SearchUserMediaAsync(searchKey, user[0].container_id);

            //int totalPages = (int)System.Math.Ceiling((decimal)(vm.UserMediaList.Count / (decimal)pageSize));
            //int totalRecordCount = vm.UserMediaList.Count;

            //ViewBag.ItemCount = vm.UserMediaList.Count;
            //ViewBag.UserDispName = user[0].UserDispName;

            //vm.UserMediaList = vm.UserMediaList.Skip(skip).Take(pageSize).ToList();

            vm.PageIndex = pageIndex;
            //vm.TotalPages = totalPages;
            vm.SearchKey = searchKey;
            //vm.TotalRecords = totalRecordCount;
            vm.Discriminator = user[0].Discriminator;
            vm.UserDispName = user[0].UserDispName;

            vm.SizeRange = 3;

            //よく使用されているタグ候補
            vm.TagList = await this.userMediaManagementService.GetOftenUseTagsAsyc(user[0].container_id, searchKey, 30);

            // get user containers
            string container_ids = user[0].ContainerIds ?? "";
            string[] containerArray = container_ids.Split(',');

            if (containerArray.Length == 0 || containerArray[0] == "")
            {
                vm.UserContainers = await this._context.UserContainer.Where(c => c.container_id == user[0].container_id).ToListAsync();
            }
            else
            {
                var containerIds = containerArray
                    .Where(x => int.TryParse(x, out _))
                    .Select(int.Parse)
                    .ToList();

                vm.UserContainers = await this._context.UserContainer.Where(c => containerIds.Contains(c.container_id)).ToListAsync();
            }

            /*vm.UserContainers = await this._context.UserContainer
                            .Where(t => t.container_visibility == true)
                            .ToListAsync();*/
            //よく使用されているアルバムの一覧 TODO
           var album = await userAlbumService.GetAlbumUserByContainer(container);
           vm.AlbumList = album.Select(n=> new nxPinterest.Data.ViewModels.UserAlbumViewModel
           {
               AlbumName = n.AlbumName,
               AlbumUrl = n.AlbumUrl,
               AlbumId = n.AlbumId
           }).ToList();

            vm.currentContainer = container;

            //登録画面で使用されているタグ候補
            vm.ImageRegistrationVM.SuggestTagsList = vm.TagList;

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeContainerID(int container)
        {
            int containerID = await this.userService.UpdateUserContainerAsync(this.UserId, container);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeViewMode(int ViewMode)
        {
            int containerID = await this.userService.UpdateUserViewModeAsync(this.UserId, ViewMode);
            return Json(new { success = true });
        }

        public async Task<IActionResult> Album(int pageIndex = 1, string searchKey = "")
        {
            HomeViewModel vm = new HomeViewModel();

            //nxPinterest.Services.CognitiveSearchService cognitiveSearchService = new Services.CognitiveSearchService();

            List<ApplicationUser> user = this._context.Users.Where(c => c.Id.Equals(this.UserId)).ToList();
            if (user == null || user.Count == 0) return RedirectToAction("LogOut", "Account");

            int skip = (pageIndex - 1) * pageSize;

            int container = user[0].container_id;
            
            vm.PageIndex = pageIndex;
            //vm.TotalPages = totalPages;
            vm.SearchKey = searchKey;
            //vm.TotalRecords = totalRecordCount;
            vm.Discriminator = user[0].Discriminator;
            vm.UserDispName = user[0].UserDispName;

            vm.SizeRange = 3;

            //よく使用されているタグ候補
            vm.TagList = await this.userMediaManagementService.GetOftenUseTagsAsyc(user[0].container_id, searchKey, 30);

            // get user containers
            string container_ids = user[0].ContainerIds ?? "";
            string[] containerArray = container_ids.Split(',');

            if (containerArray.Length == 0 || containerArray[0] == "")
            {
                vm.UserContainers = await this._context.UserContainer.Where(c => c.container_id == user[0].container_id).ToListAsync();
            }
            else
            {
                var containerIds = containerArray
                    .Where(x => int.TryParse(x, out _))
                    .Select(int.Parse)
                    .ToList();

                vm.UserContainers = await this._context.UserContainer.Where(c => containerIds.Contains(c.container_id)).ToListAsync();
            }

            /*vm.UserContainers = await this._context.UserContainer
                            .Where(t => t.container_visibility == true)
                            .ToListAsync();*/
            //よく使用されているアルバムの一覧 TODO
            var album = await userAlbumService.GetAlbumUserByContainer(container);
            vm.AlbumList = album.Select(n => new nxPinterest.Data.ViewModels.UserAlbumViewModel
            {
                AlbumName = n.AlbumName,
                AlbumUrl = n.AlbumUrl,
                AlbumThumbnailUrl = n.AlbumThumbnailUrl,
                AlbumId = n.AlbumId
            }).ToList();

            vm.currentContainer = container;

            //登録画面で使用されているタグ候補
            vm.ImageRegistrationVM.SuggestTagsList = vm.TagList;
            vm.AlbumMode = true;
            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> getAlbum(string searchKey = "")
        {
            HomeViewModel vm = new HomeViewModel();
            List<ApplicationUser> user = this._context.Users.Where(c => c.Id.Equals(this.UserId)).ToList();
            
            var album = await userAlbumService.GetAlbumUserByContainer(user[0].container_id,searchKey);
            vm.AlbumList = album.Select(n => new nxPinterest.Data.ViewModels.UserAlbumViewModel
            {
                AlbumName = n.AlbumName,
                AlbumUrl = n.AlbumUrl,
                AlbumThumbnailUrl = n.AlbumThumbnailUrl,
                ImageCount = n.ImageCount,
                AlbumId = n.AlbumId
            }).ToList();
            return Json(vm);
        }
    }
}
