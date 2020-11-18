using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using ImgProcLib;

namespace ViewModel
{
    public class ImageRecognized
    {
        public int Id { get; set; }
        public string ImageName { get; set; }
        public string Path { get; set; }
        public PredictionResult ImageType { get; set; }
        public ImageRecognizedDetails ImageRecognizedDetails { get; set; }
    }

    public class PredictionResult
    {

        public int Id { get; set; }
        public string PredictionStringResult { get; set; }
        public ICollection<ImageRecognized> RecognizedImages { get; set; }
    }

    public class ImageRecognizedDetails
    {
        public int Id { get; set; }

        public byte[] BinaryFile { get; set; }
    }


    public class ImagesLibraryContext : DbContext
    {

        //some trick from stackoverflow
        public ImagesLibraryContext() { Database.EnsureCreated(); }

        public DbSet<PredictionResult> TypesOfImages { get; set; }
        public DbSet<ImageRecognized> Images { get; set; }

        public DbSet<ImageRecognizedDetails> Details { get; set; }



        protected override void OnConfiguring(DbContextOptionsBuilder o)
        => o.UseSqlite("Data Source=library.db");

        //Check if database contains recognition results for asked file
        //return null if nothing found
        public ReturnMessage SearchFile(string imagePath)
        {
            //know name of image
            string fileName = Path.GetFileName(imagePath);

            int numOfRepeatedFiles = 0;
            var query = Images.Where(imageSet => imageSet.Path.Equals(imagePath));
            numOfRepeatedFiles = query.Count();

            if (numOfRepeatedFiles > 0)

            {
                ImageRecognized imageRecognized = null;

                foreach (var fileInQuery in query)
                {

                    Entry(fileInQuery).Reference(obj => obj.ImageRecognizedDetails).Load();
                    Entry(fileInQuery).Reference(obj => obj.ImageType).Load();


                    byte[] bdBinaryFile = fileInQuery.ImageRecognizedDetails.BinaryFile;


                    Stream stream = File.OpenRead(imagePath);
                    byte[] byteArrayImage = new byte[stream.Length];
                    stream.Read(byteArrayImage, 0, (int)stream.Length);



                    if (IsByteArraysEqual(bdBinaryFile, byteArrayImage))
                    {

                        imageRecognized = fileInQuery;
                        break;
                    }
                }

                if (imageRecognized != null)
                {


                    return new ReturnMessage(imagePath, imageRecognized.ImageType.PredictionStringResult);
                }
                else
                //didn't found
                {
                    return null;
                }

            }
            else
            //it's a new image
            {
                return null;
            }
        }

        //Adding new results in database
        public void AddRecognitionResultToDatabase(ReturnMessage processedImage)
        {
            //create new db element that will be added
            ImageRecognized imgStruct = new ImageRecognized();

            imgStruct.ImageName = Path.GetFileName(processedImage.FullFilePath);
            imgStruct.Path = processedImage.FullFilePath;

            imgStruct.ImageRecognizedDetails = new ImageRecognizedDetails();

            //extract binary content from file
            Stream stream = System.IO.File.OpenRead(processedImage.FullFilePath);
            byte[] byteArrayImage = new byte[stream.Length];
            stream.Read(byteArrayImage, 0, (int)stream.Length);

            //set binary content
            imgStruct.ImageRecognizedDetails.BinaryFile = byteArrayImage;


            PredictionResult predictionResult;
            try
            {
                predictionResult = TypesOfImages.First(obj => obj.PredictionStringResult.Equals(processedImage.PredictionStringResult));
                Entry(predictionResult).Collection(obj => obj.RecognizedImages).Load();
            }
            catch (InvalidOperationException)
            {
                predictionResult = null;
            }
            if (predictionResult == null)
            {
                imgStruct.ImageType = new PredictionResult() { PredictionStringResult = processedImage.PredictionStringResult };
                imgStruct.ImageType.RecognizedImages = new List<ImageRecognized>();
                imgStruct.ImageType.RecognizedImages.Add(imgStruct);

                Images.Add(imgStruct);
                TypesOfImages.Add(imgStruct.ImageType);
                Details.Add(imgStruct.ImageRecognizedDetails);
            }
            else
            {
                imgStruct.ImageType = predictionResult;
                imgStruct.ImageType.RecognizedImages.Add(imgStruct);

                Images.Add(imgStruct);
                Details.Add(imgStruct.ImageRecognizedDetails);
            }
            SaveChanges();
        }

        //function to check if two byte arrays are equal
        private bool IsByteArraysEqual(byte[] f1, byte[] f2)
        {
            if (f1.Length != f2.Length)
                return false;

            for (int i = 0; i < f1.Length; i++)
                if (f1[i] != f2[i])
                    return false;
            return true;
        }

        //clear external storage
        public void ResetDatabase()
        {
            foreach (var detail in Details)
                Details.Remove(detail);
            foreach (var type in TypesOfImages)
                TypesOfImages.Remove(type);
            foreach (var image in Images)
                Images.Remove(image);
        }
        public int GetNumOfEachType(string type)
        {
            var curType = TypesOfImages.Where(t => t.PredictionStringResult.Equals(type)).FirstOrDefault();
            if (curType == null)
                return 0;
            else
            {
                Entry(curType).Collection(obj => obj.RecognizedImages).Load();
                return curType.RecognizedImages.Count();
            }
        }
    }

}