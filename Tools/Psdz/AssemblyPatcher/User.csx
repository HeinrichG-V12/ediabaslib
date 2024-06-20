﻿// Requires CodegenCS VS extension
// https://marketplace.visualstudio.com/items?itemName=Drizin.CodegenCS
using CodegenCS.Runtime;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UserTemplate
{
    public class Info
    {
        public string Namespace { set; get; }
        public string Class { set; get; }
        public string Name { set; get; }
        public string Filename { set; get; }
    }

    public class InfoDict
    {
        public Dictionary<string, Info> PatchInfo { set; get; }
    }

    async Task<int> Main(ICodegenContext context, ILogger logger, VSExecutionContext vsContext)
    {
        string templatePath = vsContext?.TemplatePath;
        if (string.IsNullOrEmpty(templatePath))
        {
            templatePath = "User.csx";
            await logger.WriteLineAsync($"Template path is empty using: {templatePath}");
        }

        string templateName = Path.GetFileNameWithoutExtension(templatePath);
        await logger.WriteLineAsync($"Template name: {templatePath}");
        bool result = await GenerateConfig(context[templateName + ".config"], logger);
        if (!result)
        {
            await logger.WriteLineAsync("GenerateConfig failed");
            return 1;
        }

        return 0;
    }

    async Task<bool> GenerateConfig(ICodegenTextWriter writer, ILogger logger)
    {
        string patchCtorNamespace = string.Empty;
        string patchCtorClass = string.Empty;
        string patchMethodNamespace = string.Empty;
        string patchMethodClass = string.Empty;
        string patchMethodName = string.Empty;
        string licFileName = string.Empty;

        try
        {
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".apk", "psdz_patcher.json");
            if (File.Exists(fileName))
            {
                InfoDict infoDict = JsonConvert.DeserializeObject<InfoDict>(File.ReadAllText(fileName));
                if (infoDict != null)
                {
                    if (infoDict.PatchInfo.TryGetValue("Ctor", out Info ctorInfo))
                    {
                        patchCtorNamespace = ctorInfo.Namespace;
                        patchCtorClass = ctorInfo.Class;
                        await logger.WriteLineAsync($"Ctor: Namespace={patchCtorNamespace}, Class={patchCtorClass}");
                    }

                    if (infoDict.PatchInfo.TryGetValue("Method", out Info methodInfo))
                    {
                        patchMethodNamespace = methodInfo.Namespace;
                        patchMethodClass = methodInfo.Class;
                        patchMethodName = methodInfo.Name;
                        await logger.WriteLineAsync($"Method: Namespace={patchMethodNamespace}, Class={patchMethodClass}, Name={patchMethodName}");
                    }

                    if (infoDict.PatchInfo.TryGetValue("License", out Info licInfo))
                    {
                        licFileName = licInfo.Filename;
                        await logger.WriteLineAsync($"License: Filename={licFileName}");
                    }
                }
            }
            else
            {
                await logger.WriteLineAsync($"Configuration file not found: {fileName}");
            }
        }
        catch (Exception ex)
        {
            await logger.WriteLineAsync($"Exception: {ex.Message}");
            return false;
        }

        writer.WriteLine(
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<appSettings>
    <add key=""PatchCtorNamespace"" value=""{patchCtorNamespace}""/>
    <add key=""PatchCtorClass"" value=""{patchCtorClass}""/>
    <add key=""PatchMethodNamespace"" value=""{patchMethodNamespace}""/>
    <add key=""PatchMethodClass"" value=""{patchMethodClass}""/>
    <add key=""PatchMethodName"" value=""{patchMethodName}""/>
    <add key=""LicFileName"" value=""{licFileName}""/>
</appSettings>");

        return true;
    }
}
