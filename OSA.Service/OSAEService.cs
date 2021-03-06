﻿namespace OSAE.Service
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.ServiceModel;
    using System.ServiceProcess;
    using MySql.Data.MySqlClient;

    /// <summary>
    /// The primary server used in the OSA infrastructure to process information
    /// </summary>
    partial class OSAEService : ServiceBase
    {
        /// <summary>
        /// Used when generating messages to identify where the message came from
        /// </summary>
        private const string sourceName = "OSAE Service";
              
        private ServiceHost sHost;
        private WCF.WCFService wcfService;
        private List<Plugin> plugins = new List<Plugin>();
        private List<Plugin> masterPlugins = new List<Plugin>();

        /// <summary>
        /// The IP address of where the OSA DB & primary service run
        /// </summary>
        private string computerIP;
        private bool goodConnection = false;

        /// <summary>
        /// Provides access to logging
        /// </summary>
        Logging logging = Logging.GetLogger(sourceName);

        private bool running = true;
        
        private System.Timers.Timer timer = new System.Timers.Timer();
        private System.Timers.Timer updates = new System.Timers.Timer();

        /// <summary>
        /// Timer used to periodically check if plugins are still running
        /// </summary>
        private System.Timers.Timer checkPlugins = new System.Timers.Timer();

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main(string[] args) 
        {          
            if (args.Length > 0)
            {
                string pattern = Common.MatchPattern(args[0]);
                Logging.AddToLog("Processing command: " + args[0] + ", Named Script: " + pattern, true, "OSACL");
                if (pattern != string.Empty)
                {
                    OSAEMethodManager.MethodQueueAdd("Script Processor", "NAMED SCRIPT", pattern, "", "OSACL");
                }
            }
            else
            {
                ServiceBase.Run(new OSAEService());
            }
            
        }
        
        /// <summary>
        /// Public Constructor for WindowsService.
        /// - Put all of your Initialization code here.
        /// </summary>
        public OSAEService()
        {
            logging.AddToLog("Service Starting", true);

            InitialiseOSAInEventLog();

            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            
            this.CanStop = true;
            this.CanShutdown = true;
        }        

        #region Service Start/Stop Processing
        /// <summary>
        /// OnStart: Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
//#if (DEBUG)
//            Debugger.Launch(); //<-- Simple form to debug a web services 
//#endif

            try
            {
                computerIP = GetComputerIP();
                InitialiseLogFolder();
                DeleteStoreFiles();
            }
            catch (Exception ex)
            {
                logging.AddToLog("Error getting registry settings and/or deleting logs: " + ex.Message, true);
            }

            logging.AddToLog("OnStart", true);
            
            RemoveOrphanedMethods();
            CreateComputerObject();
            CreateServiceObject();

            // Start the WCF service so messages can be sent 
            // and received by the service
            StartWCFService();

            // Start the threads that monitor the plugin 
            // updates check the method queue and so on
            StartThreads();
        }                            

        /// <summary>
        /// OnStop: Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            ShutDownSystems();
        }        

        protected override void OnShutdown() 
        {
            ShutDownSystems();
        }
        #endregion       
    }
}
