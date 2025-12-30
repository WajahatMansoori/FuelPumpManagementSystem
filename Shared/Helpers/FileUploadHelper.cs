using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Shared.Helpers
{
    public class FileUploadHelper
    {

        private readonly string _bucketName;
        public FileUploadHelper(IConfiguration configuration)
        {
        }

        public async Task<string> UploadFileToLocalAsync(IFormFile file, string folderName = "Uploads")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return null;

                // Get base path (wwwroot/Uploads)
                var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadPath = Path.Combine(wwwRootPath, folderName);

                // Ensure directory exists
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                // Create unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                fileName = $"{fileName}-{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, fileName);

                // Copy file to local path
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for web access
                return $"/{folderName}/{fileName}";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> DeleteFileFromLocalAsync(string relativeFilePath, string folderName = "Uploads")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativeFilePath))
                    return false;

                // Get base path (wwwroot/Uploads)
                var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadPath = Path.Combine(wwwRootPath, folderName);

                // Build full file path
                var filePath = Path.Combine(wwwRootPath, relativeFilePath.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    return true;
                }

                return false; // File not found
            }
            catch (Exception ex)
            {
                // Log exception if needed
                throw;
            }
        }

        public string GetRelativePathFromUrl(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return null;

            var uri = new Uri(fileUrl);
            return uri.AbsolutePath; // e.g. "/Uploads/icons8-privacy-policy-16-f8be64f5-8c28-4728-8d19-28c4723bfabe.png"
        }


    }
}
