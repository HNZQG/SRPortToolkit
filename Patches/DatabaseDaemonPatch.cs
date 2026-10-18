using BepInEx.Logging;
using Hacknet;
using SRPortToolkit.NameType;
using System;
using System.IO;

namespace SRPortToolkit.Patches
{
    public static class DatabaseDaemonPatch
    {
        public static string GetFilenameForObject(object obj, Folder datasetFolder)
        {
            if (obj == null)
                return null;

            try
            {
                string fileName = null;

                if (obj is FileArchive fileArchive)
                {
                    fileName = !string.IsNullOrEmpty(fileArchive.FileCode)
                        ? SanitizeFileName(fileArchive.FileCode)
                        : "unknown_file_" + Guid.NewGuid().ToString().Substring(0, 8);
                }
                else if (obj is CharacterArchive characterArchive)
                {
                    fileName = !string.IsNullOrEmpty(characterArchive.Name)
                        ? SanitizeFileName(characterArchive.Name)
                        : "unknown_character_" + Guid.NewGuid().ToString().Substring(0, 8);
                }
                else if (obj is PreLibraryOfDocumentFiles preLibrary)
                {
                    fileName = !string.IsNullOrEmpty(preLibrary.KeyWord)
                        ? SanitizeFileName(preLibrary.KeyWord)
                        : "unknown_document_" + Guid.NewGuid().ToString().Substring(0, 8);
                }
                else if (obj is string str)
                {
                    fileName = SanitizeFileName(str);
                }
                else
                {
                    fileName = $"{obj.GetType().Name}_{obj.GetHashCode()}";
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    fileName = GetUniqueFileName(datasetFolder, fileName + ".OLPkg");
                    return fileName;
                }

                return null;
            }
            catch (Exception ex)
            {       
                Logger.CreateLogSource("Error in GetFilenameForObject: " + ex.Message);
                return GetUniqueFileName(datasetFolder, "error_file_" + Guid.NewGuid().ToString().Substring(0, 8) + ".OLPkg");
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            if (fileName.Length > 50)
                fileName = fileName.Substring(0, 50);

            fileName = fileName.Trim().TrimEnd('.');

            if (string.IsNullOrWhiteSpace(fileName))
                return "unnamed";

            return fileName;
        }
        private static string GetUniqueFileName(Folder folder, string fileName)
        {
            if (folder == null)
                return fileName;

            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string uniqueFileName = fileName;
            int counter = 1;

            // 检查文件是否已存在
            while (FileExistsInFolder(folder, uniqueFileName))
            {
                uniqueFileName = $"{nameWithoutExt}_{counter}{extension}";
                counter++;
            }

            return uniqueFileName;
        }

        private static bool FileExistsInFolder(Folder folder, string fileName)
        {
            if (folder == null || folder.files == null)
                return false;

            foreach (var file in folder.files)
            {
                if (file != null && file.name == fileName)
                    return true;
            }
            return false;
        }

        public static FileEntry CreateFileFromObject(object obj, string fileName)
        {
            if (obj == null || string.IsNullOrEmpty(fileName))
                return null;

            try
            {
                string content = GenerateFileContent(obj);
                return new FileEntry(content, fileName);
            }
            catch (Exception ex)
            {
                Logger.CreateLogSource("Error creating file from object: " + ex.Message);
                return new FileEntry($"Error serializing object: {ex.Message}", fileName);
            }
        }

        private static string GenerateFileContent(object obj)
        {
            if (obj == null)
                return string.Empty;

            if (obj is FileArchive fileArchive)
            {
                return $@"=== 文件档案 ===
文件代码: {fileArchive.FileCode}
文件标题: {fileArchive.FileTitle}
安全级别: {fileArchive.SecurityLevel}
所属部门: {fileArchive.Department}
状态: {fileArchive.State}

=== 内容简介 ===
{fileArchive.FileIntroduction1}
{fileArchive.FileIntroduction2}
{fileArchive.FileIntroduction3}
{fileArchive.FileIntroduction4}
{fileArchive.FileIntroduction5}

=== 备注信息 ===
{fileArchive.FileRemarks1}
{fileArchive.FileRemarks2}
{fileArchive.FileRemarks3}
{fileArchive.FileRemarks4}
{fileArchive.FileRemarks5}

创建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            else if (obj is CharacterArchive character)
            {
                return $@"=== 角色档案 ===
姓名: {character.Name}
性别: {character.Gender}
职位: {character.Position}
所属组织: {character.Organization}

=== 人物介绍 ===
{character.CharacterIntroduction1}
{character.CharacterIntroduction2}
{character.CharacterIntroduction3}
{character.CharacterIntroduction4}
{character.CharacterIntroduction5}

=== 备注信息 ===
{character.CharacterRemarks1}
{character.CharacterRemarks2}
{character.CharacterRemarks3}
{character.CharacterRemarks4}
{character.CharacterRemarks5}

创建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            else if (obj is PreLibraryOfDocumentFiles preLibrary)
            {
                return $@"=== 预图书馆文档 ===
关键词: {preLibrary.KeyWord}
文档标题: {preLibrary.FILETitle1}
{preLibrary.FILETitle2}
{preLibrary.FILETitle3}
{preLibrary.FILETitle4}
{preLibrary.FILETitle5}
文档代码: {preLibrary.FILECodes1}
{preLibrary.FILECodes2}
{preLibrary.FILECodes3}
{preLibrary.FILECodes4}
{preLibrary.FILECodes5}

创建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                // 默认使用 ToString()
                return obj.ToString() ?? "No content available";
            }
        }
    }
}