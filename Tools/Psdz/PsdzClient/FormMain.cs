﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BMW.Rheingold.Psdz;
using BMW.Rheingold.Psdz.Client;
using BMW.Rheingold.Psdz.Model;
using BMW.Rheingold.Psdz.Model.Ecu;
using BMW.Rheingold.Psdz.Model.SecurityManagement;
using BMW.Rheingold.Psdz.Model.Sfa;
using BMW.Rheingold.Psdz.Model.Svb;
using BMW.Rheingold.Psdz.Model.Swt;
using BMW.Rheingold.Psdz.Model.Tal;
using BMW.Rheingold.Psdz.Model.Tal.TalFilter;
using PsdzClient.Programming;

namespace PsdzClient
{
    public partial class FormMain : Form
    {
        private const string Baureihe = "G31";
        private ProgrammingService programmingService;
        private bool taskActive = false;
        private IPsdzConnection activePsdzConnection;
        private PsdzContext psdzContext;

        public FormMain()
        {
            InitializeComponent();
        }

        private void UpdateDisplay()
        {
            bool active = taskActive;
            bool hostRunning = false;
            bool vehicleConnected = false;
            if (!active)
            {
                hostRunning = programmingService != null && programmingService.IsPsdzPsdzServiceHostInitialized();
            }

            if (activePsdzConnection != null)
            {
                vehicleConnected = true;
            }

            textBoxIstaFolder.Enabled = !active && !hostRunning;
            ipAddressControlVehicleIp.Enabled = !active && !vehicleConnected;
            buttonStartHost.Enabled = !active && !hostRunning;
            buttonStopHost.Enabled = !active && hostRunning;
            buttonConnect.Enabled = !active && hostRunning && !vehicleConnected;
            buttonDisconnect.Enabled = !active && hostRunning && vehicleConnected;
            buttonFunc1.Enabled = !active && hostRunning && vehicleConnected;
            buttonFunc2.Enabled = buttonFunc1.Enabled;
            buttonClose.Enabled = !active;
            buttonAbort.Enabled = active;
        }

