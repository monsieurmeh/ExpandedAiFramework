using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework.Enums
{

    public enum ExpandedAiSubtype : int
    {
        None = (int)AiSubType.None,
        Wolf = (int)AiSubType.Wolf,
        Bear = (int)AiSubType.Bear,
        Stag = (int)AiSubType.Stag,
        Rabbit = (int)AiSubType.Rabbit,
        Moose = (int)AiSubType.Moose,
        Cougar = (int)AiSubType.Cougar,
        Ptarmigan = (int)AiSubType.Cougar + 1,
        Timberwolf,
        COUNT
    }
}
