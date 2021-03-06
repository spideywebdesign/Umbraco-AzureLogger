﻿namespace Our.Umbraco.AzureLogger.Core
{
    using log4net.Appender;
    using log4net.Core;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using Our.Umbraco.AzureLogger.Core.Services;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    /// <summary>
    /// Custom log4net appender
    /// </summary>
    public class TableAppender : BufferingAppenderSkeleton
    {
        /// <summary>
        /// From configuration setting, this could be the full connection string, or the name of a connection string in the web.config
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// From configuration setting
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// From (optional) configuration setting
        /// </summary>
        public string TreeName { get; set; }

        /// <summary>
        /// From (optional) configuration setting
        /// </summary>
        public string IconName { get; set; }

        /// <summary>
        /// From (optional) configuration setting
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TableAppender()
        {
            this.Lossy = false;
        }

        /// <summary>
        /// attempts to get the actual connection string (whether that was set in the log-4-net config, or the web.config
        /// </summary>
        /// <returns></returns>
        internal string GetConnectionString()
        {
            // attempt to find connection string in web.config
            if (ConfigurationManager.ConnectionStrings[this.ConnectionString] != null)
            {
                return ConfigurationManager.ConnectionStrings[this.ConnectionString].ConnectionString;
            }

            // fallback to assuming tableAppender has the full connection string
            return this.ConnectionString;
        }

        /// <summary>
        /// Gets the Azure table storage object (or null)
        /// </summary>
        /// <returns></returns>
        internal CloudTable GetCloudTable()
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(this.GetConnectionString());

            CloudTableClient cloudTableClient = cloudStorageAccount.CreateCloudTableClient();

            CloudTable cloudTable = cloudTableClient.GetTableReference(this.TableName);

            bool retry;
            do
            {
                retry = false;
                try
                {
                    cloudTable.CreateIfNotExists();
                }
                catch (StorageException exception)
                {
                    if (exception.RequestInformation.HttpStatusCode == 409 &&
                        exception.RequestInformation.ExtendedErrorInformation.ErrorCode.Equals(TableErrorCodeStrings.TableBeingDeleted))
                    {
                        retry = true;
                    }
                }
            } while (retry);

            return cloudTable;
        }

        /// <summary>
        /// Attempts to get the associated azure table, but gives up if it takes longer than 1/2 second
        /// </summary>
        /// <returns>true if the appender can connect to table storage, otherwise false</returns>
        internal bool IsConnected()
        {
            bool isConnected = false;

            Thread thread = new Thread(() => {

                CloudTable cloudTable = this.GetCloudTable();
                isConnected = cloudTable != null && cloudTable.Exists();

            });

            thread.Start();

            if (!thread.Join(500)) { thread.Abort();}

            return isConnected;
        }

        /// <summary>
        /// Append extra logging data to a log item
        /// </summary>
        /// <param name="loggingEvent">the log item to extend</param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (!this.ReadOnly)
            {
                loggingEvent.Properties["url"] = null;
                loggingEvent.Properties["sessionId"] = null;

                try
                {
                    if (HttpContext.Current != null && HttpContext.Current.Handler != null)
                    {
                        if (HttpContext.Current.Request != null)
                        {
                            loggingEvent.Properties["url"] = HttpContext.Current.Request.RawUrl;
                        }

                        if (HttpContext.Current.Session != null)
                        {
                            loggingEvent.Properties["sessionId"] = HttpContext.Current.Session.SessionID;
                        }
                    }
                }
                catch
                {
                    // failsafe as no exceptions should be ever thrown in this method
                }
            }

            base.Append(loggingEvent);
        }

        /// <summary>
        /// log4net calls this to persist the logging events
        /// </summary>
        /// <param name="events">the log events to persist</param>
        protected override void SendBuffer(LoggingEvent[] loggingEvents)
        {
            if (!this.ReadOnly)
            {
                // spin off a new thread to avoid waiting
                Task.Run(() => TableService.Instance.CreateLogTableEntities(this.Name, loggingEvents));
            }
        }
    }
}
