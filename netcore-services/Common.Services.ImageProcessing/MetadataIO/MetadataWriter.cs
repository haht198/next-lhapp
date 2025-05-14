using Common.Services.Static;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using Common.Services.Static.Logger;

namespace Common.Services.ImageProcessing.MetadataIO
{
    public class MetadataCustomModel
    {
        public string Namespace { get; set; }
        public string Xmlns { get; set; }
        public IDictionary<string, string> Metadata { get; set; }
    }

    public static class MetadataWriter
    {

        public static (bool status, string errorOrWarning) WriteMetadata(string filePath, IDictionary<string, string> metadata = null, IList<MetadataCustomModel> customMetadatas = null)
        {
            var exifTemporaryConfigFilePath = Path.Combine(UserSetting.WorkspaceFolder, "metadata", "_temporary", $"exiftool-{Guid.NewGuid()}.config");
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(exifTemporaryConfigFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(exifTemporaryConfigFilePath));
                }
                using (var sw = new StreamWriter(exifTemporaryConfigFilePath))
                {
                    sw.Write(GetConfigString(customMetadatas));
                }

                var customMetadata = new Dictionary<string, string>();
                if (customMetadatas != null)
                {
                    foreach (var item in customMetadatas)
                    {
                        customMetadata.AddRange(item.Metadata);
                    }
                }

                var standardOutput = "";
                var standardError = "";
                var regexExiftool = new Regex(@"(\d+) image files updated\r?\n?$");
                if (FileTypeHelper.IsEIPFormat(filePath))
                {
                    var eipTemporaryFolder = Path.Combine(UserSetting.WorkspaceFolder, "metadata", "_temporary", Guid.NewGuid().ToString());
                    try
                    {
                        Directory.CreateDirectory(eipTemporaryFolder);
                        var files = Utils.UnzipFile(filePath, eipTemporaryFolder);
                        var rawFileInEip = files.FirstOrDefault(f => FileTypeHelper.IsRawFormat(Path.GetExtension(f)));

                        var arguments = BuildSetMetadataCommandArguments(rawFileInEip, exifTemporaryConfigFilePath, metadata, customMetadata);
                        (standardOutput, standardError) = Utils.RunProcess(arguments);
                        Logger.Info($"Write metadata to file: {filePath} finished", standardError, standardOutput, metadata, customMetadatas, arguments);

                        if (regexExiftool.Match(standardOutput).Success)
                        {
                            var temp_eip_filepath = Path.Combine(UserSetting.WorkspaceFolder, "metadata", "_temporary", $"{Guid.NewGuid()}.eip");
                            Utils.ZipFolderAsEipFile(eipTemporaryFolder, temp_eip_filepath);
                            if (File.Exists(temp_eip_filepath))
                            {
                                File.Delete(filePath);
                                File.Move(temp_eip_filepath, filePath);
                            }
                        }
                    }
                    finally
                    {
                        if (Directory.Exists(eipTemporaryFolder))
                        {
                            Directory.Delete(eipTemporaryFolder, true);
                        }
                    }
                }
                else
                {
                    var arguments = BuildSetMetadataCommandArguments(filePath, exifTemporaryConfigFilePath, metadata, customMetadata);
                    (standardOutput, standardError) = Utils.RunProcess(arguments);
                    Logger.Info($"Write metadata to file: {filePath} finished", standardError, standardOutput, metadata, customMetadatas, arguments);
                }

