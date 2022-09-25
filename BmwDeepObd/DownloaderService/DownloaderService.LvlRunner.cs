// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderService.LvlRunner.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The downloader service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

using Android.App;
using Android.Content.PM;
using Android.Provider;
using Android.Util;
using Google.Android.Vending.Expansion.Downloader;
using Google.Android.Vending.Licensing;
using Java.Lang;

namespace BmwDeepObd
{
    /// <summary>
    /// The downloader service.
    /// </summary>
    public abstract partial class CustomDownloaderService
    {
        /// <summary>
        /// The lvl runnable.
        /// </summary>
        private class LvlRunnable : Object, IRunnable
        {
            #region Fields

            /// <summary>
            /// The context.
            /// </summary>
            private readonly CustomDownloaderService context;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="LvlRunnable"/> class.
            /// </summary>
            /// <param name="context">
            /// The context.
            /// </param>
            /// <param name="intent">
            /// The intent.
            /// </param>
            internal LvlRunnable(CustomDownloaderService context, PendingIntent intent)
            {
                Log.Info(Tag, "DownloaderService.LvlRunnable.ctor");
                this.context = context;
                this.context.pPendingIntent = intent;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The run.
            /// </summary>
            public void Run()
            {
                this.context.IsServiceRunning = true;
                this.context.downloadNotification.OnDownloadStateChanged(DownloaderClientState.FetchingUrl);
                string deviceId = Settings.Secure.GetString(this.context.ContentResolver, Settings.Secure.AndroidId);

                var aep = new APKExpansionPolicy(
                    this.context, new AESObfuscator(this.context.GetSalt(), this.context.PackageName, deviceId));

                // reset our policy back to the start of the world to force a re-check
                aep.ResetPolicy();

                // let's try and get the OBB file from LVL first
                // Construct the LicenseChecker with a IPolicy.
                var checker = new LicenseChecker(this.context, aep, this.context.PublicKey);
                checker.CheckAccess(new ApkLicenseCheckerCallback(this, aep));
            }

            #endregion

            /// <summary>
            /// The apk license checker callback.
            /// </summary>
            private class ApkLicenseCheckerCallback : Object, ILicenseCheckerCallback
            {
                #region Fields

                /// <summary>
                /// The lvl runnable.
                /// </summary>
                private readonly LvlRunnable lvlRunnable;

                /// <summary>
                /// The policy.
                /// </summary>
                private readonly APKExpansionPolicy policy;

                #endregion

                #region Constructors and Destructors

                /// <summary>
                /// Initializes a new instance of the <see cref="ApkLicenseCheckerCallback"/> class.
                /// </summary>
                /// <param name="lvlRunnable">
                /// The lvl runnable.
                /// </param>
                /// <param name="policy">
                /// The policy.
                /// </param>
                public ApkLicenseCheckerCallback(LvlRunnable lvlRunnable, APKExpansionPolicy policy)
                {
                    this.lvlRunnable = lvlRunnable;
                    this.policy = policy;
                }

                #endregion

                #region Properties

                /// <summary>
                /// Gets Context.
                /// </summary>
                private CustomDownloaderService Context
                {
                    get
                    {
                        return this.lvlRunnable.context;
                    }
                }

                #endregion

                #region Public Methods and Operators

                /// <summary>
                /// The allow.
                /// </summary>
                /// <param name="reason">
                /// The reason.
                /// </param>
                /// <exception cref="Java.Lang.RuntimeException">
                /// Error with LVL checking and database integrity
                /// </exception>
                /// <exception cref="Java.Lang.RuntimeException">
                /// Error with getting information from package name
                /// </exception>
                public void Allow(PolicyResponse reason)
                {
                    try
                    {
                        int count = this.policy.ExpansionURLCount;
                        DownloadsDB db = DownloadsDB.GetDB(Context);
                        if (count == 0)
                        {
                            Log.Info(Tag, "No expansion packs.");
                        }

                        DownloaderServiceStatus status = 0;
                        for (int index = 0; index < count; index++)
                        {
                            string currentFileName = this.policy.GetExpansionFileName(index);
                            if (currentFileName != null)
                            {
                                DownloadInfo di = new DownloadInfo(index, currentFileName, Context.PackageName );
                                long fileSize = this.policy.GetExpansionFileSize(index);
                                string expansionUrl = this.policy.GetExpansionURL(index);
                                if (this.Context.HandleFileUpdated(db, index, currentFileName, fileSize))
                                {
                                    status = (DownloaderServiceStatus)(-1);
                                    di.ResetDownload();
                                    di.Uri = expansionUrl;
                                    di.TotalBytes = fileSize;
                                    di.Status = (DownloadStatus) status;
                                    db.UpdateDownload(di);
                                }
                                else
                                {
                                    // we need to read the download information from the database
                                    DownloadInfo dbdi = GetDownloadInfoByFileName(db, di.FileName);
                                    if (dbdi == null)
                                    {
                                        // the file exists already and is the correct size
                                        // was delivered by Market or through another mechanism
                                        Log.Info(Tag, string.Format("file {0} found. Not downloading.", di.FileName));
                                        di.Status = DownloadStatus.Successful;
                                        di.TotalBytes = fileSize;
                                        di.CurrentBytes = fileSize;
                                        di.Uri = expansionUrl;
                                        db.UpdateDownload(di);
                                    }
                                    else if (dbdi.Status != DownloadStatus.Successful)
                                    {
                                        // we just update the URL
                                        dbdi.Uri = expansionUrl;
                                        db.UpdateDownload(dbdi);
                                        status = (DownloaderServiceStatus) (-1);
                                    }
                                }
                            }
                        }

                        // first: do we need to do an LVL update?
                        // we begin by getting our APK version from the package manager
                        try
                        {
                            PackageInfo pi = this.Context.PackageManager.GetPackageInfo(this.Context.PackageName, 0);
                            db.UpdateMetadata(pi.VersionCode, status);
                            DownloaderServiceRequirement required = StartDownloadServiceIfRequired(this.Context, this.Context.pPendingIntent, this.Context.GetType());
                            switch (required)
                            {
                                case DownloaderServiceRequirement.NoDownloadRequired:
                                    this.Context.downloadNotification.OnDownloadStateChanged(DownloaderClientState.Completed);
                                    break;

                                case DownloaderServiceRequirement.LvlCheckRequired: // DANGER WILL ROBINSON!
                                    Log.Error(Tag, "In LVL checking loop!");
                                    this.Context.downloadNotification.OnDownloadStateChanged(DownloaderClientState.FailedUnlicensed);
                                    throw new RuntimeException("Error with LVL checking and database integrity");

                                case DownloaderServiceRequirement.DownloadRequired:
                                    // do nothing: the download will notify the application when things are done
                                    break;
                            }
                        }
                        catch (PackageManager.NameNotFoundException e1)
                        {
                            e1.PrintStackTrace();
                            throw new RuntimeException("Error with getting information from package name");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(Tag, string.Format("LVL Update Exception: {0}", ex.Message));
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Tag, string.Format("Allow Exception: {0}", ex.Message));
                        throw;
                    }
                    finally
                    {
                        this.Context.IsServiceRunning = false;
                    }
                }

                /// <summary>
                /// The application error.
                /// </summary>
                /// <param name="errorCode">
                /// The error code.
                /// </param>
                public void ApplicationError(LicenseCheckerErrorCode errorCode)
                {
                    try
                    {
                        this.Context.downloadNotification.OnDownloadStateChanged(DownloaderClientState.FailedFetchingUrl);
                    }
                    finally
                    {
                        this.Context.IsServiceRunning = false;
                    }
                }

                /// <summary>
                /// The dont allow.
                /// </summary>
                /// <param name="reason">
                /// The reason.
                /// </param>
                public void DontAllow(PolicyResponse reason)
                {
                    try
                    {
                        switch (reason)
                        {
                            case PolicyResponse.NotLicensed:
                                this.Context.downloadNotification.OnDownloadStateChanged(DownloaderClientState.FailedUnlicensed);
                                break;
                            case PolicyResponse.Retry:
                                this.Context.downloadNotification.OnDownloadStateChanged(DownloaderClientState.FailedFetchingUrl);
                                break;
                        }
                    }
                    finally
                    {
                        this.Context.IsServiceRunning = false;
                    }
                }

                #endregion
            }
        }
    }
}