using System.Text;
using FwHeadless.Controllers;

namespace FwHeadless.Media;

/// <summary>
/// <see cref="LexCore.Entities.MediaFile.Filename"/> is a repo-relative path that is matched against
/// other paths by exact string comparison, so it must be canonicalised to a single separator convention
/// regardless of the OS that produced it (fw-headless runs on Linux '/', dev/test on Windows '\').
/// Otherwise a row stored under one convention fails to match its on-disk file and its Id — which anchors
/// the file in the CRDT/FwData bridge — gets churned into a fresh Guid.
/// </summary>
public static class MediaFilePath
{
    public static string Normalize(string path) => path.Replace('\\', '/');

    // NFD because LCM stores media references as NFD, and Linux file names aren't Unicode-normalized.
    public static string ForUpload(string subfolder, Guid fileId, string filename) =>
        Normalize(Path.Join(MediaFileController.LinkedFiles, subfolder, fileId.ToString(), filename.Normalize(NormalizationForm.FormD)));
}