        private bool LoadSettings()
        {
            try
            {
                textBoxIstaFolder.Text = Properties.Settings.Default.IstaFolder;
                ipAddressControlVehicleIp.Text = Properties.Settings.Default.VehicleIp;
                if (string.IsNullOrWhiteSpace(ipAddressControlVehicleIp.Text.Trim('.')))
                {
                    ipAddressControlVehicleIp.Text = @"127.0.0.1";
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool StoreSettings()
        {
            try
            {
                Properties.Settings.Default.IstaFolder = textBoxIstaFolder.Text;
                Properties.Settings.Default.VehicleIp = ipAddressControlVehicleIp.Text;
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void UpdateStatus(string message = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    UpdateStatus(message);
                }));
                return;
            }

            textBoxStatus.Text = message ?? string.Empty;
            textBoxStatus.SelectionStart = textBoxStatus.TextLength;
            textBoxStatus.Update();
            textBoxStatus.ScrollToCaret();

            UpdateDisplay();
        }

        private async Task<bool> StartProgrammingServiceTask(string dealerId)
        {
            return await Task.Run(() => StartProgrammingService(dealerId)).ConfigureAwait(false);
        }

        private bool StartProgrammingService(string dealerId)
        {
            try
            {
                if (!StopProgrammingService())
                {
                    return false;
                }
                programmingService = new ProgrammingService(textBoxIstaFolder.Text, dealerId);
                programmingService.PsdzLoglevel = PsdzLoglevel.TRACE;
                programmingService.ProdiasLoglevel = ProdiasLoglevel.TRACE;
                if (!programmingService.StartPsdzServiceHost())
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private async Task<bool> StopProgrammingServiceTask()
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => StopProgrammingService()).ConfigureAwait(false);
        }

        private bool StopProgrammingService()
        {
            try
            {
                if (programmingService != null)
                {
                    programmingService.Psdz.Shutdown();
                    programmingService.CloseConnectionsToPsdzHost();
                    programmingService.Dispose();
                    programmingService = null;
                    ClearProgrammingObjects();
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private async Task<IPsdzConnection> ConnectVehicleTask(string istaFolder, string url, string baureihe)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => ConnectVehicle(istaFolder, url, baureihe)).ConfigureAwait(false);
        }

        private IPsdzConnection ConnectVehicle(string istaFolder, string url, string baureihe)
        {
            try
            {
                if (programmingService == null)
                {
                    return null;
                }

                if (!InitProgrammingObjects(istaFolder))
                {
                    return null;
                }

                string verbund = programmingService.Psdz.ConfigurationService.RequestBaureihenverbund(baureihe);
                IEnumerable<IPsdzTargetSelector> targetSelectors = programmingService.Psdz.ConnectionFactoryService.GetTargetSelectors();
                IPsdzTargetSelector targetSelectorMatch = null;
                foreach (IPsdzTargetSelector targetSelector in targetSelectors)
                {
                    if (!targetSelector.IsDirect &&
                        string.Compare(verbund, targetSelector.Baureihenverbund, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        targetSelectorMatch = targetSelector;
                    }
                }

                if (targetSelectorMatch == null)
                {
                    return null;
                }

                IPsdzConnection psdzConnection = programmingService.Psdz.ConnectionManagerService.ConnectOverEthernet(targetSelectorMatch.Project, targetSelectorMatch.VehicleInfo, url, baureihe, "S15A-17-03-509");
                return psdzConnection;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<bool> DisconnectVehicleTask(IPsdzConnection psdzConnection)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => DisconnectVehicle(psdzConnection)).ConfigureAwait(false);
        }

        private bool DisconnectVehicle(IPsdzConnection psdzConnection)
        {
            try
            {
                if (programmingService == null)
                {
                    return false;
                }

                if (psdzConnection == null)
                {
                    return false;
                }

                programmingService.Psdz.ConnectionManagerService.CloseConnection(psdzConnection);

                ClearProgrammingObjects();
            }
            catch (Exception)
            {
                return false;
            }

            psdzConnection = null;
            return true;
        }

        private async Task<bool> VehicleFunctionsTask(IPsdzConnection psdzConnection, int function)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => VehicleFunctions(psdzConnection, function)).ConfigureAwait(false);
        }

        private bool VehicleFunctions(IPsdzConnection psdzConnection, int function)
        {
            StringBuilder sbResult = new StringBuilder();

            try
            {
                if (programmingService == null)
                {
                    return false;
                }

                switch (function)
                {
                    case 0:
                    default:
                    {
                        psdzContext.CleanupBackupData();
                        IPsdzIstufenTriple iStufenTriple = programmingService.Psdz.VcmService.GetIStufenTripleActual(psdzConnection);
                        psdzContext.SetIstufen(iStufenTriple);
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "IStep: Current={0}, Last={1}, Shipment={2}",
                            iStufenTriple.Current, iStufenTriple.Last, iStufenTriple.Shipment));
                        IPsdzVin psdzVin = programmingService.Psdz.VcmService.GetVinFromMaster(psdzConnection);
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Vin: {0}", psdzVin.Value));
                        UpdateStatus(sbResult.ToString());
                        if (!psdzContext.SetPathToBackupData(psdzVin.Value))
                        {
                            sbResult.AppendLine("Create backup path failed");
                            return false;
                        }

                        IPsdzStandardFa standardFa = programmingService.Psdz.VcmService.GetStandardFaActual(psdzConnection);
                        IPsdzFa psdzFa = programmingService.Psdz.ObjectBuilder.BuildFa(standardFa, psdzVin.Value);
                        psdzContext.SetFaActual(psdzFa);
                        sbResult.AppendLine("FA:");
                        sbResult.Append(psdzFa.AsXml);
                        UpdateStatus(sbResult.ToString());

                        IEnumerable<IPsdzIstufe> psdzIstufes = programmingService.Psdz.LogicService.GetPossibleIntegrationLevel(psdzContext.FaActual);
                        psdzContext.SetPossibleIstufenTarget(psdzIstufes);
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "ISteps: {0}", psdzIstufes.Count()));
                        IPsdzIstufe psdzIstufeTarget = null;
                        foreach (IPsdzIstufe iStufe in psdzIstufes.OrderBy(x => x))
                        {
                            if (iStufe.IsValid)
                            {
                                sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " IStep: {0}", iStufe.Value));
                                psdzIstufeTarget = iStufe;
                            }
                        }
                        UpdateStatus(sbResult.ToString());

