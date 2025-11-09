using System.Collections.Generic;

namespace CrossfadER;

public partial class HandleCrossfadeData
{
    private readonly Dictionary<string, float> CrossfadeTime = new Dictionary<string, float>
    {
        { "0.5s", 0.5f },
        { "1s", 1f },
        { "1.5s", 1.5f },
        { "2s", 2f },
    };

    public float GetCrossfadeTime(string time)
    {
        return CrossfadeTime[time];
    }
}