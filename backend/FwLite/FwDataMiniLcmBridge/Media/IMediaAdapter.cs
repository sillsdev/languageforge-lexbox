using MiniLcm;
using MiniLcm.Media;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Media;

public interface IMediaAdapter
{
    /// <summary>
    /// get the MediaUri representing a file, can be used later to get the path back
    /// </summary>
    /// <param name="path">the full file path must be inside the project LinkedFiles directory</param>
    /// <param name="cache">the current project</param>
    /// <returns>a media uri which can later be used to get the path</returns>
    MediaUri MediaUriFromPath(string path, LcmCache cache);
    /// <summary>
    ///
    /// </summary>
    /// <param name="mediaUri"></param>
    /// <param name="cache"></param>
    /// <returns>the full path to the file represented by the mediaUri, will return null when it can't find the file</returns>
    string? PathFromMediaUri(MediaUri mediaUri, LcmCache cache);
}
