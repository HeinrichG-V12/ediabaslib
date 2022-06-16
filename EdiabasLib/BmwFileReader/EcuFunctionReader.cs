using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Zip;
// ReSharper disable ConvertToUsingDeclaration

namespace BmwFileReader
{
    public class EcuFunctionReader
    {
        public const string EcuFuncFileName = "EcuFunctions.zip";
        public const string FaultDataBaseName = "faultdata_";
        private readonly string _rootDir;
        private readonly Dictionary<string, EcuFunctionStructs.EcuVariant> _ecuVariantDict;
        private readonly Dictionary<string, EcuFunctionStructs.EcuFaultCodeLabel> _ecuFaultCodeLabelDict;
        private readonly Dictionary<string, EcuFunctionStructs.EcuFaultModeLabel> _ecuFaultModeLabelDict;
        private readonly Dictionary<string, EcuFunctionStructs.EcuEnvCondLabel> _ecuEnvCondLabelDict;
        public EcuFunctionStructs.EcuFaultData _ecuFaultData;
        private string _ecuFaultDataLanguage;

        public EcuFunctionReader(string rootDir)
        {
            _rootDir = rootDir;
            _ecuVariantDict = new Dictionary<string, EcuFunctionStructs.EcuVariant>();
            _ecuFaultCodeLabelDict = new Dictionary<string, EcuFunctionStructs.EcuFaultCodeLabel>();
            _ecuFaultModeLabelDict = new Dictionary<string, EcuFunctionStructs.EcuFaultModeLabel>();
            _ecuEnvCondLabelDict = new Dictionary<string, EcuFunctionStructs.EcuEnvCondLabel>();
        }

        public bool Init(string language)
        {
            if (GetEcuFaultDataCached(language) == null)
            {
                return false;
            }

            return true;
        }

        public bool IsInitRequired(string language)
        {
            return _ecuFaultData == null || _ecuFaultDataLanguage == null ||
                    string.Compare(_ecuFaultDataLanguage, language, StringComparison.OrdinalIgnoreCase) != 0;
        }

        public void Reset()
        {
            _ecuFaultDataLanguage = null;
            _ecuFaultCodeLabelDict.Clear();
            _ecuFaultModeLabelDict.Clear();
            _ecuEnvCondLabelDict.Clear();
            _ecuVariantDict.Clear();
        }

        public List<EcuFunctionStructs.EcuFixedFuncStruct> GetFixedFuncStructList(EcuFunctionStructs.EcuVariant ecuVariant)
        {
            List<EcuFunctionStructs.EcuFixedFuncStruct> fixedFuncStructList = new List<EcuFunctionStructs.EcuFixedFuncStruct>();

            if (ecuVariant.RefEcuVariantList != null)
            {
                foreach (EcuFunctionStructs.RefEcuVariant refEcuVariant in ecuVariant.RefEcuVariantList)
                {
                    if (refEcuVariant.FixedFuncStructList != null)
                    {
                        fixedFuncStructList.AddRange(refEcuVariant.FixedFuncStructList);
                    }
                }
            }

            if (ecuVariant.EcuFuncStructList != null)
            {
                foreach (EcuFunctionStructs.EcuFuncStruct ecuFuncStruct in ecuVariant.EcuFuncStructList)
                {
                    if (ecuFuncStruct.FixedFuncStructList != null)
                    {
                        fixedFuncStructList.AddRange(ecuFuncStruct.FixedFuncStructList);
                    }
                }
            }

            return fixedFuncStructList;
        }

        public bool IsValidFaultCode(Int64 errorCode, EcuFunctionStructs.EcuVariant ecuVariant, bool relevantOnly = false)
        {
            if (!ecuVariant.EcuFaultCodeDict.TryGetValue(errorCode, out EcuFunctionStructs.EcuFaultCode ecuFaultCode))
            {
                return false;
            }

            if (!relevantOnly || ecuFaultCode.Relevance.ConvertToInt() > 0)
            {
                return true;
            }

            return false;
        }

        public EcuFunctionStructs.EcuFaultCodeLabel GetFaultCodeLabel(Int64 errorCode, EcuFunctionStructs.EcuVariant ecuVariant)
        {
            if (!ecuVariant.EcuFaultCodeDict.TryGetValue(errorCode, out EcuFunctionStructs.EcuFaultCode ecuFaultCode))
            {
                return null;
            }

            if (!_ecuFaultCodeLabelDict.TryGetValue(ecuFaultCode.EcuFaultCodeLabelId.ToLowerInvariant(), out EcuFunctionStructs.EcuFaultCodeLabel ecuFaultCodeLabel))
            {
                return null;
            }

            return ecuFaultCodeLabel;
        }

        public List<EcuFunctionStructs.EcuFaultModeLabel> GetFaultModeLabelMatchList(List<EcuFunctionStructs.EcuFaultModeLabel> ecuFaultModeLabelList, Int64 modeNumber)
        {
            // ReSharper disable once UseNullPropagation
            if (ecuFaultModeLabelList == null)
            {
                return null;
            }

            List<EcuFunctionStructs.EcuFaultModeLabel> ecuFaultModeLabelMatchList =
                ecuFaultModeLabelList.Where(x => x.Code.ConvertToInt() == modeNumber).
                    OrderBy(x => x.Id.ConvertToInt()).ToList();

            return ecuFaultModeLabelMatchList;
        }

