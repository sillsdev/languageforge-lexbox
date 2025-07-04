using MiniLcm;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Media;

public interface IMediaAdapter
{
    /// <summary>
    /// get the MediaUri representing a file, can be used later to get the path back
    /// </summary>
    /// <param name="path">the path relative to LinkedFiles to find the file at</param>
    /// <param name="cache">the current project</param>
    /// <returns>a media uri which can later be used to get the path</returns>
    MediaUri MediaUriFromPath(string path, LcmCache cache);
    /// <summary>
    ///
    /// </summary>
    /// <param name="mediaUri"></param>
    /// <param name="cache"></param>
    /// <returns>the path to the file represented by the mediaUri, relative to the LinkedFiles directory in the given project</returns>
    string PathFromMediaUri(MediaUri mediaUri, LcmCache cache);
}
