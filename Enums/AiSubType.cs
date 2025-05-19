using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework.Enums
{

    public enum ExpandedAiSubtype : int
    {
        Ptarmigan = (int)AiSubType.Cougar + 1,
        Timberwolf,
        COUNT
    }
}
