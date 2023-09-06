using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System.Net;

namespace UploadAWS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool keepGoing = true;

            try
            {
                while (keepGoing)
                {
                    Console.WriteLine("Digite '1' para upload via link;\nDigite '2' para upload via arquivo;\nDigite '3' para finalizar;");
                    string method = $"{Console.ReadLine()}";

                    if (String.IsNullOrEmpty(method))
                    {
                        // Repete o inicio
                        Console.WriteLine("");
                    }
                    else if (method.Equals("1"))
                    {
                        Console.WriteLine(""); // Pula linha

                        Console.WriteLine("URL da imagem: ");
                        string imgUrl = $"{Console.ReadLine()}";
                        Console.WriteLine("Novo nome da imagem (com extensão): ");
                        string fileName = $"{Console.ReadLine()}";

                        Console.WriteLine(""); // Pula linha
                        Console.WriteLine(UploadLinkToS3(imgUrl, fileName).Result);
                        Console.WriteLine("---------------------------------------"); // Pula linha
                        Console.WriteLine(""); // Pula linha
                    }
                    else if (method.Equals("2"))
                    {
                        Console.WriteLine(""); // Pula linha

                        Console.WriteLine("Caminho local da imagem: ");
                        string imgUrl = $"{Console.ReadLine()}";
                        Console.WriteLine("Novo nome da imagem (com extensão): ");
                        string fileName = $"{Console.ReadLine()}";

                        Console.WriteLine(""); // Pula linha
                        Console.WriteLine(UploadLinkToS3(imgUrl, fileName).Result);
                        Console.WriteLine("---------------------------------------"); // Pula linha
                        Console.WriteLine(""); // Pula linha
                    }
                    else
                    {
                        keepGoing = false;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro: {ex.Message}");
            }
        }

        async static Task<string> UploadLinkToS3(string? imgUrl, string? fileName)
        {
            try
            {
                if (String.IsNullOrEmpty(imgUrl) || String.IsNullOrEmpty(fileName))
                {
                    return "Preencha todos os campos!";
                }

                var bytemimg = new WebClient().DownloadData(imgUrl);
                var awsFile = string.Empty;
                using (var memoryStream = new MemoryStream(bytemimg))
                {
                    awsFile = await ConfigureUploadToS3(new FormFile(memoryStream, 0, memoryStream.Length, fileName, fileName));
                }

                return $"URL da nova imagem: {awsFile}";
            }
            catch (Exception ex)
            {
                return $"Ocorreu um erro: {ex.Message}";
            }
        }

        async static Task<string> UploadFileToS3(dynamic? imgUrl, string? fileName)
        {
            try
            {
                if (String.IsNullOrEmpty(imgUrl) || String.IsNullOrEmpty(fileName))
                {
                    return "Preencha todos os campos!";
                }

                string awsFile = string.Empty;
                using (var memoryStream = new MemoryStream())
                {
                    await imgUrl.CopyToAsync(memoryStream);

                    var bytemimg = memoryStream.ToArray();
                    var newFile = string.Empty;
                    using (var memoryStream2 = new MemoryStream(bytemimg))
                    {
                        newFile = await ConfigureUploadToS3(new FormFile(memoryStream2, 0, memoryStream2.Length, fileName, fileName));
                    }
                    awsFile = newFile;
                }

                return $"URL da nova imagem: {awsFile}";
            }
            catch (Exception ex)
            {
                return $"Ocorreu um erro: {ex.Message}";
            }
        }

        async static Task<string> ConfigureUploadToS3(IFormFile file)
        {
            string nomearquivo = "";
            try
            {
                using (var client = new AmazonS3Client(Auth.awsKey, Auth.awsSecret, RegionEndpoint.USEast2))
                {
                    using (var newMemoryStream = new MemoryStream())
                    {
                        file.CopyTo(newMemoryStream);

                        nomearquivo = file.FileName;

                        var uploadRequest = new TransferUtilityUploadRequest
                        {
                            InputStream = newMemoryStream,
                            Key = file.FileName,
                            BucketName = Auth.awsBucket,
                            CannedACL = S3CannedACL.PublicRead,
                            ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.None,
                            StorageClass = S3StorageClass.OneZoneInfrequentAccess
                        };

                        var fileTransferUtility = new TransferUtility(client);
                        await fileTransferUtility.UploadAsync(uploadRequest);
                    }
                }

                return Auth.awsFullBucketUrl + nomearquivo;
            }
            catch (Exception ex)
            {
                return $"Ocorreu um erro: {ex.Message}";
            }

        }
    }
}