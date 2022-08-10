using Azure;
using Azure.Storage.Blobs;
using Lab4.Data;
using Lab4.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lab4.Controllers
{
    public class AdvertisementsController : Controller
    {

        private readonly SchoolCommunityContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string advertisementsContainer = "advertisementimages";
        BlobContainerClient containerClient;


        public AdvertisementsController(SchoolCommunityContext context, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<IActionResult> Index(string ID)
         {
            ViewData["community"] = await (from c in _context.Communities
                            where c.ID == ID
                            select c).ToListAsync();

            ViewData["communityID"] = ID;
            var advertisements = await (from a in _context.Advertisements
                                        where a.communityID == ID
                                        select a).ToListAsync();
            return View(advertisements);
        }

   
        public IActionResult Upload(string ID)
        {
            ViewData["communityID"]=ID;
            ViewData["community"] = (from c in _context.Communities
                                          where c.ID == ID
                                          select c).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile advertisementImage,string communityId) 
        {
            //File is not selected by the user return to same page
            if (advertisementImage == null) 
            {
                return RedirectToAction("Upload", new { id = communityId });
            }
            // Create the container and return a container client object
            try
            {
                    containerClient = await _blobServiceClient.CreateBlobContainerAsync(advertisementsContainer);                
            }
            catch (RequestFailedException)
            {
                    containerClient = _blobServiceClient.GetBlobContainerClient(advertisementsContainer);
            }
            // Give access to public
            containerClient.SetAccessPolicy(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);

            try
            {
                // create the blob to hold the data
                var blockBlob = containerClient.GetBlobClient(advertisementImage.FileName);

                if (await blockBlob.ExistsAsync())
                {
                    await blockBlob.DeleteAsync();
                }

                using (var memoryStream = new MemoryStream())
                {
                    // copy the file data into memory
                    await advertisementImage.CopyToAsync(memoryStream);

                    // navigate back to the beginning of the memory stream
                    memoryStream.Position = 0;

                    // send the file to the cloud
                    await blockBlob.UploadAsync(memoryStream);
                    memoryStream.Close();
                }

                // add the image to database if it uploaded successfully
                var image = new Advertisement();
                image.url = blockBlob.Uri.AbsoluteUri;
                image.fileName = advertisementImage.FileName;
                image.communityID = communityId;
                
                _context.Advertisements.Add(image);
                _context.SaveChanges();
            }
            catch (RequestFailedException)
            {
                View("Error");
            }

            return RedirectToAction("Index", new { id = communityId});

        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Join Advertisement and Community table where advertisementId equals parameter Id
            var communityAds = (from a in _context.Advertisements
                                        join c in _context.Communities on
                                        a.communityID equals c.ID
                                        where a.advertisementId == id
                                        select c
                                        ).ToList();

            //Store following details from the above query
            foreach (var item in communityAds)
            {
                ViewData["communityAds"] = item.Title;
                ViewData["communityID"] = item.ID;
            }

            var image = await _context.Advertisements
                .FirstOrDefaultAsync(m => m.advertisementId == id);
            if (image == null)
            {
                return NotFound();
            }

            return View(image);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var image = await _context.Advertisements.FindAsync(id);


            // Get the container and return a container client object
            try
            {
                    containerClient = _blobServiceClient.GetBlobContainerClient(advertisementsContainer);
            }
            catch (RequestFailedException)
            {
                return View("Error");
            }

            try
            {
                // Get the blob that holds the data
                var blockBlob = containerClient.GetBlobClient(image.fileName);
                if (await blockBlob.ExistsAsync())
                {
                    await blockBlob.DeleteAsync();
                }

                _context.Advertisements.Remove(image);
                await _context.SaveChangesAsync();

            }
            catch (RequestFailedException)
            {
                return View("Error");
            }

            return RedirectToAction("Index" , new { id = image.communityID});
        }

    }
}
