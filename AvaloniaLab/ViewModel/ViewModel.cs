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

namespace ViewModel
{
    public class ViewModel : INotifyPropertyChanged
    {
        // Обрабатываем результаты распознования
        void PredictionHandler_WPF(object sender, PredictionEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ReturnMessage rm = (ReturnMessage)e.RecognitionResult;
                UpdateCollection(rm);
            });
            // Console.WriteLine("Queue " + ( e.RecognitionResult).ToString());
        }

        string _dirr = null;
        public event PropertyChangedEventHandler PropertyChanged;
        IUIServices _uiservices;

        ImageProcClass _imgProcClass;

        PredictionQueue _predictionQueue;

        public ICommand OpenCommand { get; set; }

        public ICommand CancelCommand { get; set; }

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

        public ViewModel(IUIServices uiservices)
        {
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

            PropertyChanged += DisplayComboBoxSelectedCollection;

            _predictionQueue = new PredictionQueue();
            _predictionQueue.Enqueued += PredictionHandler_WPF;//привязали обработку вывода результатов
        }

        void InterruptRecognition()
        {
            _imgProcClass.InterruptTasks();
        }

        // ОТКРЫТИЕ ДИАЛОГА С ВЫБОРОМ ПАПКИ С КАРТИНКАМИ
        async Task OpenFolderTask()
        {

            _dirr = await _uiservices.OpenDialog();

            RecognizedImagesCollection = new ObservableCollection<ReturnMessage>();
            RecognizedImagesCollection.CollectionChanged += RenewComboBoxSelectedCollection;
            _numOfRecognizedImages = 0;
            ProgressBar = 0;

            if (_reopenFlag == true)
            {
                _imgProcClass = new ImageProcClass("");
                _predictionQueue = new PredictionQueue();
                _predictionQueue.Enqueued += PredictionHandler_WPF;
                
                _reopenFlag=false;
            }

            if (_dirr != null)
            {
                //count num of all pics in folder
                _numOfAllImages = 0;
                foreach (string var in Directory.GetFiles(_dirr,"*.jpg"))
                    _numOfAllImages++;
                    

                //launch recognition
                _imgProcClass.SetDirr(_dirr);
                _reopenFlag=true;
                await _imgProcClass.StartProc(_predictionQueue);
            }
        }
        //flag mentions that we already have predicted some imgs previously
        bool _reopenFlag=false;
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
            NumOfRecognizedImages+=1;
        }

    }

    

    // ИНТЕРФЕЙС ДЛЯ ВЫБОРА ДИРЕКТОРИИ
    public interface IUIServices
    {
        Task<string> OpenDialog();
    }


}
