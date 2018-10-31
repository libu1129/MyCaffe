﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyCaffe.basecode;
using MyCaffe.basecode.descriptors;
using MyCaffe.common;
using MyCaffe.gym;

namespace MyCaffe.trainers
{
    /// <summary>
    /// The MyCaffeTrainerRNN is used to perform recurrent neural-network training tasks on an instance of the MyCaffeControl.
    /// </summary>
    /// <remarks>
    /// Currently, the MyCaffeTrainerRNN supports the following trainers, each of which are selected with the 'TrainerType=type' property
    /// value within the property set specified when calling the Initialize method.
    /// 
    /// TrainerType=RNN.SIMPLE - creates the initial simple policy gradient trainer that only supports single-threaded Sigmoid based models.
    /// 
    /// The following settings are used from the Model and Solver descriptions:
    /// 
    /// Solver: base_lr - specifies the learning rate used.
    /// Model: batch_size - specifies how often accumulated gradients are applied.
    /// </remarks>
    public partial class MyCaffeTrainerRNN : Component, IXMyCaffeCustomTrainerRNN, IxTrainerCallback
    {
        /// <summary>
        /// Specifies the properties parsed from the key-value pair passed to the Initialize method.
        /// </summary>
        protected PropertySet m_properties = null;
        /// <summary>
        /// Specifies the project ID of the project held by the instance of MyCaffe.
        /// </summary>
        protected int m_nProjectID = 0;
        IxTrainerRNN m_itrainer = null;
        TRAINER_TYPE m_trainerType = TRAINER_TYPE.RNN_SIMPLE;
        IXMyCaffeCustomTrainerCallback m_icallback = null;
        CryptoRandom m_random = new CryptoRandom();
        int m_nSnapshot = 0;
        bool m_bSnapshot = false;
        double m_dfLoss = 0;
        double m_dfAccuracy = 0;
        int m_nIteration = 0;
        int m_nIterations = -1;

