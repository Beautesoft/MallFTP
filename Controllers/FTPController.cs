using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace MallFTP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FTPController : ControllerBase
    {
        string ftpUrl = "ftp://ftp.jurongpoint.com.sg";
        string ftpUsername = "1030300";
        string ftpPassword = "apetqf2e";

        [HttpPost("upload-text-file", Name ="textfileupload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            // Check if the file is null or empty
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Check if the file size exceeds 10 MB (10 * 1024 * 1024 bytes)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest("File size exceeds the 10 MB limit.");
            }

            // Check if the file is a text file
            string[] permittedExtensions = { ".txt" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file type. Only .txt files are allowed.");
            }

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var content = await reader.ReadToEndAsync();
                // Process the content of the file as needed
                // For example, you can save it to a database or another storage
               
                string localFilePath = @"D:\Learning\test2.txt";

                try
                {
                    UploadFileToFtp(ftpUrl, file, ftpUsername, ftpPassword);
                    //ListDirectoryContentsActiveMode(ftpUrl, ftpUsername, ftpPassword);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                return Ok(new { FileName = content + " file uploaded" });
            }
        }

        [HttpGet("list-files", Name = "ListFiles")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            try
            {
                List<string> fileList = ListFilesOnFtp(ftpUrl, ftpUsername, ftpPassword);
                return Ok(fileList); // Returns a JSON response
            }
            catch (WebException webEx)
            {
                if (webEx.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = (FtpWebResponse)webEx.Response;
                    return StatusCode((int)response.StatusCode, response.StatusDescription);
                }
                else
                {
                    return StatusCode(500, "Internal server error");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
            /*
            try
            {
                List<string> fileList = ListFilesOnFtp(ftpUrl, ftpUsername, ftpPassword);
                return Ok(fileList); // Returns a JSON response
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }*/
        }
        public static void UploadFileToFtp(string ftpUrl, IFormFile formFile, string username, string password)
        {
            // Create FTP request
            string fileName = formFile.FileName;
            string finalURL = ftpUrl + "/"+ fileName;
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(finalURL);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(username, password);
            request.UsePassive = false;
            request.UseBinary = true;
            request.KeepAlive = false;

            // Read the file data from IFormFile
            byte[] fileContents;
            using (var ms = new MemoryStream())
            {
                formFile.CopyTo(ms);
                fileContents = ms.ToArray();
            }

            // Upload the file
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            // Get response from FTP server
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            }
        }
        public static List<string> ListFilesOnFtp(string ftpUrl, string username, string password)
        {

            List<string> files = new List<string>();
            try
            {
                // Create FTP request to get directory listing
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(username, password);
                request.UsePassive = false; // Typically, passive mode is recommended for FTP
                request.UseBinary = true;
                request.KeepAlive = false;


                // Get response from FTP server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        files.Add(line);
                    }
                    Console.WriteLine($"Directory List Complete, status {response.StatusDescription}");
                }
            }
            catch (WebException)
            {
                throw; // Rethrow to handle in the calling method
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving files from FTP server.", ex);
            }

            return files;
        }

    }
}
