using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ImgProcLib
{
    public class PredictionEventArgs : EventArgs
    {
        private ReturnMessage _recognitionResult;
        public ReturnMessage RecognitionResult { get { return _recognitionResult; } }
        public PredictionEventArgs(ReturnMessage predictionResult)
        {
            _recognitionResult = predictionResult;
        }

    }
    public class PredictionQueue
    {
        private readonly ConcurrentQueue<ReturnMessage> queue = new ConcurrentQueue<ReturnMessage>();
            public event EventHandler<PredictionEventArgs> Enqueued;
            protected virtual void OnEnqueued(PredictionEventArgs e)
            {
                if (Enqueued != null)
                    Enqueued(this, e);
            }
            public virtual void Enqueue(ReturnMessage item)
            {
                queue.Enqueue(item);
                OnEnqueued(new PredictionEventArgs(item));
            }
            public virtual ReturnMessage TryDequeue()
            {
                ReturnMessage item; 
                queue.TryDequeue(out item);
                return item;
            }
    }
}