using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace Utage
{
    public class ProjectZipper
    {
        private readonly List<string> foldersToZip = new()
        {
            "Assets",
            "Packages",
            "ProjectSettings"
        };
        
        //指定パスに圧縮ファイルを作る
        public void ZipProject(string zipFilePath)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string zipPath = Path.Combine(projectPath, zipFilePath);

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            try
            {
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        int totalFiles = CountFilesToZip(projectPath, foldersToZip);
                        int currentFileCount = 0;

                        foreach (string folder in foldersToZip)
                        {
                            string sourcePath = Path.Combine(projectPath, folder);
                            currentFileCount = AddDirectoryToZip(archive, sourcePath, folder, currentFileCount,
                                totalFiles);
                        }
                    }
                }

                Debug.Log("Project zipped successfully: " + zipPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error zipping project: " + ex.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private int AddDirectoryToZip(ZipArchive archive, string sourceDirName, string entryName, int currentFileCount,
            int totalFiles)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                Debug.LogWarning("Directory does not exist: " + sourceDirName);
                return currentFileCount;
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                string relativePath = Path.Combine(entryName, file.Name);
                archive.CreateEntryFromFile(file.FullName, relativePath);

                currentFileCount++;
                float progress = (float)currentFileCount / totalFiles;
                EditorUtility.DisplayProgressBar("Zipping Project", "Processing: " + relativePath, progress);
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string relativePath = Path.Combine(entryName, subDir.Name);
                currentFileCount =
                    AddDirectoryToZip(archive, subDir.FullName, relativePath, currentFileCount, totalFiles);
            }

            return currentFileCount;
        }

        private int CountFilesToZip(string projectPath, List<string> folders)
        {
            int fileCount = 0;

            foreach (string folder in folders)
            {
                string sourcePath = Path.Combine(projectPath, folder);
                DirectoryInfo dir = new DirectoryInfo(sourcePath);

                if (!dir.Exists)
                {
                    continue;
                }

                fileCount += dir.GetFiles("*", SearchOption.AllDirectories).Length;
            }

            return fileCount;
        }
    }
}