        public List<EcuFunctionStructs.EcuFaultModeLabel> GetFaultModeLabelList(Int64 errorCode, EcuFunctionStructs.EcuVariant ecuVariant)
        {
            if (!ecuVariant.EcuFaultCodeDict.TryGetValue(errorCode, out EcuFunctionStructs.EcuFaultCode ecuFaultCode))
            {
                return null;
            }

            if (ecuFaultCode.EcuFaultModeLabelIdList == null)
            {
                return null;
            }

            List<EcuFunctionStructs.EcuFaultModeLabel> ecuFaultModeLabelList = new List<EcuFunctionStructs.EcuFaultModeLabel>();
            foreach (string ecuFaultModeId in ecuFaultCode.EcuFaultModeLabelIdList)
            {
                if (_ecuFaultModeLabelDict.TryGetValue(ecuFaultModeId.ToLowerInvariant(), out EcuFunctionStructs.EcuFaultModeLabel ecuFaultModeLabel))
                {
                    ecuFaultModeLabelList.Add(ecuFaultModeLabel);
                }
            }

            return ecuFaultModeLabelList;
        }

        public List<EcuFunctionStructs.EcuEnvCondLabel> GetEnvCondLabelMatchList(List<EcuFunctionStructs.EcuEnvCondLabel> ecuEnvCondLabelList, Int64 envNumber)
        {
            // ReSharper disable once UseNullPropagation
            if (ecuEnvCondLabelList == null)
            {
                return null;
            }

            List<EcuFunctionStructs.EcuEnvCondLabel> ecuEnvCondLabelMatchList =
                ecuEnvCondLabelList.Where(x => string.Compare(x.IdentStr, envNumber.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) == 0).
                    OrderBy(x => x.Id.ConvertToInt()).ToList();

            return ecuEnvCondLabelMatchList;
        }

        public List<EcuFunctionStructs.EcuEnvCondLabel> GetEnvCondLabelMatchList(List<EcuFunctionStructs.EcuEnvCondLabel> ecuEnvCondLabelList, string envName)
        {
            // ReSharper disable once UseNullPropagation
            if (ecuEnvCondLabelList == null)
            {
                return null;
            }

            List<EcuFunctionStructs.EcuEnvCondLabel> ecuEnvCondLabelMatchList =
                ecuEnvCondLabelList.Where(x => string.Compare(x.IdentStr, envName, StringComparison.OrdinalIgnoreCase) == 0).
                    OrderBy(x => x.Id.ConvertToInt()).ToList();

            return ecuEnvCondLabelMatchList;
        }

        public List<EcuFunctionStructs.EcuEnvCondLabel> GetEnvCondLabelList(Int64 errorCode, EcuFunctionStructs.EcuVariant ecuVariant)
        {
            if (!ecuVariant.EcuFaultCodeDict.TryGetValue(errorCode, out EcuFunctionStructs.EcuFaultCode ecuFaultCode))
            {
                return null;
            }

            if (ecuFaultCode.EcuEnvCondLabelIdList == null)
            {
                return null;
            }

            List<EcuFunctionStructs.EcuEnvCondLabel> ecuEnvCondLabelList = new List<EcuFunctionStructs.EcuEnvCondLabel>();
            foreach (string ecuEnvCondId in ecuFaultCode.EcuEnvCondLabelIdList)
            {
                if (_ecuEnvCondLabelDict.TryGetValue(ecuEnvCondId.ToLowerInvariant(), out EcuFunctionStructs.EcuEnvCondLabel ecuEnvCondLabel))
                {
                    ecuEnvCondLabelList.Add(ecuEnvCondLabel);
                }
            }

            return ecuEnvCondLabelList;
        }

        // for tesing result states only!
        public List<EcuFunctionStructs.EcuEnvCondLabel> GetEnvCondLabelListWithResultStates(EcuFunctionStructs.EcuVariant ecuVariant)
        {
            List<EcuFunctionStructs.EcuEnvCondLabel> ecuEnvCondLabelList = new List<EcuFunctionStructs.EcuEnvCondLabel>();
            foreach (KeyValuePair<Int64, EcuFunctionStructs.EcuFaultCode> ecuFaultCodePair in ecuVariant.EcuFaultCodeDict)
            {
                if (ecuFaultCodePair.Value.EcuEnvCondLabelIdList != null)
                {
                    foreach (string ecuEnvCondId in ecuFaultCodePair.Value.EcuEnvCondLabelIdList)
                    {
                        if (_ecuEnvCondLabelDict.TryGetValue(ecuEnvCondId.ToLowerInvariant(), out EcuFunctionStructs.EcuEnvCondLabel ecuEnvCondLabel))
                        {
                            if (ecuEnvCondLabel.EcuResultStateValueList != null && ecuEnvCondLabel.EcuResultStateValueList.Count > 0)
                            {
                                ecuEnvCondLabelList.Add(ecuEnvCondLabel);
                            }
                        }
                    }
                }
            }

            return ecuEnvCondLabelList;
        }