                        if (psdzIstufeTarget == null)
                        {
                            sbResult.AppendLine("No target iStep");
                            return false;
                        }

                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "IStep Target: {0}", psdzIstufeTarget.Value));

                        IPsdzIstufe psdzIstufeShip = new PsdzIstufe
                        {
                            Value = psdzContext.IstufeShipment,
                            IsValid = true
                        };
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "IStep Ship: {0}", psdzIstufeShip.Value));
                        UpdateStatus(sbResult.ToString());

                        IEnumerable<IPsdzEcuIdentifier> psdzEcuIdentifiers = programmingService.Psdz.MacrosService.GetInstalledEcuList(psdzContext.FaActual, psdzIstufeShip);
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "EcuIds: {0}", psdzEcuIdentifiers.Count()));
                        foreach (IPsdzEcuIdentifier ecuIdentifier in psdzEcuIdentifiers)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " EcuId: BaseVar={0}, DiagAddr={1}, DiagOffset={2}",
                                ecuIdentifier.BaseVariant, ecuIdentifier.DiagAddrAsInt, ecuIdentifier.DiagnosisAddress.Offset));
                        }
                        UpdateStatus(sbResult.ToString());

                        IPsdzStandardSvt psdzStandardSvt = programmingService.Psdz.EcuService.RequestSvt(psdzConnection, psdzEcuIdentifiers);
                        IPsdzStandardSvt psdzStandardSvtNames = programmingService.Psdz.LogicService.FillBntnNamesForMainSeries(psdzConnection.TargetSelector.Baureihenverbund, psdzStandardSvt);
                        string svtString = psdzStandardSvtNames.AsString.Replace(", ECU[", ",\r\nECU[");
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Svt: {0}", svtString));
                        IPsdzSvt psdzSvt = programmingService.Psdz.ObjectBuilder.BuildSvt(psdzStandardSvtNames, psdzVin.Value);
                        psdzContext.SetSvtActual(psdzSvt);
                        UpdateStatus(sbResult.ToString());

                        IPsdzReadEcuUidResultCto psdzReadEcuUid = programmingService.Psdz.SecurityManagementService.readEcuUid(psdzConnection, psdzEcuIdentifiers, psdzContext.SvtActual);

                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "EcuUids: {0}", psdzReadEcuUid.EcuUids.Count));
                        foreach (KeyValuePair<IPsdzEcuIdentifier, IPsdzEcuUidCto> ecuUid in psdzReadEcuUid.EcuUids)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " EcuId: BaseVar={0}, DiagAddr={1}, DiagOffset={2}, Uid={3}",
                                ecuUid.Key.BaseVariant, ecuUid.Key.DiagAddrAsInt, ecuUid.Key.DiagnosisAddress.Offset, ecuUid.Value.EcuUid));
                        }
#if false
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "EcuUid failures: {0}", psdzReadEcuUid.FailureResponse.Count()));
                        foreach (IPsdzEcuFailureResponseCto failureResponse in psdzReadEcuUid.FailureResponse)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " Fail: BaseVar={0}, DiagAddr={1}, DiagOffset={2}, Cause={3}",
                                failureResponse.EcuIdentifierCto.BaseVariant, failureResponse.EcuIdentifierCto.DiagAddrAsInt, failureResponse.EcuIdentifierCto.DiagnosisAddress.Offset,
                                failureResponse.Cause.Description));
                        }
