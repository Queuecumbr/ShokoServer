﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Force.DeepCloner;
using NLog;
using Shoko.Commons.Queue;
using Shoko.Models.Queue;
using Shoko.Models.Server;
using Shoko.Server.Repositories;
using Shoko.Server.Settings;

namespace Shoko.Server.Commands
{
    public class CommandProcessorHasher
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly BackgroundWorker workerCommands = new BackgroundWorker();
        private bool processingCommands;
        private DateTime? pauseTime;

        private readonly object lockQueueCount = new object();
        private readonly object lockQueueState = new object();
        private readonly object lockPaused = new object();

        public delegate void QueueCountChangedHandler(QueueCountEventArgs ev);

        public event QueueCountChangedHandler OnQueueCountChangedEvent;

        public delegate void QueueStateChangedHandler(QueueStateEventArgs ev);

        public event QueueStateChangedHandler OnQueueStateChangedEvent;

        private bool paused;

        public bool Paused
        {
            get
            {
                lock (lockPaused)
                {
                    return paused;
                }
            }
            set
            {
                lock (lockPaused)
                {
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(ServerSettings.Instance.Culture);

                    paused = value;
                    if (paused)
                    {
                        QueueState =
                            new QueueStateStruct {queueState = QueueStateEnum.Paused, extraParams = new string[0]};
                        pauseTime = DateTime.Now;
                    }
                    else
                    {
                        QueueState =
                            new QueueStateStruct {queueState = QueueStateEnum.Idle, extraParams = new string[0]};
                        pauseTime = null;
                    }

                    ServerInfo.Instance.HasherQueuePaused = paused;
                    ServerInfo.Instance.HasherQueueRunning = !paused;
                }
            }
        }

        private int queueCount;

        public int QueueCount
        {
            get
            {
                lock (lockQueueCount)
                {
                    return queueCount;
                }
            }
            set
            {
                lock (lockQueueCount)
                {
                    queueCount = value;
                }
                Task.Factory.StartNew(() => OnQueueCountChangedEvent?.Invoke(new QueueCountEventArgs(value)));
            }
        }

        private QueueStateStruct queueState =
            new QueueStateStruct {queueState = QueueStateEnum.Idle, extraParams = new string[0]};

        public QueueStateStruct QueueState
        {
            get
            {
                lock (lockQueueState)
                {
                    return queueState.DeepClone();
                }
            }
            set
            {
                lock (lockQueueState)
                {
                    queueState = value.DeepClone();
                }
                Task.Factory.StartNew(() => OnQueueStateChangedEvent?.Invoke(new QueueStateEventArgs(value)));
            }
        }

        public CommandRequest CurrentCommand { get; private set; }

        public bool ProcessingCommands => processingCommands;

        public bool IsWorkerBusy => workerCommands.IsBusy;

        public CommandProcessorHasher()
        {
            workerCommands.WorkerReportsProgress = true;
            workerCommands.WorkerSupportsCancellation = true;
            workerCommands.DoWork += WorkerCommands_DoWork;
            workerCommands.RunWorkerCompleted += WorkerCommands_RunWorkerCompleted;
        }

        void WorkerCommands_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(ServerSettings.Instance.Culture);

            CurrentCommand = null;
            processingCommands = false;
            paused = false;
            QueueState = new QueueStateStruct {queueState = QueueStateEnum.Idle, extraParams = new string[0]};
            QueueCount = RepoFactory.CommandRequest.GetQueuedCommandCountHasher();

            if (QueueCount > 0 && !workerCommands.IsBusy)
            {
                processingCommands = true;
                workerCommands.RunWorkerAsync();
            }
        }

        public void Init()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(ServerSettings.Instance.Culture);

            processingCommands = true;
            QueueState = new QueueStateStruct
            {
                queueState = QueueStateEnum.StartingHasher,
                extraParams = new string[0]
            };
            if (!workerCommands.IsBusy)
                workerCommands.RunWorkerAsync();
        }

        public void Stop()
        {
            workerCommands.CancelAsync();
        }

        /// <summary>
        /// This is simply used to tell the command processor that a new command has been added to the database
        /// </summary>
        public void NotifyOfNewCommand()
        {
            QueueCount = RepoFactory.CommandRequest.GetQueuedCommandCountHasher();
            // if the worker is busy, it will pick up the next command from the DB
            // do not pick new command if cancellation is requested
            if (processingCommands || workerCommands.CancellationPending)
                return;

            // otherwise need to start the worker again
            processingCommands = true;
            if (!workerCommands.IsBusy)
                workerCommands.RunWorkerAsync();
        }

        void WorkerCommands_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (workerCommands.CancellationPending)
                    return;

                // if paused we will sleep for 5 seconds, and the try again
                if (Paused)
                {
                    try
                    {
                        if (workerCommands.CancellationPending)
                            return;
                    }
                    catch
                    {
                        // ignore
                    }
                    Thread.Sleep(200);
                    continue;
                }

                if (workerCommands.CancellationPending)
                    return;

                CommandRequest crdb = RepoFactory.CommandRequest.GetNextDBCommandRequestHasher();
                if (crdb == null)
                {
                    if (QueueCount > 0)
                        logger.Error($"No command returned from repo, but there are {QueueCount} commands left");
                    return;
                }

                ICommandRequest icr = CommandHelper.GetCommand(crdb);
                
                if (icr == null)
                {
                    logger.Trace("No implementation found for command: {0}-{1}", crdb.CommandType, crdb.CommandID);
                    return;
                }

                QueueState = icr.PrettyDescription;

                if (workerCommands.CancellationPending)
                    return;

                try
                {
                    CurrentCommand = crdb;
                    icr.ProcessCommand();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "ProcessCommand exception: {0}\n{1}", crdb.CommandID, ex);
                    logger.Info(ex, "Removing ProcessCommand: {0}", crdb.CommandID);
                    RepoFactory.CommandRequest.Delete(crdb.CommandRequestID);
                }
                finally
                {
                    CurrentCommand = null;
                }

                RepoFactory.CommandRequest.Delete(crdb.CommandRequestID);
                QueueCount = RepoFactory.CommandRequest.GetQueuedCommandCountHasher();
            }
        }
    }
}