        public EcuFunctionStructs.EcuVariant GetEcuVariantCached(string ecuName)
        {
            try
            {
                if (string.IsNullOrEmpty(ecuName))
                {
                    return null;
                }

                string key = ecuName.ToLowerInvariant();
                if (_ecuVariantDict.TryGetValue(key, out EcuFunctionStructs.EcuVariant ecuVariant))
                {
                    return ecuVariant;
                }

                ecuVariant = GetEcuVariant(ecuName);
                _ecuVariantDict[key] = ecuVariant;

                return ecuVariant;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public EcuFunctionStructs.EcuFaultData GetEcuFaultDataCached(string language)
        {
            if (IsInitRequired(language))
            {
                Reset();
                _ecuFaultData = GetEcuFaultData(language);
                if (_ecuFaultData != null)
                {
                    _ecuFaultDataLanguage = language;
                    if (_ecuFaultData.EcuFaultCodeLabelList != null)
                    {
                        foreach (EcuFunctionStructs.EcuFaultCodeLabel ecuFaultCodeLabel in _ecuFaultData.EcuFaultCodeLabelList)
                        {
                            string key = ecuFaultCodeLabel.Id.ToLowerInvariant();
                            _ecuFaultCodeLabelDict.TryAdd(key, ecuFaultCodeLabel);
                        }
                    }

                    if (_ecuFaultData.EcuFaultModeLabelList != null)
                    {
                        foreach (EcuFunctionStructs.EcuFaultModeLabel ecuFaultModeLabel in _ecuFaultData.EcuFaultModeLabelList)
                        {
                            string key = ecuFaultModeLabel.Id.ToLowerInvariant();
                            _ecuFaultModeLabelDict.TryAdd(key, ecuFaultModeLabel);
                        }
                    }

                    if (_ecuFaultData.EcuEnvCondLabelList != null)
                    {
                        foreach (EcuFunctionStructs.EcuEnvCondLabel ecuEnvCondLabel in _ecuFaultData.EcuEnvCondLabelList)
                        {
                            string key = ecuEnvCondLabel.Id.ToLowerInvariant();
                            _ecuEnvCondLabelDict.TryAdd(key, ecuEnvCondLabel);
                        }
                    }
                }
            }

            return _ecuFaultData;
        }

        public EcuFunctionStructs.EcuVariant GetEcuVariant(string ecuName)
        {
            EcuFunctionStructs.EcuVariant ecuVariant = GetEcuDataObject(ecuName, typeof(EcuFunctionStructs.EcuVariant)) as EcuFunctionStructs.EcuVariant;
            if (ecuVariant?.EcuFaultCodeList != null)
            {
                Dictionary<Int64, EcuFunctionStructs.EcuFaultCode> ecuFaultCodeDict = new Dictionary<Int64, EcuFunctionStructs.EcuFaultCode>();
                foreach (EcuFunctionStructs.EcuFaultCode ecuFaultCode in ecuVariant.EcuFaultCodeList)
                {
                    Int64 errorCode = ecuFaultCode.Code.ConvertToInt();
                    if (errorCode != 0)
                    {
                        ecuFaultCodeDict.TryAdd(errorCode, ecuFaultCode);
                    }
                }

                ecuVariant.EcuFaultCodeDict = ecuFaultCodeDict;
            }
            return ecuVariant;
        }

        public EcuFunctionStructs.EcuFaultData GetEcuFaultData(string language)
        {
            string fileName = FaultDataBaseName + language.ToLowerInvariant();
            EcuFunctionStructs.EcuFaultData ecuFaultData = GetEcuDataObject(fileName, typeof(EcuFunctionStructs.EcuFaultData)) as EcuFunctionStructs.EcuFaultData;
            if (ecuFaultData == null)
            {
                fileName = FaultDataBaseName + "en";
                ecuFaultData = GetEcuDataObject(fileName, typeof(EcuFunctionStructs.EcuFaultData)) as EcuFunctionStructs.EcuFaultData;
            }
            return ecuFaultData;
        }

        public object GetEcuDataObject(string name, Type type)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                object ecuObject = null;
                ZipFile zf = null;
                try
                {
                    string fileName = name.ToLowerInvariant() + ".xml";
                    FileStream fs = File.OpenRead(Path.Combine(_rootDir, EcuFuncFileName));
                    zf = new ZipFile(fs);
                    foreach (ZipEntry zipEntry in zf)
                    {
                        if (!zipEntry.IsFile)
                        {
                            continue; // Ignore directories
                        }
                        if (string.Compare(zipEntry.Name, fileName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            Stream zipStream = zf.GetInputStream(zipEntry);
                            using (TextReader reader = new StreamReader(zipStream))
                            {
                                XmlSerializer serializer = new XmlSerializer(type);
                                ecuObject = serializer.Deserialize(reader);
                            }
                            break;
                        }
                    }

                    return ecuObject;
                }
                finally
                {
                    if (zf != null)
                    {
                        zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                        zf.Close(); // Ensure we release resources
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
