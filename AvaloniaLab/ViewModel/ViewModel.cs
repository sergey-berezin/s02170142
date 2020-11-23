using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Threading;
using ImgProcLib;
using System.Threading;

namespace ViewModel
{
    public class ViewModel : INotifyPropertyChanged
    {
        // Обрабатываем результаты распознования
        void PredictionHandler_WPF(object sender, PredictionEventArgs e)
        {
            ReturnMessage rm = (ReturnMessage)e.RecognitionResult;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if(!AllUniqueRecognitionResults.Contains(rm.PredictionStringResult))
                    AllUniqueRecognitionResults.Add(rm.PredictionStringResult);

                UpdateCollection(rm); 
                
                
            });
            
            lock(lockobj){
            //заносим результаты распознования в БД
                imagesLibraryContext.AddRecognitionResultToDatabase(rm);
            }
           
        }

        //выводим результаты, которые уже есть в БД
        void DatabaseFilesHandler(ReturnMessage rm)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
             {
                 if(!AllUniqueRecognitionResults.Contains(rm.PredictionStringResult))
                    AllUniqueRecognitionResults.Add(rm.PredictionStringResult);
                    
                 UpdateCollection(rm);
             });
        }

        private object lockobj;

        string _dirr = null;
        public event PropertyChangedEventHandler PropertyChanged;
        IUIServices _uiservices;

        ImageProcClass _imgProcClass;

        PredictionQueue _predictionQueue;

        public ICommand OpenCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public ICommand ResetDatabaseCommand { get; set; }

        int _numOfRecognizedImages;
        int _numOfAllImages;

        int NumOfRecognizedImages
        {
            get { return _numOfRecognizedImages; }
            set
            {
                _numOfRecognizedImages = value;
                ProgressBar = CountProgressBar(_numOfRecognizedImages, _numOfAllImages);
            }
        }
        int CountProgressBar(int curValue, int maxValue)
        {
            return (int)(((1.0 * curValue) / maxValue) * 100);
        }

        public List<string> ComboBoxWithPossibleResults { get; set; }


        int _currentProgressBarValue;

        public int ProgressBar
        {
            get { return _currentProgressBarValue; }
            set
            {
                _currentProgressBarValue = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ProgressBar)));
            }
        }

        ObservableCollection<string> _dbStatisticCollection;
        public ObservableCollection<string> DbStatisticCollection
        {
            get { return _dbStatisticCollection; }
            set
            {
                _dbStatisticCollection = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(DbStatisticCollection)));
            }
        }

        ObservableCollection<ReturnMessage> _recognizedImagesCollection;

        public ObservableCollection<ReturnMessage> RecognizedImagesCollection
        {
            get { return _recognizedImagesCollection; }
            set
            {
                _recognizedImagesCollection = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(RecognizedImagesCollection)));
            }
        }

        List<ReturnMessage> _chosenTypeImagesCollection;
        public List<ReturnMessage> ChosenImagesCollection
        {
            get { return _chosenTypeImagesCollection; }
            set
            {
                _chosenTypeImagesCollection = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ChosenImagesCollection)));
            }
        }
        int _comboBoxIndex;

        public int ComboBoxSelectedIndex
        {
            get { return _comboBoxIndex; }
            set
            {
                _comboBoxIndex = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ComboBoxSelectedIndex)));
            }
        }


        //DATABASE
        ImagesLibraryContext imagesLibraryContext;



        public ViewModel(IUIServices uiservices)
        {   lockobj=new object();
            //database
            imagesLibraryContext = new ImagesLibraryContext();
            DbStatisticCollection = null;

            // imagesLibraryContext.AddRecognitionResultToDatabase(new ReturnMessage("/Users/denis/Downloads/ImageSet/res/car.jpg","car"));

            _uiservices = uiservices;
            RecognizedImagesCollection = null;
            _numOfRecognizedImages = 0;

            // СОЗДАЕМ ЭКЗЕМПЛЯР БИБЛИОТЕКИ
            _imgProcClass = new ImageProcClass("");

            //Initialise combo box with various possible predict results
            ComboBoxWithPossibleResults = new List<string>();
            for (int i = 0; i < ImageProcClass.classLabels.Length; i++)
                ComboBoxWithPossibleResults.Add(ImageProcClass.classLabels[i].ToString());

            //Initialise commands
            OpenCommand = new RelayCommand(async _ => await OpenFolderTask());
            CancelCommand = new RelayCommand(_ => InterruptRecognition());

            ResetDatabaseCommand = new RelayCommand(_ => ClearDb());

            PropertyChanged += DisplayComboBoxSelectedCollection;

            _predictionQueue = new PredictionQueue();
            _predictionQueue.Enqueued += PredictionHandler_WPF;//привязали обработку вывода результатов
        }

        void InterruptRecognition()
        {
            _imgProcClass.InterruptTasks();
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateStatistic();
            });
        }

        bool flagLastActionDbClear = false;
        void ClearDb()
        {
            imagesLibraryContext.ResetDatabase();
            flagLastActionDbClear = true;
            DbStatisticCollection = new ObservableCollection<string>();
        }

        // ОТКРЫТИЕ ДИАЛОГА С ВЫБОРОМ ПАПКИ С КАРТИНКАМИ
        async Task OpenFolderTask()
        {

            _dirr = await _uiservices.OpenDialog();

            RecognizedImagesCollection = new ObservableCollection<ReturnMessage>();
            RecognizedImagesCollection.CollectionChanged += RenewComboBoxSelectedCollection;
            AllUniqueRecognitionResults = new List<string>();
            DbStatisticCollection = new ObservableCollection<string>();
            _numOfRecognizedImages = 0;
            ProgressBar = 0;

            if (_reopenFlag == true)
            {
                _imgProcClass = new ImageProcClass("");
                _predictionQueue = new PredictionQueue();
                _predictionQueue.Enqueued += PredictionHandler_WPF;

                _reopenFlag = false;
            }

            if (_dirr != null)
            {
                //count num of all pics in folder
                _numOfAllImages = 0;
                foreach (string var in Directory.GetFiles(_dirr, "*.jpg"))
                    _numOfAllImages++;


                //launch recognition
                _imgProcClass.SetDirr(_dirr);

                //know what files are not in database and need to be processed
                string[] unRecognizedImageFiles = getUnProcessedImageFiles();

                ImageProcClass.filePaths = unRecognizedImageFiles;

                //if we alredy have processed that folder
                if (ImageProcClass.filePaths != null)
                {
                    _reopenFlag = true;
                    await _imgProcClass.StartProc(_predictionQueue);
                }
            }
        }

        // this function will match files in folder with already predicted files and 
        // if file is new -> will process it in lib
        string[] getUnProcessedImageFiles()
        {
            string[] allFilesInFolder = null;
            string[] unProcessedImageFiles = null;
            try
            {
                allFilesInFolder = Directory.GetFiles(_dirr, "*.jpg");
                // Console.WriteLine("filePath[0] " + ImageProcClass.filePaths[0]);
            }
            catch (Exception)
            {
                Console.WriteLine("Next message is from method 'StartProc()' ");
                Console.WriteLine("Looks like you entered incorrect filepath");
                Console.WriteLine("Or there is no jpg images in your folder");
                //Console.WriteLine(e.ToString());
            }
            if (allFilesInFolder != null)
            {
                //if we cleared database last time then process all files
                if (flagLastActionDbClear == true)
                {
                    flagLastActionDbClear = false;
                    return allFilesInFolder;
                }

                unProcessedImageFiles = FilterImagesInDatabase(allFilesInFolder);
            }
            return unProcessedImageFiles;

        }

        string[] FilterImagesInDatabase(string[] allFiles)
        {
            List<string> newFiles = new List<string>();

            ReturnMessage returnMessageFromDB;
            foreach (string file in allFiles)
            {
                // string f = file;
                returnMessageFromDB = imagesLibraryContext.SearchFile(file);
                if (returnMessageFromDB == null)
                //didn't find same file in database
                {
                    newFiles.Add(file);
                }

                else
                //else we found it in database and process it
                {
                    DatabaseFilesHandler(returnMessageFromDB);

                }
            }
            if (newFiles.Count() == 0)
                return null;
            else
            {
                return newFiles.ToArray();
            }
        }




        //flag mentions that we already have predicted some imgs previously
        bool _reopenFlag = false;

        void DisplayComboBoxSelectedCollection(object obj, PropertyChangedEventArgs e)
        {
            if (RecognizedImagesCollection != null)
                if (nameof(ComboBoxSelectedIndex).Equals(e.PropertyName))
                {
                    ChosenImagesCollection = RecognizedImagesCollection.Where(returnMessageResult => returnMessageResult.PredictionStringResult.Equals(ImageProcClass.classLabels[ComboBoxSelectedIndex])).ToList<ReturnMessage>();
                }
        }

        void RenewComboBoxSelectedCollection(object obj, NotifyCollectionChangedEventArgs e)
        {
            ChosenImagesCollection = new List<ReturnMessage>();
        }

        //Adding recognized image to collection
        void UpdateCollection(ReturnMessage rm)
        {
            RecognizedImagesCollection.Add(rm);
            NumOfRecognizedImages += 1;
            // Console.WriteLine(ProgressBar);
            if (ProgressBar >= 99)
            {
                // Console.WriteLine(AllRecognitionResults.Count());
                UpdateStatistic();
            }

        }
        List<string> AllUniqueRecognitionResults;
        void UpdateStatistic()
        {
        //     var q = from x in AllUniqueRecognitionResults
        //             group x by x into g
        //             let count = g.Count()
        //             orderby count descending
        //             select new { Value = g.Key, Count = count };
        //     foreach (var x in q)
        //     {
        //         // Console.WriteLine("Value: " + x.Value + " Count: " + x.Count);
        //         DbStatisticCollection.Add($"Class: {x.Value} Count: {x.Count}");
        //     }
            // foreach(var t in imagesLibraryContext.statList)
            //     DbStatisticCollection.Add($"Class: {t.stringResult} Count: {t.repeatCnt}");

            foreach (var t in AllUniqueRecognitionResults)
                DbStatisticCollection.Add($"Class: {t} Count: {imagesLibraryContext.GetNumOfEachType(t)}" );
            if (PropertyChanged != null)
            {

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(DbStatisticCollection)));
            }
        }

    }



    // ИНТЕРФЕЙС ДЛЯ ВЫБОРА ДИРЕКТОРИИ
    public interface IUIServices
    {
        Task<string> OpenDialog();
    }


}