                return (regexExiftool.Match(standardOutput).Success, standardError); // && int.Parse(match.Groups[1].Value) == 1; //unchanged may be understood as success
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Write metadata exception: {ex.Message}");
                return (false, ex.Message);
            }
            finally
            {
                if (File.Exists(exifTemporaryConfigFilePath))
                {
                    File.Delete(exifTemporaryConfigFilePath);
                }
            }
        }

        private static string BuildSetMetadataCommandArguments(string filePath, string tempConfigPath, IDictionary<string, string> metadata, IDictionary<string, string> customMetadata)
        {
            var arguments = new List<string>();
            arguments.Add($"-config {tempConfigPath.KeepSafeArgumentValue()}");
            arguments.Add($"-overwrite_original");
            if (metadata != null)
            {
                BuildArgument(arguments, metadata);
            }

            if (customMetadata != null)
            {
                BuildArgument(arguments, customMetadata);
            }

            arguments.Add(filePath.KeepSafeArgumentValue());
            return string.Join(" ", arguments);
        }

        private static string GetConfigString(IList<MetadataCustomModel> customMetadatas)
        {
            var defaultSchema = new XMPSchema
            {
                Schema = "creativeforce",
                Xmlns = "http://ns.myname.com/creativeforce/1.0/",
                Tags = new List<string>
            {
                "contentcreationtype",
                "physicalfileid",
                "sendtovendordatetimeutc",
                "sendtovendorstate",
                "department",
                "class",
                "itemdescription",
                "fit",
                "directstyle",
                "iplongsku",
                "ipcolorcode",
                "color",
                "size",
                "seasonatt",
                "photographer",
                "tech",
                "stylist",
                "shottype",
                "modelname",
                "modelheight",
            }
            };
            var sb = new StringBuilder();
            var xmpSchemas = GetSchemas(customMetadatas);
            xmpSchemas.Insert(0, defaultSchema);

            //Schema - open
            sb.Append(@"
%Image::ExifTool::UserDefined = (
    'Image::ExifTool::XMP::Main' => {
");
            //Schema - element
            foreach (var schema in xmpSchemas)
            {
                sb.AppendFormat(@"
        '{0}' => {{
            SubDirectory => {{
                TagTable => 'Image::ExifTool::UserDefined::{0}',
            }},
        }},", schema.Schema);
            }

            //Schema - close
            sb.Append(@"
    },
);
");
            //Schema - detail
            foreach (var schema in xmpSchemas)
            {
                sb.AppendFormat(@"
%Image::ExifTool::UserDefined::{0} = (
    GROUPS => {{ 0 => 'XMP', 1 => 'XMP-{0}', 2 => 'Image' }},
    NAMESPACE => {{ '{0}' => '{1}' }},
    WRITABLE => 'string',
", schema.Schema, schema.Xmlns);
                foreach (var tag in schema.Tags)
                {
                    sb.AppendLine($"    '{tag}' => {{ Writable => 'string' }},");
                }
                sb.Append(@"
);
");
            }

            return sb.ToString();
        }
        private static List<XMPSchema> GetSchemas(IList<MetadataCustomModel> customMetadatas)
        {
            if (customMetadatas?.Count > 0)
            {
                var schemas = new List<XMPSchema>();
                foreach (var customMetadata in customMetadatas)
                {
                    var schema = new XMPSchema
                    {
                        Schema = customMetadata.Namespace,
                        Xmlns = customMetadata.Xmlns
                    };

                    var tags = new List<string>();
                    foreach (var fullTag in customMetadata.Metadata.Keys)
                    {
                        if (XMPTag.TryParse(fullTag, out var xmpTag))
                        {
                            tags.Add(xmpTag.Tag);
                        }
                    }

                    schema.Tags = tags;
                    schemas.Add(schema);
                }

                return schemas;
            }
            else
            {
                return new List<XMPSchema>();
            }
        }
        private static void BuildArgument(List<string> arguments, IDictionary<string, string> metadata)
        {
            foreach (var item in metadata)
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    arguments.Add($"-{item.Key.KeepSafeArgumentKey()}={item.Value.KeepSafeArgumentValue()}");
                    continue;
                }

                var emptyValue = "<${filename;$_=''}";
                arguments.Add($"-{item.Key.KeepSafeArgumentKey()}{emptyValue}");
            }
        }

        class XMPSchema
        {
            public string Schema { get; set; }
            public string Xmlns { get; set; }
            public List<string> Tags { get; set; }
        }
        class XMPTag
        {
            private static readonly Regex regex = new Regex("XMP-(.+):(.+)");
            public static bool TryParse(string fullTag, out XMPTag xmpTag)
            {
                var match = regex.Match(fullTag);
                if (!match.Success)
                {
                    xmpTag = null;
                    return false;
                }
                else
                {
                    xmpTag = new XMPTag
                    {
                        Schema = match.Groups[1].Value,
                        Tag = match.Groups[2].Value,
                    };
                    return true;
                }
            }
            public string Schema { get; set; }
            public string Tag { get; set; }
        }
    }
}