        enum TRAINER_TYPE
        {
            RNN_SIMPLE
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        public MyCaffeTrainerRNN()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="container">The container of the component.</param>
        public MyCaffeTrainerRNN(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        #region Overrides

        /// <summary>
        /// Overriden to give the actual name of the custom trainer.
        /// </summary>
        protected virtual string name
        {
            get { return "MyCaffe RNN Trainer"; }
        }

        /// <summary>
        /// Override when using a training method other than the RECURRENT method (the default).
        /// </summary>
        protected virtual TRAINING_CATEGORY category
        {
            get { return TRAINING_CATEGORY.RECURRENT; }
        }

        /// <summary>
        /// Returns a dataset override to use (if any) instead of the project's dataset.  If there is no dataset override
        /// <i>null</i> is returned and the project's dataset is used.
        /// </summary>
        /// <param name="nProjectID">Specifies the project ID associated with the trainer (if any)</param>
        protected virtual DatasetDescriptor get_dataset_override(int nProjectID)
        {
            return null;
        }

        /// <summary>
        /// Returns information describing the specific trainer, such as the gym used, if any.
        /// </summary>
        /// <returns>The string describing the trainer is returned.</returns>
        protected virtual string get_information()
        {
            return "";
        }

        /// <summary>
        /// Optionally overridden to return a new type of trainer.
        /// </summary>
        /// <remarks>
        /// Override this method when using the MyCaffeControl that uses the <i>double</i> base type.
        /// </remarks>
        /// <param name="caffe">Specifies the MyCaffeControl used.</param>
        /// <returns>The IxTraininer interface implemented by the new trainer is returned.</returns>
        protected virtual IxTrainerRNN create_trainerD(Component caffe)
        {
            MyCaffeControl<double> mycaffe = caffe as MyCaffeControl<double>;
            m_nProjectID = mycaffe.CurrentProject.ID;
            int.TryParse(mycaffe.CurrentProject.GetSolverSetting("max_iter"), out m_nIterations);
            int.TryParse(mycaffe.CurrentProject.GetSolverSetting("snapshot"), out m_nSnapshot);

            switch (m_trainerType)
            {
                case TRAINER_TYPE.RNN_SIMPLE:
                    return new rnn.simple.TrainerRNN<double>(mycaffe, m_properties, m_random, this);

                default:
                    throw new Exception("Unknown trainer type '" + m_trainerType.ToString() + "'!");
            }
        }

        /// <summary>
        /// Optionally overridden to return a new type of trainer.
        /// </summary>
        /// <remarks>
        /// Override this method when using the MyCaffeControl that uses the <i>double</i> base type.
        /// </remarks>
        /// <param name="caffe">Specifies the MyCaffeControl used.</param>
        /// <returns>The IxTraininer interface implemented by the new trainer is returned.</returns>
        protected virtual IxTrainerRNN create_trainerF(Component caffe)
        {
            MyCaffeControl<float> mycaffe = caffe as MyCaffeControl<float>;
            m_nProjectID = mycaffe.CurrentProject.ID;
            int.TryParse(mycaffe.CurrentProject.GetSolverSetting("max_iter"), out m_nIterations);
            int.TryParse(mycaffe.CurrentProject.GetSolverSetting("snapshot"), out m_nSnapshot);

            switch (m_trainerType)
            {
                case TRAINER_TYPE.RNN_SIMPLE:
                    return new rnn.simple.TrainerRNN<float>(mycaffe, m_properties, m_random, this);

                default:
                    throw new Exception("Unknown trainer type '" + m_trainerType.ToString() + "'!");
            }
        }

        /// <summary>
        /// Override to dispose of resources used.
        /// </summary>
        protected virtual void dispose()
        {
        }

        /// <summary>
        /// Override called by the Initialize method of the trainer.
        /// </summary>
        /// <remarks>
        /// When providing a new trainer, this method is not used.
        /// </remarks>
        /// <param name="e">Specifies the initialization arguments.</param>
        protected virtual void initialize(InitializeArgs e)
        {
        }

        /// <summary>
        /// Override called from within the CleanUp method.
        /// </summary>
        protected virtual void shutdown()
        {
        }

        /// <summary>
        /// Override called by the OnGetData event fired by the Trainer to retrieve a new set of observation collections making up a set of experiences.
        /// </summary>
        /// <param name="e">Specifies the getData argments used to return the new observations.</param>
        /// <returns>A value of <i>true</i> is returned when data is retrieved.</returns>
        protected virtual bool getData(GetDataArgs e)
        {
            return false;
        }

        /// <summary>
        /// Returns <i>true</i> when the training is ready for a snap-shot, <i>false</i> otherwise.
        /// </summary>
        /// <param name="nIteration">Specifies the current iteration.</param>
        /// <param name="dfAccuracy">Returns the current accuracy.</param>
        protected virtual bool get_update_snapshot(out int nIteration, out double dfAccuracy)
        {
            nIteration = (int)GetProperty("GlobalIteration");
            dfAccuracy = GetProperty("GlobalAccuracy");

            if (m_bSnapshot)
            {
                m_bSnapshot = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called by OpenUi, override this when a UI (via WCF) should be displayed.
        /// </summary>
        protected virtual void openUi()
        {
        }

        #endregion

        #region IXMyCaffeCustomTrainer Interface

        /// <summary>
        /// Returns the name of the custom trainer.  This method calls the 'name' override.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Returns the training category of the custom trainer (default = REINFORCEMENT).
        /// </summary>
        public TRAINING_CATEGORY TrainingCategory
        {
            get { return category; }
        }

        /// <summary>
        /// Returns <i>true</i> when the training is ready for a snap-shot, <i>false</i> otherwise.
        /// </summary>
        /// <param name="nIteration">Specifies the current iteration.</param>
        /// <param name="dfAccuracy">Specifies the current accuracy.</param>
        public bool GetUpdateSnapshot(out int nIteration, out double dfAccuracy)
        {
            return get_update_snapshot(out nIteration, out dfAccuracy);
        }

        /// <summary>
        /// Returns a dataset override to use (if any) instead of the project's dataset.  If there is no dataset override
        /// <i>null</i> is returned and the project's dataset is used.
        /// </summary>
        /// <param name="nProjectID">Specifies the project ID associated with the trainer (if any)</param>
        public DatasetDescriptor GetDatasetOverride(int nProjectID)
        {
            return get_dataset_override(nProjectID);
        }

        /// <summary>
        /// Returns whether or not Training is supported.
        /// </summary>
        public bool IsTrainingSupported
        {
            get { return true; }
        }

        /// <summary>
        /// Returns whether or not Testing is supported.
        /// </summary>
        public bool IsTestingSupported
        {
            get { return true; }
        }

        /// <summary>
        /// Returns whether or not Running is supported.
        /// </summary>
        public bool IsRunningSupported
        {
            get { return true; }
        }

        /// <summary>
        /// Releases any resources used by the component.
        /// </summary>
        public void CleanUp()
        {
            if (m_itrainer != null)
            {
                m_itrainer.Shutdown(3000);
                m_itrainer = null;
            }

            shutdown();
        }

        /// <summary>
        /// Initializes a new custom trainer by loading the key-value pair of properties into the property set.
        /// </summary>
        /// <param name="strProperties">Specifies the key-value pair of properties each separated by ';'.  For example the expected
        /// format is 'key1'='value1';'key2'='value2';...</param>
        /// <param name="icallback">Specifies the parent callback.</param>
        public void Initialize(string strProperties, IXMyCaffeCustomTrainerCallback icallback)
        {
            m_icallback = icallback;
            m_properties = new PropertySet(strProperties);

            string strTrainerType = m_properties.GetProperty("TrainerType");

            switch (strTrainerType)
            {
                case "RNN.SIMPLE":   // bare bones model
                    m_trainerType = TRAINER_TYPE.RNN_SIMPLE;
                    break;

                default:
                    throw new Exception("Unknown trainer type '" + strTrainerType + "'!");
            }
        }

        private IxTrainerRNN createTrainer(Component mycaffe)
        {
            IxTrainerRNN itrainer = null;

            if (mycaffe is MyCaffeControl<double>)
                itrainer = create_trainerD(mycaffe);
            else
                itrainer = create_trainerF(mycaffe);

            itrainer.Initialize();

            return itrainer;
        }

        /// <summary>
        /// Create a new trainer and use it to run a single run cycle.
        /// </summary>
        /// <param name="mycaffe">Specifies the MyCaffeControl to use.</param>
        /// <param name="nN">specifies the number of samples to run.</param>
        /// <returns>The results of the run are returned.</returns>
        public float[] Run(Component mycaffe, int nN)
        {
            if (m_itrainer == null)
                m_itrainer = createTrainer(mycaffe);

            float[] rgResults = m_itrainer.Run(nN);
            m_itrainer.Shutdown(0);
            m_itrainer = null;

            return rgResults;
        }

        /// <summary>
        /// Create a new trainer and use it to run a test cycle.
        /// </summary>
        /// <param name="mycaffe">Specifies the MyCaffeControl to use.</param>
        /// <param name="nIterationOverride">Specifies the iterations to run if greater than zero.</param>
        public void Test(Component mycaffe, int nIterationOverride)
        {
            if (m_itrainer == null)
                m_itrainer = createTrainer(mycaffe);

            if (nIterationOverride == -1)
                nIterationOverride = m_nIterations;

            m_itrainer.Test(nIterationOverride);
            m_itrainer.Shutdown(0);
            m_itrainer = null;
        }

        /// <summary>
        /// Create a new trainer and use it to run a training cycle.
        /// </summary>
        /// <param name="mycaffe">Specifies the MyCaffeControl to use.</param>
        /// <param name="nIterationOverride">Specifies the iterations to run if greater than zero.</param>
        /// <param name="step">Optionally, specifies whether or not to step the training for debugging (default = NONE).</param>
        public void Train(Component mycaffe, int nIterationOverride, TRAIN_STEP step = TRAIN_STEP.NONE)
        {
            if (m_itrainer == null)
                m_itrainer = createTrainer(mycaffe);

            if (nIterationOverride == -1)
                nIterationOverride = m_nIterations;

            m_itrainer.Train(nIterationOverride, step);
            m_itrainer.Shutdown(0);
            m_itrainer = null;
        }

        #endregion

        /// <summary>
        /// The OnIntialize callback fires when initializing the trainer.
        /// </summary>
        public void OnInitialize(InitializeArgs e)
        {
            initialize(e);
        }

        /// <summary>
        /// The OnShutdown callback fires when shutting down the trainer.
        /// </summary>
        public void OnShutdown()
        {
            shutdown();
        }

        /// <summary>
        /// The OnGetData callback fires from within the Train method and is used to get a new observation data.
        /// </summary>
        public void OnGetData(GetDataArgs e)
        {
            getData(e);
        }

        /// <summary>
        /// The OnGetStatus callback fires on each iteration within the Train method.
        /// </summary>
        public void OnUpdateStatus(GetStatusArgs e)
        {
            if (m_icallback != null)
            {
                Dictionary<string, double> rgValues = new Dictionary<string, double>();
                rgValues.Add("GlobalIteration", m_nIteration);
                rgValues.Add("GlobalLoss", m_dfLoss);
                rgValues.Add("LearningRate", e.LearningRate);
                rgValues.Add("GlobalAccuracy", m_dfAccuracy);
                m_icallback.Update(TrainingCategory, rgValues);
            }
        }

        /// <summary>
        /// The OnWait callback fires when waiting for a shutdown.
        /// </summary>
        public void OnWait(WaitArgs e)
        {
            Thread.Sleep(e.Wait);
        }

        /// <summary>
        /// Returns a specific property value.
        /// </summary>
        /// <param name="strProp">Specifies the property to get.</param>
        /// <returns>The property value is returned.</returns>
        /// <remarks>
        /// The following properties are supported by the RNN trainers:
        ///     'GlobalIteration'
        /// </remarks>
        public double GetProperty(string strProp)
        {
            switch (strProp)
            {
                case "GlobalLoss":
                    return m_dfLoss;

                case "GlobalAccuracy":
                    return m_dfAccuracy;

                case "GlobalIteration":
                    return m_nIteration;

                case "GlobalMaxIterations":
                    return m_nIterations;

                default:
                    throw new Exception("The property '" + strProp + "' is not supported by the MyCaffeTrainerRNN.");
            }
        }

        /// <summary>
        /// Returns information describing the trainer.
        /// </summary>
        public string Information
        {
            get { return get_information(); }
        }

        /// <summary>
        /// Open the user interface for the trainer, of one exists.
        /// </summary>
        public void OpenUi()
        {
            openUi();
        }
    }
}