#endif
                        UpdateStatus(sbResult.ToString());

                        IPsdzReadStatusResultCto psdzReadStatusResult = programmingService.Psdz.SecureFeatureActivationService.ReadStatus(PsdzStatusRequestFeatureTypeEtoEnum.ALL_FEATURES, psdzConnection, psdzContext.SvtActual, psdzEcuIdentifiers, true, 3, 100);
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Status failures: {0}", psdzReadStatusResult.Failures.Count()));
#if false
                        foreach (IPsdzEcuFailureResponseCto failureResponse in psdzReadStatusResult.Failures)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " Fail: BaseVar={0}, DiagAddr={1}, DiagOffset={2}, Cause={3}",
                                failureResponse.EcuIdentifierCto.BaseVariant, failureResponse.EcuIdentifierCto.DiagAddrAsInt, failureResponse.EcuIdentifierCto.DiagnosisAddress.Offset,
                                failureResponse.Cause.Description));
                        }
#endif
                        UpdateStatus(sbResult.ToString());

                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Status features: {0}", psdzReadStatusResult.FeatureStatusSet.Count()));
                        foreach (IPsdzFeatureLongStatusCto featureLongStatus in psdzReadStatusResult.FeatureStatusSet)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " Feature: BaseVar={0}, DiagAddr={1}, DiagOffset={2}, Status={3}, Token={4}",
                                featureLongStatus.EcuIdentifierCto.BaseVariant, featureLongStatus.EcuIdentifierCto.DiagAddrAsInt, featureLongStatus.EcuIdentifierCto.DiagnosisAddress.Offset,
                                featureLongStatus.FeatureStatusEto, featureLongStatus.TokenId));
                        }
                        UpdateStatus(sbResult.ToString());

                        IPsdzSollverbauung psdzSollverbauung = programmingService.Psdz.LogicService.GenerateSollverbauungGesamtFlash(psdzConnection, psdzIstufeTarget, psdzIstufeShip, psdzContext.SvtActual, psdzContext.FaActual, psdzContext.TalFilter);
                        psdzContext.SetSollverbauung(psdzSollverbauung);
                        sbResult.AppendLine("Target flash:");
                        sbResult.Append(psdzSollverbauung.AsXml);
                        UpdateStatus(sbResult.ToString());

                        IEnumerable<IPsdzEcuContextInfo> psdzEcuContextInfos = programmingService.Psdz.EcuService.RequestEcuContextInfos(psdzConnection, psdzEcuIdentifiers);
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Ecu contexts: {0}", psdzEcuContextInfos.Count()));
                        foreach (IPsdzEcuContextInfo ecuContextInfo in psdzEcuContextInfos)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " Ecu context: BaseVar={0}, DiagAddr={1}, DiagOffset={2}, ManuDate={3}, PrgDate={4}, PrgCnt={5}, FlashCnt={6}, FlashRemain={7}",
                                ecuContextInfo.EcuId.BaseVariant, ecuContextInfo.EcuId.DiagAddrAsInt, ecuContextInfo.EcuId.DiagnosisAddress.Offset,
                                ecuContextInfo.ManufacturingDate, ecuContextInfo.LastProgrammingDate, ecuContextInfo.ProgramCounter, ecuContextInfo.PerformedFlashCycles, ecuContextInfo.RemainingFlashCycles));
                        }
                        UpdateStatus(sbResult.ToString());

                        IPsdzSwtAction psdzSwtAction = programmingService.Psdz.ProgrammingService.RequestSwtAction(psdzConnection, true);
                        psdzContext.SwtAction = psdzSwtAction;
                        if (psdzSwtAction?.SwtEcus != null)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Ecus: {0}", psdzSwtAction.SwtEcus.Count()));
                            foreach (IPsdzSwtEcu psdzSwtEcu in psdzSwtAction.SwtEcus)
                            {
                                sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Ecu: Id={0}, Vin={1}, CertState={2}, SwSig={3}",
                                    psdzSwtEcu.EcuIdentifier, psdzSwtEcu.Vin, psdzSwtEcu.RootCertState, psdzSwtEcu.SoftwareSigState));
                            }
                        }
                        UpdateStatus(sbResult.ToString());

                        IPsdzTal psdzTal = programmingService.Psdz.LogicService.GenerateTal(psdzConnection, psdzContext.SvtActual, psdzSollverbauung, psdzContext.SwtAction, psdzContext.TalFilter);
                        psdzContext.Tal = psdzTal;
                        sbResult.AppendLine("Tal:");
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " State: {0}", psdzTal.TalExecutionState));
                        foreach (IPsdzEcuIdentifier ecuIdentifier in psdzTal.AffectedEcus)
                        {
                            sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " Affected Ecu: BaseVar={0}, DiagAddr={1}, DiagOffset={2}",
                                ecuIdentifier.BaseVariant, ecuIdentifier.DiagAddrAsInt, ecuIdentifier.DiagnosisAddress.Offset));
                        }

                        foreach (IPsdzTalLine talLine in psdzTal.TalLines)
                        {
                            sbResult.Append(string.Format(CultureInfo.InvariantCulture, " Tal line: BaseVar={0}, DiagAddr={1}, DiagOffset={2}",
                                talLine.EcuIdentifier.BaseVariant, talLine.EcuIdentifier.DiagAddrAsInt, talLine.EcuIdentifier.DiagnosisAddress.Offset));
                            sbResult.Append(string.Format(CultureInfo.InvariantCulture, " Fsc={0}, Flash={1}, Iba={2}, Sw={3}, Restore={4}, Sfa={5}",
                                talLine.FscDeploy.Tas.Count(), talLine.BlFlash.Tas.Count(), talLine.IbaDeploy.Tas.Count(),
                                talLine.SwDeploy.Tas.Count(), talLine.IdRestore.Tas.Count(), talLine.SFADeploy.Tas.Count()));
                            sbResult.AppendLine();
                        }
                        UpdateStatus(sbResult.ToString());

                        IPsdzTal psdzBackupTal = programmingService.Psdz.IndividualDataRestoreService.GenerateBackupTal(psdzConnection, psdzContext.PathToBackupData, psdzTal, psdzContext.TalFilter);
                        psdzContext.IndividualDataBackupTal = psdzBackupTal;
                        sbResult.AppendLine(" Backup Tal:");
                        sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, " State: {0}", psdzBackupTal.TalExecutionState));
                        UpdateStatus(sbResult.ToString());

                        break;
                    }
                }

                UpdateStatus(sbResult.ToString());
                return true;
            }
            catch (Exception ex)
            {
                sbResult.AppendLine(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", ex.Message));
                UpdateStatus(sbResult.ToString());
                return false;
            }
        }

        private bool InitProgrammingObjects(string istaFolder)
        {
            try
            {
                psdzContext = new PsdzContext(istaFolder);
                ProgrammingTaskFlags programmingTaskFlags =
                    ProgrammingTaskFlags.Mount |
                    ProgrammingTaskFlags.Unmount |
                    ProgrammingTaskFlags.Replace |
                    ProgrammingTaskFlags.Flash |
                    ProgrammingTaskFlags.Code |
                    ProgrammingTaskFlags.DataRecovery |
                    ProgrammingTaskFlags.Fsc;
                IPsdzTalFilter psdzTalFilter = ProgrammingUtils.CreateTalFilter(programmingTaskFlags, programmingService.Psdz.ObjectBuilder);
                psdzContext.SetTalFilter(psdzTalFilter);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void ClearProgrammingObjects()
        {
            activePsdzConnection = null;
            if (psdzContext != null)
            {
                psdzContext.CleanupBackupData();
                psdzContext = null;
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonAbort_Click(object sender, EventArgs e)
        {

        }

        private void buttonIstaFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialogIsta.SelectedPath = textBoxIstaFolder.Text;
            DialogResult result = folderBrowserDialogIsta.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxIstaFolder.Text = folderBrowserDialogIsta.SelectedPath;
                UpdateDisplay();
            }
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            UpdateDisplay();
            StoreSettings();
            timerUpdate.Enabled = false;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            LoadSettings();
            UpdateDisplay();
            UpdateStatus();
            timerUpdate.Enabled = true;
        }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void buttonStartHost_Click(object sender, EventArgs e)
        {
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine("Starting host ...");
            UpdateStatus(sbMessage.ToString());

            StartProgrammingServiceTask("32395").ContinueWith(task =>
            {
                taskActive = false;
                if (task.Result)
                {
                    sbMessage.AppendLine("Host started");
                }
                else
                {
                    sbMessage.AppendLine("Host start failed");
                }
                UpdateStatus(sbMessage.ToString());
            });

            taskActive = true;
            UpdateDisplay();
        }

        private void buttonStopHost_Click(object sender, EventArgs e)
        {
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine("Stopping host ...");
            UpdateStatus(sbMessage.ToString());

            StopProgrammingServiceTask().ContinueWith(task =>
            {
                taskActive = false;
                if (task.Result)
                {
                    sbMessage.AppendLine("Host stopped");
                }
                else
                {
                    sbMessage.AppendLine("Host stop failed");
                }
                UpdateStatus(sbMessage.ToString());

                if (e == null)
                {
                    BeginInvoke((Action)(() =>
                    {
                        Close();
                    }));
                }
            });

            taskActive = true;
            UpdateDisplay();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (taskActive)
            {
                e.Cancel = true;
                return;
            }

            if (programmingService != null && programmingService.IsPsdzPsdzServiceHostInitialized())
            {
                buttonStopHost_Click(sender, null);
                e.Cancel = true;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (activePsdzConnection != null)
            {
                return;
            }

            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine("Connecting vehicle ...");
            UpdateStatus(sbMessage.ToString());

            string url = "tcp://" + ipAddressControlVehicleIp.Text + ":6801";
            ConnectVehicleTask(textBoxIstaFolder.Text, url, Baureihe).ContinueWith(task =>
            {
                taskActive = false;
                IPsdzConnection psdzConnection = task.Result;
                if (psdzConnection != null)
                {
                    activePsdzConnection = psdzConnection;
                    sbMessage.AppendLine("Vehicle connected");
                    sbMessage.AppendLine(string.Format(CultureInfo.InvariantCulture, "Id: {0}", psdzConnection.Id));
                    sbMessage.AppendLine(string.Format(CultureInfo.InvariantCulture, "Port: {0}", psdzConnection.Port));
                    sbMessage.AppendLine(string.Format(CultureInfo.InvariantCulture, "Project: {0}", psdzConnection.TargetSelector.Project));
                    sbMessage.AppendLine(string.Format(CultureInfo.InvariantCulture, "Vehicle: {0}", psdzConnection.TargetSelector.VehicleInfo));
                    sbMessage.AppendLine(string.Format(CultureInfo.InvariantCulture, "Series: {0}", psdzConnection.TargetSelector.Baureihenverbund));
                    sbMessage.AppendLine(string.Format(CultureInfo.InvariantCulture, "Direct: {0}", psdzConnection.TargetSelector.IsDirect));

                    if (psdzContext.TalFilter != null)
                    {
                        sbMessage.AppendLine("TalFilter:");
                        sbMessage.Append(psdzContext.TalFilter.AsXml);
                    }
                }
                else
                {
                    sbMessage.AppendLine("Vehicle connect failed");
                }
                UpdateStatus(sbMessage.ToString());
            });

            taskActive = true;
            UpdateDisplay();
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            if (activePsdzConnection == null)
            {
                return;
            }

            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine("Disconnecting vehicle ...");
            UpdateStatus(sbMessage.ToString());

            DisconnectVehicleTask(activePsdzConnection).ContinueWith(task =>
            {
                taskActive = false;
                activePsdzConnection = null;
                if (task.Result)
                {
                    sbMessage.AppendLine("Vehicle disconnected");
                }
                else
                {
                    sbMessage.AppendLine("Vehicle disconnect failed");
                }
                UpdateStatus(sbMessage.ToString());
            });

            taskActive = true;
            UpdateDisplay();
        }

        private void buttonFunc_Click(object sender, EventArgs e)
        {
            if (activePsdzConnection == null)
            {
                return;
            }

            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine("Executing vehicle functions ...");
            UpdateStatus(sbMessage.ToString());

            int function = 0;
            if (sender == buttonFunc1)
            {
                function = 0;
            }
            if (sender == buttonFunc2)
            {
                function = 1;
            }

            VehicleFunctionsTask(activePsdzConnection, function).ContinueWith(task =>
            {
                taskActive = false;
            });

            taskActive = true;
            UpdateDisplay();
        }
    }
}
