using MiniLcm.Models;
using SIL.WritingSystems;

namespace MiniLcm.Culture;

public interface IWritingSystemCollatorProvider
{
    ICollator GetCollator(WritingSystem writingSystem);
}
